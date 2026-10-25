using System;
using System.IO;
using System.Text;

namespace PetBridge {
    // Ordinary ChatGPT is a desktop draft handoff. It never runs through Codex CLI.
    // mode=chat/codex is accepted by the installed desktop deep-link handler;
    // prompt prefills the composer, and the user still sends in the desktop app.
    static class ChatRouting {
        public const int MaxDraftUrlLength=8000;
        public static string NormalizeTarget(string target){return String.Equals((target??"").Trim(),"chatgpt",StringComparison.OrdinalIgnoreCase)?"chatgpt":"codex";}
        public static bool IsOrdinary(string target){return NormalizeTarget(target)=="chatgpt";}
        public static bool TryDraftUrl(string text,string target,out string url,out string error){
            url=null;error=null;
            if(String.IsNullOrWhiteSpace(text)){error="请先输入要带到桌面的内容。";return false;}
            // Avoid framework URI length errors before percent-encoding; never truncate a draft.
            if(text.Length>MaxDraftUrlLength){error=LongDraftMessage;return false;}
            string value="codex://new?mode="+(IsOrdinary(target)?"chat":"codex")+"&prompt="+Uri.EscapeDataString(text);
            if(value.Length>MaxDraftUrlLength){error=LongDraftMessage;return false;}
            url=value;return true;
        }
        public static string LongDraftMessage {get{return "内容较长，无法完整放进桌面草稿链接。输入已保留，请从左侧模式菜单的“聊天选项”选择“复制输入内容”，再到桌面粘贴。";}}
    }
    static class ChatRoutingChecks {
        static void Require(bool ok,string name){if(!ok)throw new Exception("Chat routing: "+name);}
        public static void Run(){
            Require(ChatRouting.NormalizeTarget(" ChatGPT ")=="chatgpt"&&ChatRouting.NormalizeTarget(null)=="codex"&&ChatRouting.NormalizeTarget("work")=="codex","normalize known target; unknown falls back to Codex");
            string url,error;string text="  中文 😀 & ? # = + %\r\n第二行  ";
            Require(ChatRouting.TryDraftUrl(text,"chatgpt",out url,out error)&&error==null&&url.StartsWith("codex://new?mode=chat&prompt="),"ordinary target opens an unsent desktop Chat draft");
            Require(Uri.UnescapeDataString(url.Substring(url.IndexOf("&prompt=",StringComparison.Ordinal)+8))==text,"Unicode, punctuation, whitespace and newlines round trip without truncation");
            Require(ChatRouting.TryDraftUrl("给 Codex 的草稿","codex",out url,out error)&&url.StartsWith("codex://new?mode=codex&prompt="),"Codex desktop handoff has explicit Codex mode");
            foreach(string empty in new[]{null,""," \r\n\t "})Require(!ChatRouting.TryDraftUrl(empty,"chatgpt",out url,out error)&&url==null&&!String.IsNullOrEmpty(error),"reject empty drafts");
            Require(!ChatRouting.TryDraftUrl(new string('a',ChatRouting.MaxDraftUrlLength+1),"chatgpt",out url,out error)&&url==null&&error==ChatRouting.LongDraftMessage,"oversize original returns explicit copy fallback");
            Require(!ChatRouting.TryDraftUrl(new string('中',1000),"chatgpt",out url,out error)&&url==null&&error==ChatRouting.LongDraftMessage,"oversize encoded URL is rejected instead of silently shortened");
            Require(ChatRouting.TryDraftUrl(new string('a',2000),"chatgpt",out url,out error)&&Uri.UnescapeDataString(url.Substring(url.IndexOf("&prompt=",StringComparison.Ordinal)+8)).Length==2000,"accepted long draft retains every character");
            File.WriteAllText(Path.Combine(Program.DataRoot,"verification-chat-routing-checks.txt"),"PASS: visible target routing helpers; explicit chat/codex mode; Unicode and whitespace round trip; empty and long draft rejection. Pure offline tests only; no desktop launch or chat messages.",Encoding.UTF8);
        }
    }
}
