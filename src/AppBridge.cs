using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace PetBridge {
    // Compatibility adapter to the installed, unmodified app-tools MCP server.
    // Monitoring calls only list_threads/read_thread; navigation is a separate click action.
    // No browser credentials or UI automation.
    sealed class AppToolsReader : IDisposable {
        Process process;int sequence;readonly string origin;
        readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=32*1024*1024};
        public AppToolsReader(string originThread){origin=originThread;}
        static string Latest(string root,string relative){if(!Directory.Exists(root))return null;return Directory.GetDirectories(root).OrderByDescending(Directory.GetLastWriteTimeUtc).Select(d=>Path.Combine(d,relative)).FirstOrDefault(File.Exists);}
        static string PipePath(){string inherited=Environment.GetEnvironmentVariable("CODEX_APP_TOOLS_PIPE_PATH");var paths=Directory.GetFiles(@"\\.\pipe\","codex-app-tools-*");if(!String.IsNullOrEmpty(inherited)&&paths.Any(p=>String.Equals(p,inherited,StringComparison.OrdinalIgnoreCase)))return inherited;return paths.OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).FirstOrDefault();}
        void Start(){
            if(process!=null&&!process.HasExited)return;
            string home=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string server=Latest(Path.Combine(Program.CodexHome,"plugins","cache","openai-bundled","codex-app-tools"),"server.mjs");
            string node=Environment.GetEnvironmentVariable("CODEX_MCP_NODE_PATH");if(String.IsNullOrEmpty(node)||!File.Exists(node))node=Latest(Path.Combine(local,"OpenAI","Codex","runtimes","cua_node"),Path.Combine("bin","node.exe"));
            string pipe=PipePath();if(server==null||node==null||pipe==null||String.IsNullOrEmpty(origin))throw new IOException("App bridge unavailable");
            var info=new ProcessStartInfo(node,"\""+server+"\""){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=Path.GetDirectoryName(server)};
            info.EnvironmentVariables["CODEX_APP_TOOLS_PIPE_PATH"]=pipe;info.EnvironmentVariables["CODEX_APP_TOOLS_DEFERRED"]="0";
            process=new Process{StartInfo=info};process.Start();process.ErrorDataReceived+=delegate{};process.BeginErrorReadLine();
            Request("initialize",new{protocolVersion="2024-11-05",capabilities=new{},clientInfo=new{name="petbridge-status-reader",version="1.3.1"}});
            Send(new{jsonrpc="2.0",method="notifications/initialized"});
        }
        void Send(object value){byte[] bytes=Encoding.UTF8.GetBytes(json.Serialize(value)+"\n");process.StandardInput.BaseStream.Write(bytes,0,bytes.Length);process.StandardInput.BaseStream.Flush();}
        Dictionary<string,object> Request(string method,object args){
            int id=++sequence;Send(new{jsonrpc="2.0",id=id,method=method,@params=args});
            var reader=Task.Run(delegate{string line;while((line=process.StandardOutput.ReadLine())!=null){var msg=json.DeserializeObject(line) as Dictionary<string,object>;if(msg==null||!msg.ContainsKey("id")||Convert.ToString(msg["id"])!=id.ToString())continue;if(msg.ContainsKey("error"))throw new IOException("App bridge request rejected");return msg["result"] as Dictionary<string,object>;}throw new IOException("App bridge disconnected");});
            if(!reader.Wait(12000)){Dispose();throw new IOException("App bridge timed out");}return reader.Result;
        }
        public Dictionary<string,object> Call(string name,object args){
            if(name!="list_threads"&&name!="read_thread")throw new InvalidOperationException("Status reader is read only");
            Start();var meta=new Dictionary<string,object>{{"threadId",origin}};
            return Decode(Request("tools/call",new{name=name,arguments=args,_meta=meta}));
        }
        public bool NavigateToChat(string id){
            Guid guid;if(!Guid.TryParse(id,out guid))return false;Start();var meta=new Dictionary<string,object>{{"threadId",origin}};
            var result=Decode(Request("tools/call",new{name="navigate_to_codex_page",arguments=new{threadId=guid.ToString()},_meta=meta}));
            object navigated;return result.TryGetValue("navigated",out navigated)&&navigated is bool&&(bool)navigated;
        }
        Dictionary<string,object> Decode(Dictionary<string,object> result){
            if(result==null||result.ContainsKey("isError")&&Convert.ToBoolean(result["isError"]))throw new IOException("App status unavailable");
            var content=result.ContainsKey("content")?result["content"] as IEnumerable:null;if(content!=null)foreach(var item in content){var block=item as Dictionary<string,object>;if(block!=null&&AppState.Text(block,"type")=="text"){var value=json.DeserializeObject(AppState.Text(block,"text")) as Dictionary<string,object>;if(value!=null)return value;}}
            throw new IOException("App status format changed");
        }
        public void Dispose(){var p=process;process=null;if(p!=null){try{if(!p.HasExited)p.Kill();}catch{}try{p.Dispose();}catch{}}}
    }
    sealed class AppThreadState {
        public string Id,Kind,Host,Status,Updated;public string Key{get{return Kind+":"+Host+":"+Id;}}
        public bool Running{get{return Status=="active"||Status=="running"||Status=="inProgress";}}
        public bool Supported{get{return Kind=="chatgpt"||Kind=="codex"&&!String.IsNullOrEmpty(Host)&&Host!="local";}}
    }
    static class AppState {
        public static string Text(Dictionary<string,object> o,string key){object v;return o!=null&&o.TryGetValue(key,out v)&&v!=null?Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture):"";}
        public static List<AppThreadState> Threads(Dictionary<string,object> page){var list=new List<AppThreadState>();foreach(string key in new[]{"pinnedThreads","threads"}){object items;if(!page.TryGetValue(key,out items)||!(items is IEnumerable))continue;foreach(var item in (IEnumerable)items){var o=item as Dictionary<string,object>;if(o==null)continue;var t=new AppThreadState{Id=Text(o,"id"),Kind=Text(o,"kind"),Host=Text(o,"hostId"),Status=Text(o,"status"),Updated=Text(o,"updatedAt")};if(t.Id!=""&&t.Supported&&!list.Any(x=>x.Key==t.Key))list.Add(t);}}return list;}
        public static CompletionEvent Terminal(AppThreadState thread,Dictionary<string,object> page,double started){
            object turns;if(!page.TryGetValue("turns",out turns)||!(turns is IEnumerable))return null;
            foreach(var item in (IEnumerable)turns){var turn=item as Dictionary<string,object>;if(turn==null)return null;string status=Text(turn,"status"),id=Text(turn,"id");double ended;Double.TryParse(Text(turn,"completedAt"),System.Globalization.NumberStyles.Any,System.Globalization.CultureInfo.InvariantCulture,out ended);
                // Only explicit terminal states with a fresh timestamp count; idle alone never does.
                string terminal=status=="completed"?"task_complete":status=="failed"?"task_failed":status=="interrupted"||status=="cancelled"?"turn_aborted":"";
                if(terminal==""||id==""||ended<started)return null;
                return new CompletionEvent{Status=terminal,ThreadId=thread.Id,TurnId=id,Source=thread.Kind+":"+thread.Host,UsesCodexQuota=thread.Kind=="codex",Link=thread.Kind=="chatgpt"?"https://chatgpt.com/c/"+Uri.EscapeDataString(thread.Id):"codex://threads/"+Uri.EscapeDataString(thread.Id)};
            }return null;
        }
    }
    sealed class AppChatWatcher : IDisposable {
        readonly Func<AppThreadState,bool> sourceEnabled;AppThreadState[] snapshot=new AppThreadState[0];
        readonly AppInputReducer inputs=new AppInputReducer();readonly Action<InputEvent> onInput;
        readonly AppToolsReader reader;readonly Func<string,object,Dictionary<string,object>> call;readonly System.Threading.Timer timer;readonly Action<int> activity;readonly Action<CompletionEvent> complete;readonly Action changed;
        readonly Dictionary<string,AppThreadState> previous=new Dictionary<string,AppThreadState>();readonly Dictionary<string,AppThreadState> pending=new Dictionary<string,AppThreadState>();readonly HashSet<string> delivered=new HashSet<string>();
        readonly double started=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0;int working;bool baseline,disposed;DateTime lastGood=DateTime.MinValue;
        public UsageTaskChoice[] UsageChoices=new UsageTaskChoice[0];public bool Connected;public int ActiveCount,KnownChats,KnownRemote,Delivered;public string State="connecting";
        public AppChatWatcher(string origin,Action<int> active,Action<CompletionEvent> ended,Action update,Func<string,object,Dictionary<string,object>> testCall=null,Action<InputEvent> input=null,Func<AppThreadState,bool> enabled=null){sourceEnabled=enabled??(t=>true);onInput=input;reader=new AppToolsReader(origin);call=testCall??reader.Call;activity=active;complete=ended;changed=update;if(testCall==null)timer=new System.Threading.Timer(delegate{Poll();},null,100,2500);}
        public void PollOnceForTest(){Poll();}
        public void RefreshPolicy(){SetCount(snapshot.Count(t=>t.Running&&sourceEnabled(t)));}
        void SetCount(int count){if(count==ActiveCount)return;ActiveCount=count;activity(count);}
        void Poll(){if(disposed||Interlocked.CompareExchange(ref working,1,0)!=0)return;try{
            var page=call("list_threads",new{limit=50});var list=AppState.Threads(page);UsageChoices=UsageTaskSelection.FromApp(page);bool unavailable=false;object missing;if(page.TryGetValue("unavailableSources",out missing)&&missing is IEnumerable)foreach(var value in (IEnumerable)missing)if(Convert.ToString(value)=="chatgpt")unavailable=true;
            KnownChats=list.Count(t=>t.Kind=="chatgpt");KnownRemote=list.Count-KnownChats;Connected=!unavailable;State=unavailable?"chatgpt-unavailable":"connected";lastGood=DateTime.UtcNow;
            snapshot=list.ToArray();RefreshPolicy();
            foreach(var t in list){AppThreadState old;if(baseline&&(!previous.TryGetValue(t.Key,out old)||old.Status!=t.Status||old.Updated!=t.Updated))pending[t.Key]=t;previous[t.Key]=t;}
            baseline=true;
            if(onInput!=null)foreach(var t in list.Where(t=>t.Running||t.Status=="needsInput"||t.Status=="waitingForUserInput"||t.Status=="waitingOnUserInput").Take(8))pending[t.Key]=t;
            // Read only the newest turn of changed chats. Never save conversation contents.
            foreach(var entry in pending.ToArray().Take(8)){var t=entry.Value;try{object args=t.Kind=="codex"?(object)new{threadId=t.Id,hostId=t.Host,turnLimit=1,includeOutputs=false,maxOutputCharsPerItem=128}:new{threadId=t.Id,turnLimit=1,includeOutputs=false,maxOutputCharsPerItem=128};var detail=call("read_thread",args);if(onInput!=null){var question=inputs.Observe(t,detail);if(question!=null)onInput(question);}var evt=AppState.Terminal(t,detail,started);bool uncertain=false;object turns;if(detail.TryGetValue("turns",out turns)&&turns is IEnumerable)foreach(var item in (IEnumerable)turns){var turn=item as Dictionary<string,object>;string status=AppState.Text(turn,"status");uncertain=status=="inProgress"||status=="in_progress"||status=="completed"&&AppState.Text(turn,"completedAt")=="";break;}if(t.Running||!uncertain)pending.Remove(entry.Key);if(evt!=null&&delivered.Add(t.Key+":"+evt.TurnId)){if(sourceEnabled(t)){Delivered++;complete(evt);}}}catch{}}
            if(delivered.Count>2000){delivered.Clear();}if(previous.Count>400){var keep=new HashSet<string>(list.Select(t=>t.Key));foreach(string k in previous.Keys.ToArray())if(!keep.Contains(k))previous.Remove(k);}
            changed();
        }catch{Connected=false;State="disconnected";reader.Dispose();if((DateTime.UtcNow-lastGood).TotalSeconds>15)SetCount(0);changed();}finally{Interlocked.Exchange(ref working,0);}}
        public void Dispose(){disposed=true;if(timer!=null)timer.Dispose();reader.Dispose();}
        public static void Probe(string origin){using(var reader=new AppToolsReader(origin)){var page=reader.Call("list_threads",new{limit=50});var list=AppState.Threads(page);var inspected=new List<object>();foreach(var t in list.GroupBy(x=>x.Kind).Select(x=>x.First())){object args=t.Kind=="codex"?(object)new{threadId=t.Id,hostId=t.Host,turnLimit=1,includeOutputs=false,maxOutputCharsPerItem=128}:new{threadId=t.Id,turnLimit=1,includeOutputs=false,maxOutputCharsPerItem=128};var detail=reader.Call("read_thread",args);object turns;var statuses=new List<string>();if(detail.TryGetValue("turns",out turns)&&turns is IEnumerable)foreach(var turn in (IEnumerable)turns)statuses.Add(AppState.Text(turn as Dictionary<string,object>,"status"));inspected.Add(new{source=t.Kind,latestTurnStatus=statuses});}File.WriteAllText(Path.Combine(Program.DataRoot,"verification-app-bridge-live.json"),new JavaScriptSerializer().Serialize(new{ok=true,method="installed app-tools MCP, read-only",checkedAt=DateTimeOffset.Now.ToString("o"),inspected=inspected,chats=list.Count(t=>t.Kind=="chatgpt"),remoteWork=list.Count(t=>t.Kind=="codex"),active=list.Count(t=>t.Running),unavailableSources=page.ContainsKey("unavailableSources")?page["unavailableSources"]:null,states=list.Select(t=>new{source=t.Kind,host=t.Host,status=t.Status})}),Encoding.UTF8);}}
    }
}

