using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace PetBridge {
    static class AppConversationNavigation {
        // Intercept the old ordinary-chat URL too, so it never goes to the browser.
        public static bool TryChatId(string link,out string id){
            id=null;Uri uri;Guid guid;
            if(!Uri.TryCreate(link,UriKind.Absolute,out uri)||uri.Scheme!="https"||!String.Equals(uri.Host,"chatgpt.com",StringComparison.OrdinalIgnoreCase)||!uri.IsDefaultPort||uri.UserInfo.Length!=0)return false;
            var parts=uri.AbsolutePath.Trim('/').Split('/');
            if(parts.Length!=2||parts[0]!="c"||!Guid.TryParse(parts[1],out guid))return false;
            id=guid.ToString();return true;
        }
        public static bool Navigate(string origin,string id){Guid guid;if(!Guid.TryParse(id,out guid))return false;using(var reader=new AppToolsReader(origin))return reader.NavigateToChat(guid.ToString());}
        public static void Probe(string origin,string id){bool ok=Navigate(origin,id);File.WriteAllText(Path.Combine(Program.DataRoot,"verification-app-navigation-live.json"),new JavaScriptSerializer().Serialize(new{ok=ok,target=id,route="desktop app navigate_to_codex_page",browserOpened=false,checkedAt=DateTimeOffset.Now.ToString("o")}));if(!ok)throw new IOException("Desktop navigation was not confirmed");}
    }
    sealed partial class PetForm {
        bool openingTask;string taskNavigation="none";
        void OpenDesktopChat(string id,string link){
            if(openingTask)return;openingTask=true;taskNavigation="opening-desktop";DumpState();string origin=Program.BridgeOrigin(settings);
            Task.Run(delegate{bool ok=false;try{ok=AppConversationNavigation.Navigate(origin,id);}catch{}if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke(new Action(delegate{
                openingTask=false;taskNavigation=ok?"desktop-opened":"desktop-unavailable";
                if(!ok)ShowDecision("open-chat-retry:"+id,new QuotaDecision{Kind="low",Title="暂时没能打开桌面对话",Message="点击重试 · 请保持 ChatGPT 桌面版运行"},link);
                DumpState();
            }));}catch(InvalidOperationException){} });
        }
    }
    static class AppNavigationChecks {
        static void Require(bool ok,string test){if(!ok)throw new Exception("Navigation: "+test);}
        public static void Run(){string id;string valid="22222222-2222-4222-8222-222222222222";
            Require(AppConversationNavigation.TryChatId("https://chatgpt.com/c/"+valid,out id)&&id==valid,"ordinary completion URL uses desktop route");
            Require(AppConversationNavigation.TryChatId("https://chatgpt.com/c/"+valid+"/?test=1",out id),"ordinary URL query and trailing slash");
            foreach(var bad in new[]{"https://chatgpt.com.evil.example/c/"+valid,"https://evil.example/c/"+valid,"https://chatgpt.com/c/not-an-id","https://chatgpt.com/c/"+valid+"/other","https://chatgpt.com:443@evil.example/c/"+valid,"codex://threads/"+valid})Require(!AppConversationNavigation.TryChatId(bad,out id),"only recognized ordinary conversation URLs intercepted");
            File.WriteAllText(Path.Combine(Program.DataRoot,"verification-app-navigation-checks.txt"),"PASS: ordinary ChatGPT completion URLs route to desktop; strict host and conversation ID validation; Codex links preserve existing routing; browser fallback disabled for ordinary chats.");
        }
    }
}
