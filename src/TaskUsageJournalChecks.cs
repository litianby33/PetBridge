using System;
using System.IO;
using System.Web.Script.Serialization;
namespace PetBridge {
    static class TaskUsageJournalChecks {
        public static void Run(){
            string id="11111111-1111-4111-8111-111111111111",root=Path.Combine(Path.GetTempPath(),"pet-task-usage-check-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            string path="rollout-test-"+id+".jsonl";var json=new JavaScriptSerializer();
            Func<int,double,long,string> quota=delegate(int sec,double value,long reset){return json.Serialize(new{timestamp=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero).AddSeconds(sec).ToString("o"),type="event_msg",payload=new{type="token_count",rate_limits=new{limit_id="codex",secondary=new{used_percent=value,window_minutes=10080,resets_at=reset}}}});};
            Func<int,string,string,string> evt=delegate(int sec,string turn,string kind){return json.Serialize(new{timestamp=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero).AddSeconds(sec).ToString("o"),type="event_msg",payload=new{type=kind,turn_id=turn}});};
            var ledger=new TaskUsageJournal(Path.Combine(root,"usage.json"));ledger.Observe(quota(0,20,1000),path,0);ledger.Observe(evt(1,"one","task_started"),path,0);
            ledger.Observe(quota(5,22,1000),path,1);ledger.Observe(evt(6,"one","task_complete"),path,1);ledger.Observe(evt(6,"one","task_complete"),path,1);
            if(ledger.Latest(id).State!="sampled"||ledger.Latest(id).WeekDelta!=2||ledger.Recent(id,10).Length!=1)throw new Exception("Task usage delta/dedup");
            ledger.Observe(evt(7,"two","task_started"),path,1);ledger.Observe(quota(8,24,1000),path,2);ledger.Observe(evt(9,"two","turn_aborted"),path,2);
            if(ledger.Latest(id).State!="parallel"||ledger.Latest(id).WeekDelta.HasValue)throw new Exception("Parallel usage must not be attributed");
            ledger.Observe(evt(10,"three","task_started"),path,0);ledger.Observe(quota(11,1,2000),path,1);ledger.Observe(evt(12,"three","task_complete"),path,1);
            if(ledger.Latest(id).State!="reset"||ledger.Latest(id).WeekDelta.HasValue)throw new Exception("Window reset attributed to task");
            var missing=new TaskUsageJournal(Path.Combine(root,"missing.json"));missing.Observe(evt(1,"missing","task_started"),path,0);missing.Observe(quota(5,5,1000),path,1);missing.Observe(evt(6,"missing","task_failed"),path,1);
            if(missing.Latest(id).State!="missing")throw new Exception("Missing baseline became zero");
            ledger.Observe(evt(13,"four","task_started"),path,0);var loaded=new TaskUsageJournal(Path.Combine(root,"usage.json"));loaded.Observe(quota(14,3,2000),path,1);loaded.Observe(evt(15,"four","task_complete"),path,1);
            if(loaded.Latest(id).State!="gap"||loaded.Latest(id).WeekDelta.HasValue)throw new Exception("Restart gap presented as precise usage");
            Func<int,double,long,string> five=delegate(int sec,double value,long reset){return json.Serialize(new{timestamp=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero).AddSeconds(sec).ToString("o"),type="event_msg",payload=new{type="token_count",rate_limits=new{limit_id="codex",primary=new{used_percent=value,window_minutes=300,resets_at=reset}}}});};
            var dual=new TaskUsageJournal(Path.Combine(root,"dual.json"));
            dual.Observe(quota(0,20,1000),path,0);dual.Observe(five(0,30,500),path,0);dual.Observe(evt(1,"dual","task_started"),path,0);
            dual.Observe(quota(5,22,1000),path,1);dual.Observe(five(5,35,500),path,1);dual.Observe(evt(6,"dual","task_complete"),path,1);
            var record=dual.Latest(id);if(record.WeekDelta!=2||record.FiveDelta!=5||record.FiveState!="sampled")throw new Exception("Independent five-hour and weekly sampling");
            dual.Observe(evt(7,"reset-five","task_started"),path,0);dual.Observe(five(8,1,700),path,1);dual.Observe(quota(8,23,1000),path,1);dual.Observe(evt(9,"reset-five","task_complete"),path,1);
            record=dual.Latest(id);if(record.FiveState!="reset"||record.FiveDelta.HasValue||record.WeekDelta!=1)throw new Exception("Five-hour reset affected valid weekly delta");
            var onlyFive=new TaskUsageJournal(Path.Combine(root,"only-five.json"));onlyFive.Observe(five(0,10,500),path,0);onlyFive.Observe(evt(1,"only","task_started"),path,0);onlyFive.Observe(five(5,13,500),path,1);onlyFive.Observe(evt(6,"only","task_failed"),path,1);
            record=onlyFive.Latest(id);if(record.FiveDelta!=3||record.State!="missing"||!TaskUsageJournal.Display(record,true).Contains("3"))throw new Exception("Missing weekly data suppressed five-hour record");
            var account=new TaskUsageJournal(Path.Combine(root,"account.json"));var origin=new DateTimeOffset(2026,1,1,0,0,0,TimeSpan.Zero);
            account.UpdateAccount(new UsageSnapshot{Primary=new Quota{Remaining=80,WindowMinutes=10080,ResetsAt=1000},Secondary=new Quota{Remaining=60,WindowMinutes=300,ResetsAt=500}},origin);
            account.Observe(evt(1,"account","task_started"),path,0);
            account.UpdateAccount(new UsageSnapshot{Primary=new Quota{Remaining=78,WindowMinutes=10080,ResetsAt=1000},Secondary=new Quota{Remaining=56,WindowMinutes=300,ResetsAt=500}},origin.AddSeconds(5));account.Observe(evt(6,"account","task_complete"),path,1);
            record=account.Latest(id);if(record.WeekDelta!=2||record.FiveDelta!=4)throw new Exception("Account sampling must use window length, not slot");
            var persisted=new TaskUsageJournal(Path.Combine(root,"account.json"));if(persisted.Latest(id).FiveDelta!=4)throw new Exception("Five-hour history did not persist");
            File.WriteAllText(Path.Combine(root,"legacy.json"),json.Serialize(new[]{new TaskUsageEntry{ThreadId=id,TurnId="legacy",State="sampled",WeekDelta=2}}));
            var legacy=new TaskUsageJournal(Path.Combine(root,"legacy.json"));if(legacy.Latest(id).WeekDelta!=2||TaskUsageJournal.Display(legacy.Latest(id),true)!="缺少起止采样")throw new Exception("Legacy weekly record compatibility");
            dual.Observe(evt(10,"parallel-five","task_started"),path,1);dual.Observe(five(11,2,700),path,2);dual.Observe(evt(12,"parallel-five","turn_aborted"),path,2);
            if(dual.Latest(id).FiveDelta.HasValue||dual.Latest(id).FiveState!="parallel")throw new Exception("Parallel five-hour usage attributed");
            onlyFive.Observe(evt(300,"stale","task_started"),path,0);onlyFive.Observe(five(301,15,500),path,1);onlyFive.Observe(evt(302,"stale","task_complete"),path,1);
            if(onlyFive.Latest(id).FiveDelta.HasValue||onlyFive.Latest(id).FiveState!="missing")throw new Exception("Stale five-hour baseline used");
        }
    }
}
