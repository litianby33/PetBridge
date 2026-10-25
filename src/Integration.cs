using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PetBridge {
    sealed class Quota {
        public double Remaining;public long ResetsAt;public int WindowMinutes;
        public string ResetText {get {if(ResetsAt<=0)return "重置时间暂不可用";var reset=DateTimeOffset.FromUnixTimeSeconds(ResetsAt).ToLocalTime();return (WindowMinutes<=300?reset.ToString("HH:mm"):reset.ToString("M月d日 HH:mm"))+" 重置";}}
        public static Quota Parse(object value){var o=value as Dictionary<string,object>;if(o==null||!o.ContainsKey("usedPercent")||o["usedPercent"]==null)return null;return new Quota{Remaining=Math.Max(0,Math.Min(100,100-Convert.ToDouble(o["usedPercent"]))),ResetsAt=o.ContainsKey("resetsAt")&&o["resetsAt"]!=null?Convert.ToInt64(o["resetsAt"]):0,WindowMinutes=o.ContainsKey("windowDurationMins")?Convert.ToInt32(o["windowDurationMins"]):0};}
    }
    sealed class UsageSnapshot {public Quota Primary,Secondary;}
    sealed class UsageService : IDisposable {
        public UsageSnapshot Snapshot;public DateTimeOffset UpdatedAt;public bool LastError,Refreshing;public event Action Changed;
        readonly object freshLock=new object();readonly List<Action<UsageSnapshot>> fresh=new List<Action<UsageSnapshot>>();readonly System.Threading.Timer timer;readonly Func<UsageSnapshot> testReader;int working;bool disposed;Process active;
        public UsageService(Func<UsageSnapshot> reader=null){testReader=reader;timer=new System.Threading.Timer(delegate{Refresh();},null,Program.Offline?Timeout.Infinite:2000,60000);}
        void Raise(){var h=Changed;if(h!=null)h();}
        public void RefreshFresh(Action<UsageSnapshot> callback){if(disposed)return;lock(freshLock)fresh.Add(callback);Refresh();}
        public void Refresh(){if(Program.Offline&&testReader==null)return;if(disposed||Interlocked.CompareExchange(ref working,1,0)!=0)return;
            Action<UsageSnapshot>[] callbacks;lock(freshLock){callbacks=fresh.ToArray();fresh.Clear();}
            Refreshing=true;Raise();Task.Run(delegate{UsageSnapshot result=null;try{result=Read();if(result.Primary==null&&result.Secondary==null)throw new Exception("No quota data");Snapshot=result;UpdatedAt=DateTimeOffset.Now;LastError=false;}catch{result=null;LastError=true;}finally{Refreshing=false;Interlocked.Exchange(ref working,0);if(!disposed){foreach(var callback in callbacks)try{callback(result);}catch{}Raise();bool again;lock(freshLock)again=fresh.Count>0;if(again)Refresh();}}});
        }
        public static UsageSnapshot Parse(Dictionary<string,object> result){Dictionary<string,object> bucket=null;object v;if(result.TryGetValue("rateLimitsByLimitId",out v)){var all=v as Dictionary<string,object>;if(all!=null&&all.ContainsKey("codex"))bucket=all["codex"] as Dictionary<string,object>;}if(bucket==null&&result.TryGetValue("rateLimits",out v))bucket=v as Dictionary<string,object>;if(bucket==null)throw new Exception("No bucket");return new UsageSnapshot{Primary=Quota.Parse(bucket.ContainsKey("primary")?bucket["primary"]:null),Secondary=Quota.Parse(bucket.ContainsKey("secondary")?bucket["secondary"]:null)};}
        UsageSnapshot Read(){if(testReader!=null)return testReader();var settings=PetSettings.Load();var info=new ProcessStartInfo(ChatBackend.FindCli(settings.CliPath),"app-server --stdio"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=Program.DataRoot};
            using(var p=new Process{StartInfo=info}){active=p;p.Start();p.ErrorDataReceived+=delegate{};p.BeginErrorReadLine();var read=Task.Run(delegate{Send(p,new{method="initialize",id=1,@params=new{clientInfo=new{name="petbridge_desktop",title="PetBridge desktop pet",version="1.3.1"}}});string line;while((line=p.StandardOutput.ReadLine())!=null){Dictionary<string,object> o;try{o=new JavaScriptSerializer().DeserializeObject(line) as Dictionary<string,object>;}catch{continue;}if(o==null||!o.ContainsKey("id"))continue;int id=Convert.ToInt32(o["id"]);if(id==1){if(o.ContainsKey("error"))throw new Exception("Initialize failed");Send(p,new{method="initialized",@params=new{}});Send(p,new{method="account/rateLimits/read",id=2});}else if(id==2){if(!o.ContainsKey("result"))throw new Exception("Quota read failed");return Parse((Dictionary<string,object>)o["result"]);}}throw new Exception("Disconnected");});
                try{if(!read.Wait(25000))throw new Exception("Quota read timed out");return read.Result;}finally{try{if(!p.HasExited)p.Kill();}catch{}active=null;}
            }
        }
        static void Send(Process p,object message){byte[] b=Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(message)+"\n");p.StandardInput.BaseStream.Write(b,0,b.Length);p.StandardInput.BaseStream.Flush();}
        public void Dispose(){disposed=true;timer.Dispose();try{if(active!=null&&!active.HasExited)active.Kill();}catch{}}
    }
    sealed class CompletionEvent {public string ThreadId,TurnId,Status="task_complete",Source="codex",Link="";public bool UsesCodexQuota=true;}
    sealed class CompletionWatcher : IDisposable {
        sealed class Tail {public long Offset;public bool Child;}
        readonly Dictionary<string,Tail> files=new Dictionary<string,Tail>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> seen=new HashSet<string>();
        readonly TaskActivityReducer activity=new TaskActivityReducer();readonly Action<int> onActivity;
        public int ActiveCount {get{return activity.Count;}}
        readonly LocalInputReducer questions=new LocalInputReducer();readonly Action<InputEvent> onInput;
        public int PendingInputs{get{return questions.Count;}}
        readonly string root;readonly Action<CompletionEvent> onComplete;readonly System.Threading.Timer timer;int working;bool disposed;
        public int Delivered;
        public CompletionWatcher(string path,Action<CompletionEvent> callback,Action<int> changed=null,Action<InputEvent> input=null){onInput=input;root=path;onComplete=callback;onActivity=changed;if(Directory.Exists(root))foreach(string f in Directory.GetFiles(root,"rollout-*.jsonl",SearchOption.AllDirectories)){var state=NewTail(f,true);files[f]=state;if(!state.Child)activity.Bootstrap(f);}if(onActivity!=null)onActivity(activity.Count);timer=new System.Threading.Timer(delegate{Poll();},null,1000,1000);}
        static Tail NewTail(string path,bool skipOld){var state=new Tail{Offset=skipOld?new FileInfo(path).Length:0};try{using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var r=new StreamReader(f,Encoding.UTF8)){string first=r.ReadLine();var o=new JavaScriptSerializer().DeserializeObject(first) as Dictionary<string,object>;var p=o!=null&&o.ContainsKey("payload")?o["payload"] as Dictionary<string,object>:null;state.Child=p!=null&&((p.ContainsKey("agent_path")&&p["agent_path"]!=null)||(p.ContainsKey("source")&&!(p["source"] is string)));}}catch{}return state;}
        public static CompletionEvent Parse(string line,string path){if(line.IndexOf("task_complete",StringComparison.Ordinal)<0&&line.IndexOf("turn_aborted",StringComparison.Ordinal)<0&&line.IndexOf("task_failed",StringComparison.Ordinal)<0)return null;var o=new JavaScriptSerializer{MaxJsonLength=32*1024*1024}.DeserializeObject(line) as Dictionary<string,object>;if(o==null||!o.ContainsKey("type")||(string)o["type"]!="event_msg")return null;var p=o["payload"] as Dictionary<string,object>;if(p==null||!p.ContainsKey("type"))return null;string turnId=p.ContainsKey("turn_id")?Convert.ToString(p["turn_id"]):"";if(String.IsNullOrWhiteSpace(turnId))return null;string status=Convert.ToString(p["type"]);if(status!="task_complete"&&status!="turn_aborted"&&status!="task_failed")return null;var match=System.Text.RegularExpressions.Regex.Match(Path.GetFileNameWithoutExtension(path),@"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})$",System.Text.RegularExpressions.RegexOptions.IgnoreCase);if(!match.Success)return null;return new CompletionEvent{Status=status,ThreadId=match.Value,TurnId=p.ContainsKey("turn_id")?Convert.ToString(p["turn_id"]):""};}
        void Poll(){if(disposed||Interlocked.CompareExchange(ref working,1,0)!=0)return;try{if(!Directory.Exists(root))return;foreach(string path in Directory.GetFiles(root,"rollout-*.jsonl",SearchOption.AllDirectories)){if(disposed)break;Tail state;if(!files.TryGetValue(path,out state)){state=NewTail(path,false);files[path]=state;}if(state.Child)continue;try{using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){if(f.Length<state.Offset)state.Offset=0;long available=f.Length-state.Offset;if(available<=0)continue;int count=(int)Math.Min(available,16*1024*1024);byte[] b=new byte[count];f.Position=state.Offset;int read=f.Read(b,0,count),end=Array.LastIndexOf(b,(byte)10,read-1,read);if(end<0)continue;state.Offset+=end+1;string text=Encoding.UTF8.GetString(b,0,end+1);foreach(string line in text.Split('\n')){if(onInput!=null)foreach(var question in questions.Apply(line,path))onInput(question);if(activity.Apply(line,path)&&onActivity!=null)onActivity(activity.Count);CompletionEvent evt;try{evt=Parse(line,path);}catch{continue;}if(evt==null)continue;string key=evt.ThreadId+":"+evt.TurnId;if(!seen.Add(key))continue;if(seen.Count>300)seen.Clear();Delivered++;onComplete(evt);}}}catch(IOException){}catch(UnauthorizedAccessException){}}}catch{}finally{Interlocked.Exchange(ref working,0);}}
        public void Dispose(){disposed=true;timer.Dispose();}
    }
    static class PetIpc {
        public static string Name {get{return Program.Qa?"petbridge-v1-qa":"petbridge-v1";}}
        public static bool Send(string message){try{using(var p=new NamedPipeClientStream(".",Name,PipeDirection.Out)){p.Connect(500);using(var w=new StreamWriter(p,new UTF8Encoding(false))){w.WriteLine(message);w.Flush();}}return true;}catch{return false;}}
        public static void Listen(PetForm form){Task.Run(delegate{while(!form.IsDisposed){try{using(var p=new NamedPipeServerStream(Name,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous)){p.WaitForConnection();using(var r=new StreamReader(p,Encoding.UTF8)){string command=r.ReadLine();if(!form.IsDisposed&&form.IsHandleCreated)form.BeginInvoke(new Action(delegate{form.HandleCommand(command);}));}}}catch{if(form.IsDisposed)return;Thread.Sleep(300);}}});}
    }
    sealed class LaunchWatcher : ApplicationContext {
        readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();bool launched;
        public LaunchWatcher(){timer.Interval=2500;timer.Tick+=delegate{Check();};timer.Start();Check();}
        void Check(){try{var settings=PetSettings.Load();if(!settings.AutoStartWithChatGPT){ExitThread();return;}bool running=Process.GetProcessesByName("ChatGPT").Length>0;if(!running){launched=false;return;}if(!launched){launched=true;if(!Program.PetWindowExists())Process.Start(new ProcessStartInfo(Path.Combine(Program.Root,"PetBridge.exe")){WorkingDirectory=Program.DataRoot,UseShellExecute=false,WindowStyle=ProcessWindowStyle.Normal});}}catch{}}
        protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
    }
}
