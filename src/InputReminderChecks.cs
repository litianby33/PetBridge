using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace PetBridge {
    static class InputReminderChecks {
        const string PathName="rollout-2026-10-21T00-00-00-11111111-1111-4111-8111-111111111111.jsonl";
        static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
        static Dictionary<string,object> Parse(string s){return Json.DeserializeObject(s) as Dictionary<string,object>;}
        static void Require(bool ok,string label){if(!ok)throw new Exception("Input reminder: "+label);}
        static string Call(string name,string id,int count){var qs=new object[count];for(int i=0;i<count;i++)qs[i]=new{title="Test question",options=new[]{"A","B"}};return Json.Serialize(new{type="response_item",payload=new{type="function_call",name=name,call_id=id,arguments=Json.Serialize(new{questions=qs})}});}
        static string Reply(string id,int index){string text="<send_user_message_question_reply>\n"+Json.Serialize(new[]{new{questionItemId=Json.Serialize(new object[]{"request_user_input_async",id,index}),answer="A"}})+"\n</send_user_message_question_reply>";return Json.Serialize(new{type="response_item",payload=new{type="message",role="user",content=new[]{new{type="input_text",text=text}}}});}
        public static void ProbeHistory(string path){var reducer=new LocalInputReducer();int requested=0,resolved=0;using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){long start=Math.Max(0,f.Length-24*1024*1024);f.Position=start;using(var reader=new StreamReader(f,System.Text.Encoding.UTF8)){if(start>0)reader.ReadLine();string line;while((line=reader.ReadLine())!=null)foreach(var evt in reducer.Apply(line,path)){if(evt.Resolved)resolved++;else requested++;}}}File.WriteAllText(System.IO.Path.Combine(Program.DataRoot,"verification-input-history.json"),Json.Serialize(new{requested=requested,resolved=resolved,pending=reducer.Count,mode="offline parse only, no notifications replayed",checkedAt=DateTimeOffset.Now.ToString("o")}));}
        public static void Run(){
            var state=new LocalInputReducer();var a=state.Apply(Call("request_user_input_async","ask1",2),PathName);Require(a.Count==1&&!a[0].Resolved&&a[0].Link.StartsWith("codex://threads/")&&state.Count==1,"asynchronous question opens desktop-linked reminder");
            Require(state.Apply(Call("request_user_input_async","ask1",2),PathName).Count==0,"duplicate question only once");
            string accepted=Json.Serialize(new{type="response_item",payload=new{type="function_call_output",call_id="ask1",output="{\"accepted\":true}"}});Require(state.Apply(accepted,PathName).Count==0&&state.Count==1,"accepted async tool is still waiting for human");
            Require(state.Apply(Reply("ask1",0),PathName).Count==0&&state.Count==1,"partial multi-question answer remains pending");a=state.Apply(Reply("ask1",1),PathName);Require(a.Count==1&&a[0].Resolved&&state.Count==0,"all answers resolve reminder");Require(state.Apply(Reply("ask1",1),PathName).Count==0,"response_item and event duplicate answer ignored");
            state.Apply(Call("functions.request_user_input","sync",1),PathName);a=state.Apply(Json.Serialize(new{type="response_item",payload=new{type="function_call_output",call_id="sync",output="{}"}}),PathName);Require(a.Count==1&&a[0].Resolved,"blocking question resolves with tool result");
            state.Apply(Call("request_user_input_async","rejected",1),PathName);a=state.Apply(accepted.Replace("ask1","rejected").Replace("true","false"),PathName);Require(a.Count==1&&a[0].Resolved,"rejected question is cleared");
            Require(state.Apply(Call("exec","ordinary command",1),PathName).Count==0,"ordinary tool does not create questions");
            Require(state.Apply("{\"type\":\"response_item\",\"payload\":{\"type\":\"message\",\"role\":\"assistant\",\"content\":[{\"text\":\"request_user_input questions\"}]}}",PathName).Count==0,"prose and quoted tool names never trigger");
            state.Apply(Call("request_user_input_async","async",1),PathName);Require(state.Apply("{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_complete\"}}",PathName).Count==0&&state.Count==1,"nonblocking question is not confused with task completion");a=state.Apply("{\"type\":\"event_msg\",\"payload\":{\"type\":\"turn_aborted\"}}",PathName);Require(a.Count==1&&a[0].Resolved,"aborted task clears outstanding prompt");
            var app=new AppInputReducer();var t=new AppThreadState{Id="chat1",Kind="chatgpt",Host=""};var waiting=Parse("{\"thread\":{\"status\":{\"type\":\"active\",\"activeFlags\":[\"waitingOnUserInput\"]}}}");a=new List<InputEvent>();var evt=app.Observe(t,waiting);Require(evt!=null&&!evt.Resolved&&evt.Link=="https://chatgpt.com/c/chat1","Work input status uses ordinary-chat desktop navigation route");Require(app.Observe(t,waiting)==null,"polling waiting status does not repeat");
            Require(app.Observe(t,Parse("{}"))==null&&app.Count==1,"missing state never resolves a prompt");
            evt=app.Observe(t,Parse("{\"thread\":{\"status\":{\"type\":\"active\",\"activeFlags\":[]}}}"));Require(evt!=null&&evt.Resolved,"resume resolves waiting");evt=app.Observe(t,waiting);Require(evt!=null&&!evt.Resolved,"next question in same turn can remind again");
            File.WriteAllText(System.IO.Path.Combine(Program.DataRoot,"verification-input-reminders.txt"),"PASS: actual structured request schemas; async accepted remains pending; per-question dedup; partial and complete answers; blocking result; rejected and aborted prompts; ordinary prose ignored; no false task completion; app waiting status, resume and next question; missing state preservation.");
        }
    }
}
