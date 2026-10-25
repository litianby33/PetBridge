using System;
namespace PetBridge {
    // Source switches govern activity/completion only. Questions have their own switch.
    static class AppSourcePolicy {
        public static bool Ordinary(string source){return String.Equals(source,"chatgpt",StringComparison.OrdinalIgnoreCase)||(source??"").StartsWith("chatgpt:",StringComparison.OrdinalIgnoreCase);}
        public static bool NeedsBridge(PetSettings s){return s.AppChatMonitoring&&(s.OrdinaryChatMonitoring||s.WorkAppMonitoring||s.InputReminders);}
        public static bool Allows(AppThreadState t,PetSettings s){return s.AppChatMonitoring&&(t.Kind=="chatgpt"?s.OrdinaryChatMonitoring:s.WorkAppMonitoring);}
        public static bool Allows(CompletionEvent e,PetSettings s){
            if(e==null)return false;
            if(Ordinary(e.Source))return s.AppChatMonitoring&&s.OrdinaryChatMonitoring;
            if((e.Source??"").StartsWith("codex:",StringComparison.OrdinalIgnoreCase)&&!String.Equals(e.Source,"codex:local",StringComparison.OrdinalIgnoreCase))return s.AppChatMonitoring&&s.WorkAppMonitoring;
            return true; // The local Codex file watcher is independent of the app bridge.
        }
    }
    // Completion popups have separate preferences; activity and input requests do not
    // pass through this policy. A remote Work task is codex:durable, not chatgpt:.
    static class CompletionNoticePolicy {
        public static bool CanShow(CompletionEvent evt,PetSettings settings){
            if(evt==null||evt.Status!="task_complete"||!AppSourcePolicy.Allows(evt,settings))return false;
            bool ordinary=String.Equals(evt.Source,"chatgpt",StringComparison.OrdinalIgnoreCase)||(evt.Source??"").StartsWith("chatgpt:",StringComparison.OrdinalIgnoreCase);
            return ordinary?settings.ChatGPTCompletionReminders:settings.TaskReminders;
        }
        public static void Verify(){
            var settings=new PetSettings{TaskReminders=true,ChatGPTCompletionReminders=false};
            var ordinary=new CompletionEvent{Source="chatgpt:",UsesCodexQuota=false};
            var cloudWork=new CompletionEvent{Source="codex:durable"};
            var localWork=new CompletionEvent{Source="codex:local"};
            var codex=new CompletionEvent{Source="codex"};
            if(CanShow(ordinary,settings)||!CanShow(cloudWork,settings)||!CanShow(localWork,settings)||!CanShow(codex,settings))throw new Exception("Completion source preferences did not isolate ordinary ChatGPT");
            settings.TaskReminders=false;settings.ChatGPTCompletionReminders=true;settings.OrdinaryChatMonitoring=true;
            if(!CanShow(ordinary,settings)||CanShow(cloudWork,settings)||CanShow(codex,settings))throw new Exception("Completion preferences must be independent");
            settings.OrdinaryChatMonitoring=false;if(CanShow(ordinary,settings))throw new Exception("Disabled ordinary source bypassed by completion switch");
            settings.WorkAppMonitoring=false;settings.TaskReminders=true;
            if(CanShow(cloudWork,settings)||!CanShow(localWork,settings)||!CanShow(codex,settings))throw new Exception("Work source switch must preserve local Codex");
            settings.AppChatMonitoring=false;if(!CanShow(codex,settings))throw new Exception("Local Codex must survive app bridge off");
            ordinary.Status="task_failed";if(CanShow(ordinary,settings))throw new Exception("A failed task is not a completion popup");
        }
    }
}
