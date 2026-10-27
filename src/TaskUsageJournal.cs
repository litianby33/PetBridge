using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Web.Script.Serialization;
namespace PetBridge {
    // Separate numeric sampling journal. It never changes completion/reminder ledgers.
    sealed class TaskUsageEntry {
        public string ThreadId="",TurnId="",State="tracking";
        public DateTimeOffset StartedAt,EndedAt;
        public double? WeekBefore,WeekAfter,WeekDelta;
        public double? FiveBefore,FiveAfter,FiveDelta;public long FiveReset;public string FiveState="missing";
        public long WindowReset;public bool Parallel,MonitorGap;
    }
    sealed class TaskUsageJournal {
        readonly object gate=new object();readonly string file;readonly List<TaskUsageEntry> rows=new List<TaskUsageEntry>();
        double? used,fiveUsed;long reset,fiveReset;DateTimeOffset sampled,fiveSampled;
        public TaskUsageJournal(string path){
            file=path;try{var saved=new JavaScriptSerializer{MaxJsonLength=4*1024*1024}.Deserialize<TaskUsageEntry[]>(File.ReadAllText(file,Encoding.UTF8));if(saved!=null)rows.AddRange(saved);
                foreach(var row in rows)if(row.State=="tracking"){row.MonitorGap=true;row.State="gap";}}catch{}
        }
        public void UpdateAccount(UsageSnapshot value,DateTimeOffset at){
            if(value==null)return;
            lock(gate)foreach(var quota in new[]{value.Primary,value.Secondary}){
                if(quota==null)continue;double amount=100-quota.Remaining;
                if(Double.IsNaN(amount)||Double.IsInfinity(amount)||amount<0||amount>100)continue;
                if(quota.WindowMinutes==300&&at>=fiveSampled){fiveUsed=amount;fiveReset=quota.ResetsAt;fiveSampled=at;}
                else if(quota.WindowMinutes>=10080&&at>=sampled){used=amount;reset=quota.ResetsAt;sampled=at;}
            }
        }
        public void NoteParallel(int active){if(active<=1)return;lock(gate)foreach(var row in rows)if(row.State=="tracking"||row.State=="gap")row.Parallel=true;}
        static object Get(Dictionary<string,object> o,string k){object v;return o!=null&&o.TryGetValue(k,out v)?v:null;}
        static string Text(Dictionary<string,object> o,string k){return Convert.ToString(Get(o,k),CultureInfo.InvariantCulture)??"";}
        static Dictionary<string,object> Obj(object x){return x as Dictionary<string,object>;}
        public void Observe(string line,string path,int activeBefore){
            if(line.IndexOf("token_count",StringComparison.Ordinal)<0&&line.IndexOf("task_started",StringComparison.Ordinal)<0&&line.IndexOf("task_complete",StringComparison.Ordinal)<0&&line.IndexOf("turn_aborted",StringComparison.Ordinal)<0&&line.IndexOf("task_failed",StringComparison.Ordinal)<0)return;
            try{
                var root=Obj(new JavaScriptSerializer{MaxJsonLength=4*1024*1024}.DeserializeObject(line));
                if(Text(root,"type")!="event_msg")return;var p=Obj(Get(root,"payload"));string kind=Text(p,"type");DateTimeOffset at;
                if(!DateTimeOffset.TryParse(Text(root,"timestamp"),CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out at))return;
                lock(gate){
                    if(kind=="token_count"){
                        var limits=Obj(Get(p,"rate_limits"));if(limits==null)return;string limitId=Text(limits,"limit_id");if(limitId!=""&&limitId!="codex")return;
                        foreach(string key in new[]{"primary","secondary"}){
                            var quota=Obj(Get(limits,key));double amount;long window,end;
                            if(quota==null||!Int64.TryParse(Text(quota,"window_minutes"),out window)||(window!=300&&window<10080)||!Int64.TryParse(Text(quota,"resets_at"),out end)||!Double.TryParse(Text(quota,"used_percent"),NumberStyles.Float,CultureInfo.InvariantCulture,out amount)||Double.IsNaN(amount)||Double.IsInfinity(amount)||amount<0||amount>100)continue;
                            if(window==300){if(at>=fiveSampled){fiveUsed=amount;fiveReset=end;fiveSampled=at;}}
                            else if(at>=sampled){used=amount;reset=end;sampled=at;}
                        }return;
                    }
                    string turn=Text(p,"turn_id");var match=System.Text.RegularExpressions.Regex.Match(Path.GetFileNameWithoutExtension(path),@"([0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})$");
                    if(turn==""||!match.Success)return;string id=match.Value;
                    var current=rows.LastOrDefault(r=>r.ThreadId==id&&r.TurnId==turn);
                    if(kind=="task_started"){
                        if(current!=null)return;
                        foreach(var old in rows)if(old.ThreadId==id&&(old.State=="tracking"||old.State=="gap")){old.State="incomplete";old.EndedAt=at;}
                        bool fresh=Fresh(used,sampled,at),fiveFresh=Fresh(fiveUsed,fiveSampled,at);
                        rows.Add(new TaskUsageEntry{ThreadId=id,TurnId=turn,StartedAt=at,WeekBefore=fresh?used:null,WindowReset=fresh?reset:0,FiveBefore=fiveFresh?fiveUsed:null,FiveReset=fiveFresh?fiveReset:0,FiveState="tracking",Parallel=activeBefore>0});
                        if(activeBefore>0)foreach(var row in rows)if(row.State=="tracking")row.Parallel=true;
                        if(rows.Count>2000)rows.RemoveRange(0,rows.Count-2000);Save();return;
                    }
                    if(kind!="task_complete"&&kind!="turn_aborted"&&kind!="task_failed")return;
                    if(current==null||current.EndedAt!=default(DateTimeOffset))return;
                    current.EndedAt=at;current.Parallel|=activeBefore>1;
                    current.WeekAfter=Fresh(used,sampled,at)?used:null;
                    current.FiveAfter=Fresh(fiveUsed,fiveSampled,at)?fiveUsed:null;
                    current.State=Finish(current,current.WeekBefore,current.WeekAfter,current.WindowReset,reset);
                    current.FiveState=Finish(current,current.FiveBefore,current.FiveAfter,current.FiveReset,fiveReset);
                    if(current.State=="sampled")current.WeekDelta=current.WeekAfter.Value-current.WeekBefore.Value;
                    if(current.FiveState=="sampled")current.FiveDelta=current.FiveAfter.Value-current.FiveBefore.Value;
                    Save();
                }
            }catch{}
        }
        static bool Fresh(double? amount,DateTimeOffset sampled,DateTimeOffset at){return amount.HasValue&&(at-sampled).TotalSeconds>=0&&(at-sampled).TotalMinutes<=3;}
        static string Finish(TaskUsageEntry item,double? before,double? after,long startReset,long endReset){
            if(item.MonitorGap)return "gap";if(item.Parallel)return "parallel";
            if(!before.HasValue||!after.HasValue)return "missing";
            if(startReset!=endReset||after.Value<before.Value)return "reset";return "sampled";
        }
        void Save(){try{string folder=Path.GetDirectoryName(file);if(!String.IsNullOrEmpty(folder))Directory.CreateDirectory(folder);string pending=file+".tmp";File.WriteAllText(pending,new JavaScriptSerializer().Serialize(rows),new UTF8Encoding(false));if(File.Exists(file))File.Replace(pending,file,null);else File.Move(pending,file);}catch{}}
        public TaskUsageEntry Latest(string id){lock(gate){var item=rows.LastOrDefault(r=>r.ThreadId==id);return item==null?null:new TaskUsageEntry{ThreadId=item.ThreadId,TurnId=item.TurnId,State=item.State,StartedAt=item.StartedAt,EndedAt=item.EndedAt,WeekBefore=item.WeekBefore,WeekAfter=item.WeekAfter,WeekDelta=item.WeekDelta,WindowReset=item.WindowReset,FiveBefore=item.FiveBefore,FiveAfter=item.FiveAfter,FiveDelta=item.FiveDelta,FiveReset=item.FiveReset,FiveState=item.FiveState,Parallel=item.Parallel,MonitorGap=item.MonitorGap};}}
        public TaskUsageEntry[] Recent(string id,int count){lock(gate)return rows.Where(r=>r.ThreadId==id&&r.EndedAt!=default(DateTimeOffset)).Reverse().Take(count).ToArray();}
        public static string Display(TaskUsageEntry item){return Display(item,false);}
        public static string Display(TaskUsageEntry item,bool five){
            if(item==null)return "等待下一任务开始记录";
            string state=five?item.FiveState:item.State;double? before=five?item.FiveBefore:item.WeekBefore,delta=five?item.FiveDelta:item.WeekDelta;
            if(item.State=="tracking")return before.HasValue?"任务进行中 · 正在记录":"任务进行中 · 起点未采样";
            if(item.MonitorGap||item.State=="gap"||item.State=="incomplete")return "监测中断 · 结束数据不完整";
            if(item.Parallel||state=="parallel")return "存在并行任务 · 无法归属";
            if(state=="reset")return "期间额度重置 · 无法比较";
            if(state!="sampled"||!delta.HasValue)return "缺少起止采样";
            return "~ "+delta.Value.ToString("0.##",CultureInfo.InvariantCulture)+" 个百分点";
        }
        public static string Summary(TaskUsageEntry item){
            return "5小时 "+Compact(Display(item,true))+" · 周 "+Compact(Display(item,false));
        }
        static string Compact(string value){return value.Replace(" 个百分点","%").Replace("缺少起止采样","未采样").Replace("存在并行任务 · 无法归属","并行无法归属").Replace("期间额度重置 · 无法比较","期间重置").Replace("监测中断 · 结束数据不完整","监测中断");}
    }
}
