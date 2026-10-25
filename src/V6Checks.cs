using System;
using System.Drawing;
using System.IO;
namespace PetBridge {
    static class V6Checks {
        static void Require(bool passed,string test){if(!passed)throw new Exception("v6: "+test);}
        public static void Run(){ChatRoutingChecks.Run();QuotaLayoutChecks.Run();QuotaRepeatChecks.Run();InputReminderChecks.Run();AppNavigationChecks.Run();AppBridgeChecks.Run();NoticeJumpChecks.Run();NoticeLayoutChecks.Run();
            using(var started=new System.Threading.ManualResetEvent(false))using(var release=new System.Threading.ManualResetEvent(false))using(var finished=new System.Threading.ManualResetEvent(false)){
                int reads=0;double freshValue=0;
                using(var usage=new UsageService(delegate{int n=System.Threading.Interlocked.Increment(ref reads);if(n==1){started.Set();release.WaitOne(3000);}return new UsageSnapshot{Primary=new Quota{Remaining=n,ResetsAt=1000}};})){
                    usage.Refresh();Require(started.WaitOne(3000),"first quota read started");usage.RefreshFresh(delegate(UsageSnapshot data){freshValue=data.Primary.Remaining;finished.Set();});release.Set();Require(finished.WaitOne(5000)&&freshValue>=2,"task must get read begun after event, never stale in-flight read");
                }
            }
            var state=new QuotaAlarmState();var alarm=new QuotaAlarm(state);
            var s=new UsageSnapshot{Primary=new Quota{Remaining=40,ResetsAt=1000},Secondary=new Quota{Remaining=80,ResetsAt=10000}};
            Require(alarm.Observe(s,20,10,100)==null,"above threshold quiet");s.Primary.Remaining=20;Require(alarm.Observe(s,20,10,200)==null,"strict threshold");s.Primary.Remaining=19;var a=alarm.Observe(s,20,10,300);Require(a!=null&&a.Kind=="low","cross once");Require(alarm.Observe(s,20,10,301)==null,"no repeated poll alert");Require(new QuotaAlarm(state).Observe(s,20,10,302)==null,"dedup survives reload");
            Require(alarm.AfterTask(s,20,10,400).Kind=="low"&&alarm.AfterTask(s,20,10,401).Kind=="low","each terminal task gets one low notice");
            s.Primary.Remaining=0;a=alarm.AfterTask(s,20,10,500);Require(a.Kind=="empty"&&a.Title=="5 小时已用完啦"&&a.Message.Contains("9 分钟"),"exhaustion text and countdown");Require(alarm.Observe(s,20,10,501)==null,"zero must not repeat low notice");Require(alarm.AfterTask(s,0,0,600).Kind=="empty","exhaustion with threshold disabled");
            s.Primary.Remaining=100;s.Primary.ResetsAt=2000;a=alarm.Observe(s,20,10,1001);Require(a!=null&&a.Kind=="reset","automatic reset blue notice");Require(alarm.Observe(s,20,10,1002)==null,"reset once");Require(new QuotaAlarm(state).Observe(s,20,10,1003)==null,"reset persists");
            s.Secondary.Remaining=5;Require(alarm.AfterTask(s,20,10,1100).Message.Contains("本周剩余 5%"),"weekly low");s.Secondary.Remaining=0;Require(alarm.AfterTask(s,20,10,1200).Title=="本周额度已用完啦","weekly exhausted");
            s.Secondary.Remaining=80;Require(alarm.AfterTask(s,20,10,1201)==null,"healthy completion no quota notice");
            Require(NoticeCard.NeedsBelow(20,0,-300)&&!NoticeCard.NeedsBelow(300,0,-300),"bubble side depends on head room");Require(Math.Abs(NoticeCard.Expand(0))<.001&&Math.Abs(NoticeCard.Expand(1)-1)<.001&&NoticeCard.Expand(.7)>1,"ball expansion endpoints and rebound");
            Require(NoticeCard.ParseColor("#7f181b").ToArgb()==Color.FromArgb(127,24,27).ToArgb(),"muted exhaustion color");
            var monitor=new Rectangle(1920,0,1920,1080);Require(PetForm.CoversMonitor(monitor,monitor)&&!PetForm.CoversMonitor(new Rectangle(1920,0,1920,1040),monitor),"fullscreen vs maximized secondary screen");
            var area=new Rectangle(0,0,1920,1080);Require(PetForm.DetectEdge(new Rectangle(4,100,140,220),area,3)==null&&PetForm.DetectEdge(new Rectangle(3,100,140,220),area,3)=="left","close actual character edge");Require(PetForm.DetectEdge(new Rectangle(1777,100,140,220),area,3)=="right","right edge");
            string path="rollout-2026-10-20T00-00-00-11111111-1111-4111-8111-111111111111.jsonl";foreach(var kind in new[]{"task_complete","turn_aborted","task_failed"}){string line="{\"type\":\"event_msg\",\"payload\":{\"type\":\""+kind+"\",\"turn_id\":\"one\"}}";Require(CompletionWatcher.Parse(line,path).Status==kind,"terminal "+kind);}
            string large=Path.Combine(Program.DataRoot,"qa-long-turn.jsonl");using(var writer=new StreamWriter(large)){writer.WriteLine("{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_started\",\"turn_id\":\"long\"}}");string filler="{\"type\":\"response_item\",\"payload\":{\"text\":\""+new string('x',8192)+"\"}}";for(int i=0;i<700;i++)writer.WriteLine(filler);}var longTask=new TaskActivityReducer();longTask.Bootstrap(large);Require(longTask.Count==1,"restart during turn larger than old 4MB tail");File.AppendAllText(large,"{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_complete\",\"turn_id\":\"long\"}}\n");var endedLong=new TaskActivityReducer();endedLong.Bootstrap(large);Require(endedLong.Count==0,"finished long turn remains idle");
            File.WriteAllText(large,"{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_started\",\"turn_id\":\"old\"}}\n{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_started\",\"turn_id\":\"new\"}}\n{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_complete\",\"turn_id\":\"old\"}}\n");var late=new TaskActivityReducer();late.Bootstrap(large);Require(late.Count==1,"late old completion must not hide newer running task on restart");
            File.WriteAllText(Path.Combine(Program.DataRoot,"verification-policy-v6.txt"),"PASS: fresh read after task event, threshold crossing, strict boundary, persisted dedup, per-task low quota, zero and countdown, reset once, weekly, bubble placement and morph, muted color, fullscreen geometry, edge geometry, complete/abort/failure lifecycle, restart during long turn, late old completion.");
        }
    }
}
