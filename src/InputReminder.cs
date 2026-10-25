using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace PetBridge {
    sealed class InputEvent {
        public string ThreadId,RequestId,TurnId,Link;public bool Resolved;
        public string Key{get{return "input:"+ThreadId+":"+RequestId;}}
    }
    // Read structured lifecycle records only. Ordinary prose mentioning questions is ignored.
    sealed class LocalInputReducer {
        sealed class Request {public InputEvent Event;public bool Async;public int Count;public HashSet<int> Answers=new HashSet<int>();}
        readonly Dictionary<string,Request> pending=new Dictionary<string,Request>();readonly HashSet<string> seen=new HashSet<string>();
        readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=32*1024*1024};
        public int Count{get{return pending.Count;}}
        static string ToolName(string name){return (name??"").Split('.').Last();}
        static string ThreadId(string path){var m=System.Text.RegularExpressions.Regex.Match(Path.GetFileNameWithoutExtension(path),@"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})$");return m.Success?m.Value:"";}
        void Resolve(string key,List<InputEvent> events){Request r;if(!pending.TryGetValue(key,out r))return;pending.Remove(key);events.Add(new InputEvent{ThreadId=r.Event.ThreadId,RequestId=r.Event.RequestId,TurnId=r.Event.TurnId,Link=r.Event.Link,Resolved=true});}
        void Reply(string text,string thread,List<InputEvent> events){
            const string start="<send_user_message_question_reply>",end="</send_user_message_question_reply>";
            if(text==null||!text.TrimStart().StartsWith(start,StringComparison.Ordinal))return;int a=text.IndexOf(start)+start.Length,b=text.IndexOf(end,a);if(b<a)return;
            var replies=json.DeserializeObject(text.Substring(a,b-a)) as IEnumerable;if(replies==null)return;
            foreach(var value in replies){var reply=value as Dictionary<string,object>;if(reply==null||!reply.ContainsKey("answer"))continue;var id=json.DeserializeObject(AppState.Text(reply,"questionItemId")) as object[];if(id==null||id.Length<3)continue;string key=thread+":"+Convert.ToString(id[1]);Request request;if(!pending.TryGetValue(key,out request))continue;int index;if(!Int32.TryParse(Convert.ToString(id[2]),out index)||index<0||index>=request.Count)continue;request.Answers.Add(index);if(request.Answers.Count>=request.Count)Resolve(key,events);}
        }
        void Add(string thread,string id,int count,bool async,List<InputEvent> events){if(thread==""||id==""||count<=0)return;string key=thread+":"+id;if(!seen.Add(key))return;var evt=new InputEvent{ThreadId=thread,RequestId=id,Link="codex://threads/"+thread};pending[key]=new Request{Event=evt,Async=async,Count=count};events.Add(evt);}
        public List<InputEvent> Apply(string line,string path){var events=new List<InputEvent>();
            if(line.IndexOf("request_user_input",StringComparison.Ordinal)<0&&line.IndexOf("send_user_message_question_reply",StringComparison.Ordinal)<0&&line.IndexOf("function_call_output",StringComparison.Ordinal)<0&&line.IndexOf("\"questions\"",StringComparison.Ordinal)<0&&line.IndexOf("turn_aborted",StringComparison.Ordinal)<0&&line.IndexOf("task_failed",StringComparison.Ordinal)<0)return events;
            try{string thread=ThreadId(path);var o=json.DeserializeObject(line) as Dictionary<string,object>;if(o==null||!o.ContainsKey("payload"))return events;var p=o["payload"] as Dictionary<string,object>;if(p==null)return events;string record=AppState.Text(o,"type"),type=AppState.Text(p,"type");
                if(record=="response_item"&&(type=="function_call"||type=="custom_tool_call")){
                    string name=ToolName(AppState.Text(p,"name"));if(name!="request_user_input"&&name!="request_user_input_async")return events;
                    string args=AppState.Text(p,type=="custom_tool_call"?"input":"arguments");var input=json.DeserializeObject(args) as Dictionary<string,object>;object questions;if(input!=null&&input.TryGetValue("questions",out questions)&&questions is object[])Add(thread,AppState.Text(p,"call_id"),((object[])questions).Length,name.EndsWith("_async"),events);
                }else if(record=="response_item"&&(type=="function_call_output"||type=="custom_tool_call_output")){
                    string key=thread+":"+AppState.Text(p,"call_id");Request r;if(pending.TryGetValue(key,out r)){if(!r.Async)Resolve(key,events);else{string output=AppState.Text(p,"output");try{var result=json.DeserializeObject(output) as Dictionary<string,object>;object accepted;if(result!=null&&result.TryGetValue("accepted",out accepted)&&accepted is bool&&!(bool)accepted)Resolve(key,events);}catch{}}}
                }else if(record=="response_item"&&type=="message"&&AppState.Text(p,"role")=="user"){
                    object content;if(p.TryGetValue("content",out content)&&content is IEnumerable)foreach(var item in (IEnumerable)content)Reply(AppState.Text(item as Dictionary<string,object>,"text"),thread,events);
                }else if(record=="event_msg"&&type=="item_completed"){
                    object value;if(p.TryGetValue("item",out value)){var item=value as Dictionary<string,object>;object q;if(item!=null&&AppState.Text(item,"type")=="AgentMessage"&&item.TryGetValue("questions",out q)&&q is object[])Add(thread,AppState.Text(item,"id"),((object[])q).Length,AppState.Text(item,"delivery")=="async",events);}
                }else if(record=="event_msg"&&type=="user_message")Reply(AppState.Text(p,"message"),thread,events);
                else if(record=="event_msg"&&(type=="turn_aborted"||type=="task_failed"))foreach(var key in pending.Keys.Where(k=>k.StartsWith(thread+":",StringComparison.Ordinal)).ToArray())Resolve(key,events);
            }catch{}return events;
        }
    }
    sealed class AppInputReducer {
        readonly Dictionary<string,string> pending=new Dictionary<string,string>();readonly Dictionary<string,int> episodes=new Dictionary<string,int>();
        public int Count{get{return pending.Count;}}
        static bool? Waiting(Dictionary<string,object> page){
            object value;var thread=page.TryGetValue("thread",out value)?value as Dictionary<string,object>:null;var status=thread!=null&&thread.TryGetValue("status",out value)?value as Dictionary<string,object>:null;
            if(status==null)return null;string type=AppState.Text(status,"type");if(type=="needsInput"||type=="waitingForUserInput"||type=="waitingOnUserInput")return true;
            if(type=="notLoaded"||type=="systemError"||type=="active"&&!status.ContainsKey("activeFlags"))return null;
            object flags;if(status.TryGetValue("activeFlags",out flags)&&flags is IEnumerable)foreach(var flag in (IEnumerable)flags)if(Convert.ToString(flag)=="waitingOnUserInput")return true;return false;
        }
        public InputEvent Observe(AppThreadState t,Dictionary<string,object> page){
            bool? state=Waiting(page);if(!state.HasValue)return null;bool waiting=state.Value;string old;pending.TryGetValue(t.Key,out old);if(waiting&&old!=null||!waiting&&old==null)return null;
            string id=old;if(waiting){int episode;episodes.TryGetValue(t.Key,out episode);episodes[t.Key]=++episode;id="app-wait-"+episode;pending[t.Key]=id;}else pending.Remove(t.Key);
            return new InputEvent{ThreadId=t.Id,RequestId=id,Resolved=!waiting,Link=t.Kind=="chatgpt"?"https://chatgpt.com/c/"+Uri.EscapeDataString(t.Id):"codex://threads/"+Uri.EscapeDataString(t.Id)};
        }
    }
    sealed partial class PetForm {
        readonly HashSet<string> pendingInputs=new HashSet<string>();int inputNotices;Action currentNoticeShow;
        void OnInputChanged(InputEvent evt){
            if(evt.Resolved){pendingInputs.Remove(evt.Key);pendingNotices.RemoveAll(n=>n.Key==evt.Key);if(notice!=null&&notice.Visible&&noticeKey==evt.Key){notice.Hide();NoticeDismissed();}DumpState();return;}
            if(!pendingInputs.Add(evt.Key)||!settings.InputReminders)return;inputNotices++;
            ShowDecision(evt.Key,new QuotaDecision{Kind="input",Title="有个问题等你回答",Message="选择选项或输入回复 · 点击回到桌面版"},evt.Link);DumpState();
        }
        public void PreviewInputReminder(){ShowDecision("input-test",new QuotaDecision{Kind="input",Title="等待回答提醒测试",Message="测试成功 · 实际提醒可点击返回对话"},"");}
    }
}
