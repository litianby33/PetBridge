using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
namespace PetBridge {
    // Titles and lifecycle status only. This selector never reads chat bodies.
    sealed class UsageTaskChoice {
        public string Id="",Title="",Status="";public double Updated;
        public bool Running {get{return Status=="active"||Status=="running"||Status=="inProgress"||Status=="in_progress";}}
        public string Caption {get{return (Running?"进行中 · ":"最近 · ")+Title;}}
    }
    static class UsageTaskSelection {
        static string Text(Dictionary<string,object> o,string key){object v;return o!=null&&o.TryGetValue(key,out v)?Convert.ToString(v,CultureInfo.InvariantCulture)??"":"";}
        public static string CleanTitle(string title,string id){var text=new string((title??"").Where(c=>!Char.IsControl(c)).ToArray()).Trim();return text==""?"Codex 对话 · "+id.Substring(Math.Max(0,id.Length-8)):text;}
        public static UsageTaskChoice[] FromApp(Dictionary<string,object> page){
            var choices=new List<UsageTaskChoice>();foreach(string key in new[]{"pinnedThreads","threads"}){
                object value;if(page==null||!page.TryGetValue(key,out value)||!(value is IEnumerable))continue;
                foreach(var item in (IEnumerable)value){var o=item as Dictionary<string,object>;Guid id;
                    if(Text(o,"kind")!="codex"||Text(o,"hostId")!="local"||!Guid.TryParse(Text(o,"id"),out id))continue;
                    string guid=id.ToString();if(choices.Any(c=>c.Id==guid))continue;double updated;Double.TryParse(Text(o,"updatedAt"),NumberStyles.Any,CultureInfo.InvariantCulture,out updated);
                    choices.Add(new UsageTaskChoice{Id=guid,Title=CleanTitle(Text(o,"title"),guid),Status=Text(o,"status"),Updated=updated});
                }
            }return Sort(choices);
        }
        public static UsageTaskChoice[] Sort(IEnumerable<UsageTaskChoice> choices){return choices.OrderByDescending(c=>c.Running).ThenByDescending(c=>c.Updated).ThenBy(c=>c.Id,StringComparer.Ordinal).Take(60).ToArray();}
        public static UsageTaskChoice[] Merge(UsageTaskChoice[] choices,string[] active,string selected,string bound,string home){
            var result=(choices??new UsageTaskChoice[0]).Select(c=>new UsageTaskChoice{Id=c.Id,Title=c.Title,Status=c.Status,Updated=c.Updated}).ToList();
            var running=new HashSet<string>(active??new string[0],StringComparer.OrdinalIgnoreCase);
            foreach(string id in running.Concat(new[]{selected,bound}).Where(id=>!String.IsNullOrEmpty(id)).Distinct(StringComparer.OrdinalIgnoreCase)){
                Guid guid;if(!Guid.TryParse(id,out guid))continue;string canonical=guid.ToString();var item=result.FirstOrDefault(c=>c.Id==canonical);
                if(item==null){item=new UsageTaskChoice{Id=canonical,Title=CleanTitle(ConversationTitle.Read(home,canonical),canonical)};result.Add(item);}
                if(running.Contains(id))item.Status="active";
            }return Sort(result);
        }
        public static string Resolve(string selected,string recent,string bound,bool followRecent){Guid id;if(Guid.TryParse(selected,out id))return id.ToString();return followRecent?recent:bound;}
    }
    static class UsageTaskSelectionChecks {
        public static void Run(){
            string a="11111111-1111-4111-8111-111111111111",b="22222222-2222-4222-8222-222222222222";
            var json=new JavaScriptSerializer();
            var page=json.DeserializeObject(json.Serialize(new{pinnedThreads=new[]{new{id=a,kind="codex",hostId="local",title="空闲对话",status="idle",updatedAt=900}},threads=new object[]{new{id=b,kind="codex",hostId="local",title="正在运行",status="active",updatedAt=10},new{id=b,kind="codex",hostId="local",title="重复条目",status="active",updatedAt=12},new{id=a,kind="chatgpt",hostId="local",title="普通聊天",status="active",updatedAt=1000},new{id=a,kind="codex",hostId="durable",title="云任务",status="active",updatedAt=1000}}})) as Dictionary<string,object>;
            var choices=UsageTaskSelection.FromApp(page);if(choices.Length!=2||choices[0].Id!=b||!choices[0].Running||choices[1].Title!="空闲对话")throw new Exception("Usage task selection filtering/order/dedup");
            if(UsageTaskSelection.Resolve(a,b,b,true)!=a||UsageTaskSelection.Resolve("",b,a,true)!=b||UsageTaskSelection.Resolve("",b,a,false)!=a)throw new Exception("Pinned usage task must not follow other activity");
            var merged=UsageTaskSelection.Merge(choices,new[]{a},a,b,Path.GetTempPath());if(!merged.First(c=>c.Id==a).Running||merged.Length!=2)throw new Exception("Running task overlay");
            if(UsageTaskSelection.CleanTitle("\r\n",a).Length<8)throw new Exception("Missing title fallback");
        }
    }
}
