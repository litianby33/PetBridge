using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace PetBridge {
    // One terminal receipt per logical turn, shared by all notification sources.
    // Keep it across restarts; no conversation content or quota values are saved here.
    sealed class TaskEndLedger {
        readonly string path;readonly object gate=new object();readonly Queue<string> order=new Queue<string>();readonly HashSet<string> seen=new HashSet<string>(StringComparer.Ordinal);
        public TaskEndLedger(string file){path=file;if(String.IsNullOrEmpty(path))return;try{var keys=new JavaScriptSerializer().Deserialize<string[]>(File.ReadAllText(path,Encoding.UTF8));if(keys!=null)foreach(string key in keys)if(!String.IsNullOrEmpty(key)&&seen.Add(key))order.Enqueue(key);Trim();}catch{}}
        void Trim(){while(order.Count>4096)seen.Remove(order.Dequeue());}
        static string Key(CompletionEvent evt){
            if(evt==null||String.IsNullOrWhiteSpace(evt.TurnId))return null;
            if(evt.Status!="task_complete"&&evt.Status!="task_failed"&&evt.Status!="turn_aborted"&&evt.Status!="local")return null;
            if(String.IsNullOrWhiteSpace(evt.ThreadId)&&evt.Status!="local")return null;
            return (String.IsNullOrWhiteSpace(evt.ThreadId)?"pet-chat":evt.ThreadId.Trim().ToLowerInvariant())+"|"+evt.TurnId.Trim();
        }
        public bool TryClaim(CompletionEvent evt){string key=Key(evt);if(key==null)return false;lock(gate){if(!seen.Add(key))return false;order.Enqueue(key);Trim();
            if(!String.IsNullOrEmpty(path))try{string temp=path+".tmp";File.WriteAllText(temp,new JavaScriptSerializer().Serialize(order.ToArray()),Encoding.UTF8);if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}catch{}
            return true;
        }}
    }
}
