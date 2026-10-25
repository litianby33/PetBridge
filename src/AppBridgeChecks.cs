using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
namespace PetBridge {
    sealed partial class PetForm {
        public string AppBridgeDescription {get{
            string status=appChats==null?"已关闭":appChats.Connected?"已连接":"暂未连接，正在自动重试";
            return "聊天桥接："+status+"\n普通 ChatGPT 联动："+(settings.AppChatMonitoring&&settings.OrdinaryChatMonitoring?"开启":"关闭")+"\nWork / 云端 Codex 联动："+(settings.AppChatMonitoring&&settings.WorkAppMonitoring?"开启":"关闭")+" · 进行中："+appActiveTasks+" 个\n\n普通聊天联动关闭后，不触发思考、三点状态\n或完成提醒。提问提醒由上方开关独立控制。\n本地 Codex 任务监听不受这两个联动开关影响。\n\n约 2.5 秒检查最近 50 个及置顶聊天。\n为保留提问提醒，仍会读取应用导出的状态。\n取消、失败和断线不会报成完成。\n\n5 小时 / 周额度属于 Codex。\n此为本机兼容桥接，应用更新后可能需适配。";
        }}
    }
    static class AppBridgeChecks {
        static Dictionary<string,object> Parse(string s){return new JavaScriptSerializer().DeserializeObject(s) as Dictionary<string,object>;}
        static void Require(bool yes,string title){if(!yes)throw new Exception("App bridge: "+title);}
        static void SourceSwitches(){
            var s=new PetSettings();int running=0,ended=0,questions=0;string state="active",turn="inProgress",turnId="fresh";int revision=1;
            Func<string,object,Dictionary<string,object>> fake=delegate(string method,object args){
                if(method=="list_threads")return Parse(new JavaScriptSerializer().Serialize(new{threads=new[]{new{id="ordinary",kind="chatgpt",hostId="",status=state,updatedAt=revision},new{id="work",kind="codex",hostId="durable",status=state,updatedAt=revision}}}));
                return Parse(new JavaScriptSerializer().Serialize(new{thread=new{status=new{type="active",activeFlags=turn=="inProgress"?new[]{"waitingOnUserInput"}:new string[0]}},turns=new[]{new{id=turnId,status=turn,completedAt=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+30}}}));
            };
            using(var w=new AppChatWatcher("test",v=>running=v,e=>{Require(e.Source=="codex:durable","ordinary completion leaked through disabled source");ended++;},delegate{},fake,e=>{if(!e.Resolved)questions++;},t=>AppSourcePolicy.Allows(t,s))){
                w.PollOnceForTest();Require(running==1&&questions==2,"default excludes ordinary activity but keeps Work and both question sources");
                s.OrdinaryChatMonitoring=true;w.RefreshPolicy();Require(running==2,"source enable applies immediately");
                s.OrdinaryChatMonitoring=false;s.WorkAppMonitoring=false;w.RefreshPolicy();Require(running==0,"source disable clears activity immediately");
                state="idle";turn="completed";revision++;w.PollOnceForTest();Require(ended==0,"disabled sources cannot complete");
                s.WorkAppMonitoring=true;w.RefreshPolicy();revision++;w.PollOnceForTest();Require(ended==0,"reenabling never replays suppressed terminal turn");
                turnId="next";state="active";turn="inProgress";revision++;w.PollOnceForTest();Require(running==1,"Work still runs with ordinary off");state="idle";turn="completed";revision++;w.PollOnceForTest();Require(ended==1,"new Work task completes exactly once with ordinary off");w.PollOnceForTest();Require(ended==1,"Work completion remains deduplicated");
            }
            Require(AppSourcePolicy.NeedsBridge(s),"questions keep the bridge available");s.WorkAppMonitoring=false;s.InputReminders=false;Require(!AppSourcePolicy.NeedsBridge(s),"all app channels disabled stops bridge");
        }
        public static void Run(){
            var list=AppState.Threads(Parse("{\"pinnedThreads\":[{\"id\":\"a\",\"kind\":\"chatgpt\",\"status\":\"active\"}],\"threads\":[{\"id\":\"a\",\"kind\":\"chatgpt\",\"status\":\"active\"},{\"id\":\"b\",\"kind\":\"codex\",\"hostId\":\"local\",\"status\":\"active\"},{\"id\":\"c\",\"kind\":\"codex\",\"hostId\":\"durable\",\"status\":\"idle\"}]}"));
            Require(list.Count==2&&list[0].Running&&!list[1].Running,"dedup pinned, omit local duplicates, include remote");
            var chat=list[0];var done=Parse("{\"turns\":[{\"id\":\"turn1\",\"status\":\"completed\",\"completedAt\":120.5}]}");
            var evt=AppState.Terminal(chat,done,100);Require(evt!=null&&!evt.UsesCodexQuota&&evt.Link=="https://chatgpt.com/c/a","ChatGPT completion has its own link and no Codex quota");
            Require(AppState.Terminal(list[1],done,100).UsesCodexQuota,"remote Codex quota association");
            Require(AppState.Terminal(chat,done,130)==null,"ignore historical completion");
            foreach(string status in new[]{"failed","interrupted","cancelled"}){var terminalEvent=AppState.Terminal(chat,Parse("{\"turns\":[{\"id\":\"x\",\"status\":\""+status+"\",\"completedAt\":120}]}"),100);Require(terminalEvent!=null&&terminalEvent.Status!="task_complete","terminal "+status+" is distinct from success");}
            Require(AppState.Terminal(chat,Parse("{\"turns\":[{\"id\":\"x\",\"status\":\"inProgress\",\"completedAt\":120}]}"),100)==null,"running is not terminal");
            Require(AppState.Terminal(chat,Parse("{\"turns\":[]}"),100)==null,"empty history never means completed");
            Require(AppState.Terminal(chat,Parse("{\"turns\":[{\"id\":\"x\",\"status\":\"completed\"}]}"),100)==null,"missing timestamp never replays history");
            int poll=0,events=0,running=0;bool failing=false;string latestStatus="completed",latestId="old",threadStatus="idle";double updated=1,ended=1;
            Func<string,object,Dictionary<string,object>> fake=delegate(string method,object args){if(failing)throw new IOException("disconnect");if(method=="list_threads"){poll++;return Parse("{\"threads\":[{\"id\":\"test-chat\",\"kind\":\"chatgpt\",\"status\":\""+threadStatus+"\",\"updatedAt\":"+updated.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}]}");}return Parse("{\"turns\":[{\"id\":\""+latestId+"\",\"status\":\""+latestStatus+"\",\"completedAt\":"+ended.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}]}");};
            using(var watcher=new AppChatWatcher("test-origin",v=>running=v,e=>{Require(!e.UsesCodexQuota,"ordinary chat callback cannot use Codex quota");if(e.Status=="task_complete")events++;},delegate{},fake)){
                watcher.PollOnceForTest();Require(events==0&&running==0,"startup never replays old completed chats");
                threadStatus="active";updated=2;latestId="new1";latestStatus="inProgress";ended=DateTimeOffset.UtcNow.ToUnixTimeSeconds()+10;watcher.PollOnceForTest();Require(running==1&&events==0,"real active state drives thinking");
                threadStatus="idle";watcher.PollOnceForTest();Require(events==0,"stale detail after idle must not fake completion");
                latestStatus="completed";watcher.PollOnceForTest();Require(running==0&&events==1,"retry lagging detail confirms completion");watcher.PollOnceForTest();Require(events==1,"same terminal turn once");
                updated=3;latestStatus="interrupted";latestId="new2";watcher.PollOnceForTest();Require(events==1,"cancel is quiet");
                updated=4;latestStatus="completed";latestId="quick";watcher.PollOnceForTest();Require(events==2,"short turn completed between polls detected");
                failing=true;watcher.PollOnceForTest();Require(events==2&&!watcher.Connected,"disconnect cannot generate a completion");
            }
            SourceSwitches();
            File.WriteAllText(Path.Combine(Program.DataRoot,"verification-app-bridge-checks.txt"),"PASS: pinned dedup; local duplicate suppression; ChatGPT and remote sources; source-specific quota and links; historical/failed/interrupted/cancelled/in-progress/empty/unknown completion suppression; startup baseline; active to idle; delayed detail; terminal dedup; short turns; disconnect.");
        }
    }
}

