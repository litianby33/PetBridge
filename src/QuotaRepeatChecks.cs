using System;
using System.IO;
using System.Web.Script.Serialization;
namespace PetBridge {
    static class QuotaRepeatChecks {
        static void Require(bool ok,string title){if(!ok)throw new Exception("Quota repeat: "+title);}
        public static void Run(){
            var state=new QuotaAlarmState();var alarm=new QuotaAlarm(state);var s=new UsageSnapshot{Primary=new Quota{Remaining=0,ResetsAt=1000},Secondary=new Quota{Remaining=80,ResetsAt=10000}};
            for(int i=0;i<120;i++){s.Primary.ResetsAt=1000+i;Require(alarm.Observe(s,i%2==0?20:15,10,100+i)==null,"zero polling quiet despite reset and threshold changes");}
            s.Secondary.Remaining=5;Require(alarm.Observe(s,20,10,300)==null,"weekly threshold cannot reannounce exhausted five-hour quota");
            s.Primary.Remaining=50;s.Secondary.Remaining=0;for(int i=0;i<30;i++){s.Secondary.ResetsAt+=i;Require(alarm.Observe(s,20,10,400+i)==null,"weekly exhaustion quiet on periodic reads");}
            s.Secondary.Remaining=80;s.Primary.Remaining=19;s.Primary.ResetsAt=2000;Require(alarm.Observe(s,20,10,500).Kind=="low","positive low warning retained");Require(alarm.Observe(s,20,10,501)==null,"positive threshold once");
            s.Primary.Remaining=0;Require(alarm.Observe(s,20,10,600)==null,"transition to zero waits for terminal event");
            var ledger=new TaskEndLedger(null);int reminders=0;
            foreach(string status in new[]{"task_complete","task_failed","turn_aborted"}){
                var evt=new CompletionEvent{ThreadId="thread",TurnId=status,Status=status};
                for(int i=0;i<10;i++)if(ledger.TryClaim(evt)){var d=alarm.AfterTask(s,20,10,700);Require(d!=null&&d.Kind=="empty"&&d.Title=="5 小时已用完啦","terminal quota exhausted wording");reminders++;}
                Require(!ledger.TryClaim(new CompletionEvent{ThreadId="thread",TurnId=status,Status="task_complete",Source="another-monitor"}),"duplicate source or changed terminal status is same task");
            }Require(reminders==3,"one alert each for complete, fail and abort");
            Require(!ledger.TryClaim(new CompletionEvent{ThreadId="thread",TurnId="",Status="task_failed"}),"unknown task identity does not generate a quota alert");
            Require(!ledger.TryClaim(new CompletionEvent{ThreadId="thread",TurnId="running",Status="task_started"}),"nonterminal event cannot claim an alert");
            string path=Path.Combine(Program.DataRoot,"qa-terminal-receipts-test.json");string id=Guid.NewGuid().ToString();var persistent=new TaskEndLedger(path);Require(persistent.TryClaim(new CompletionEvent{ThreadId="thread",TurnId=id,Status="task_failed"}),"first persistent receipt");Require(!new TaskEndLedger(path).TryClaim(new CompletionEvent{ThreadId="thread",TurnId=id,Status="task_complete"}),"restart or reconnect cannot replay terminal quota alert");
            Require(new TaskEndLedger(path).TryClaim(new CompletionEvent{ThreadId="thread",TurnId=id+"-next",Status="turn_aborted"}),"new task is independently eligible");
            s.Primary.ResetsAt=3000;s.Primary.Remaining=100;var reset=alarm.Observe(s,20,10,2001);Require(reset!=null&&reset.Kind=="reset","real five-hour reset still informs once");Require(alarm.Observe(s,20,10,2002)==null,"reset not repeated");
            File.WriteAllText(Path.Combine(Program.DataRoot,"verification-quota-repeat.json"),new JavaScriptSerializer().Serialize(new{passed=true,zeroPeriodicReads=120,weeklyZeroPeriodicReads=30,thresholdChangesQuietAtZero=true,onePerTerminalTask=true,completedFailedAborted=true,differentSourcesDedup=true,restartDedup=true,positiveLowAndResetPreserved=true}));
        }
    }
}
