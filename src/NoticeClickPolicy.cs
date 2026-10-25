using System;
namespace PetBridge {
    static class NoticeClickPolicy {
        public static bool ShouldNavigate(string kind,string link,bool navigateOnTaskClick){
            if(String.IsNullOrWhiteSpace(link))return false;
            // Low/empty quota messages may represent the same terminal task as a
            // completion message. Input requests keep their action link.
            bool taskNotice=kind=="complete"||kind=="low"||kind=="empty";
            return !taskNotice||navigateOnTaskClick;
        }
        public static string CompletionText(bool navigate){return navigate?"点击气泡回到对话查看":"点击气泡关闭";}
        public static void Verify(){
            const string link="codex://threads/11111111-1111-4111-8111-111111111111";
            foreach(string kind in new[]{"complete","low","empty"}){
                if(ShouldNavigate(kind,link,false))throw new Exception("Dismiss-only task notice tried to navigate");
                if(!ShouldNavigate(kind,link,true))throw new Exception("Navigate-on-click task notice lost its link");
            }
            if(!ShouldNavigate("input",link,false)||!ShouldNavigate("input",link,true))throw new Exception("Input request navigation must remain available");
            if(ShouldNavigate("complete","",true)||ShouldNavigate("input",null,true))throw new Exception("A notice without a target must only dismiss");
        }
    }
}
