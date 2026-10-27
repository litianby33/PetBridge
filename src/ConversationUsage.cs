using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PetBridge {
    // Reads numeric telemetry only. Never stores or displays conversation bodies.
    sealed class ConversationUsage {
        public string ThreadId="",Title="",Source="最近操作的本机 Codex",Error="";
        public long? Total,Input,Cached,Output,Context,Window;
        public double? OutputRate; public DateTimeOffset Updated;
    }
    static class ConversationUsageReader {
        static readonly object gate=new object(); static string cachedId,cachedPath;
        public static Dictionary<string,object> Obj(object x){return x as Dictionary<string,object>;}
        static object Get(Dictionary<string,object> x,string key){object v;return x!=null&&x.TryGetValue(key,out v)?v:null;}
        static long? Number(Dictionary<string,object> x,string key){var v=Get(x,key);long n;return v!=null&&Int64.TryParse(Convert.ToString(v,CultureInfo.InvariantCulture),out n)&&n>=0?(long?)n:null;}
        static string Text(Dictionary<string,object> x,string key){return Convert.ToString(Get(x,key),CultureInfo.InvariantCulture)??"";}
        public static string RecentThread(string json){
            var state=Obj(new JavaScriptSerializer{MaxJsonLength=32*1024*1024}.DeserializeObject(json));
            var atom=Obj(Get(state,"electron-persisted-atom-state"));var activity=Obj(Get(atom,"thread-user-activity-times-v1"));
            string result="";long latest=-1;if(activity==null)return result;
            foreach(var item in activity){try{var key=new JavaScriptSerializer().DeserializeObject(item.Key) as object[];long time;
                Guid id;if(key==null||key.Length!=2||Convert.ToString(key[0])!="local"||!Guid.TryParse(Convert.ToString(key[1]),out id)||!Int64.TryParse(Convert.ToString(item.Value),out time))continue;
                if(time>latest){latest=time;result=id.ToString();}}catch{}}
            return result;
        }
        public static string Tail(string path,int count){
            using(var f=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){
                long offset=Math.Max(0,f.Length-count);f.Position=offset;byte[] bytes=new byte[(int)(f.Length-offset)];int read=0,n;
                while(read<bytes.Length&&(n=f.Read(bytes,read,bytes.Length-read))>0)read+=n;
                string text=Encoding.UTF8.GetString(bytes,0,read);if(offset>0){int first=text.IndexOf('\n');text=first<0?"":text.Substring(first+1);}return text;
            }
        }
        static string FindSession(string home,string id){
            lock(gate){if(cachedId==id&&cachedPath!=null&&File.Exists(cachedPath))return cachedPath;
                cachedId=id;cachedPath=null;string root=Path.Combine(home,"sessions");if(Directory.Exists(root))
                    cachedPath=Directory.EnumerateFiles(root,"rollout-*"+id+".jsonl",SearchOption.AllDirectories).FirstOrDefault();
                return cachedPath;}
        }
        public static ConversationUsage Read(string home,string bound,bool followRecent,string selected=null){
            var result=new ConversationUsage{Source=followRecent?"最近操作的本机 Codex":"宠物绑定的 Codex 对话"};
            try{
                Guid selectedId;bool fixedTask=Guid.TryParse(selected,out selectedId);string recent=!fixedTask&&followRecent?RecentThread(File.ReadAllText(Path.Combine(home,".codex-global-state.json"),Encoding.UTF8)):"";
                string id=UsageTaskSelection.Resolve(selected,recent,bound,followRecent);
                Guid parsed;if(!Guid.TryParse(id,out parsed)){result.Error=followRecent?"尚未找到最近操作的本机对话":"尚未绑定本机 Codex 对话";return result;}
                result.ThreadId=parsed.ToString();result.Title="Codex 对话 · "+result.ThreadId.Substring(result.ThreadId.Length-8);
                string path=FindSession(home,result.ThreadId);if(path==null){result.Error="该对话暂无本机 Token 记录";return result;}
                Apply(Tail(path,1024*1024),result);
                string index=Path.Combine(home,"session_index.jsonl");if(File.Exists(index))foreach(string line in Tail(index,512*1024).Split('\n')){
                    if(line.IndexOf(result.ThreadId,StringComparison.Ordinal)<0)continue;
                    try{var entry=Obj(new JavaScriptSerializer().DeserializeObject(line));if(Text(entry,"id")!=result.ThreadId)continue;
                        string title=Text(entry,"thread_name");if(title=="")title=Text(entry,"title");if(title!="")result.Title=title;}catch{}}
                string savedName=ConversationTitle.Read(home,result.ThreadId);if(!String.IsNullOrWhiteSpace(savedName))result.Title=savedName;
                if(result.Total==null)result.Error="等待这段对话的用量记录";
            }catch(UnauthorizedAccessException){result.Error="暂时无法读取本机用量记录";}
             catch(IOException){result.Error="用量记录正在更新，请稍候";}
             catch{result.Error="当前桌面版本的用量记录暂不支持";}
            return result;
        }
        public static ConversationUsage Parse(string text,string id){var result=new ConversationUsage{ThreadId=id};Apply(text,result);return result;}
        static void Totals(Dictionary<string,object> usage,ConversationUsage result){
            result.Total=Number(usage,"total_tokens");result.Input=Number(usage,"input_tokens");result.Cached=Number(usage,"cached_input_tokens");result.Output=Number(usage,"output_tokens");
        }
        static void Apply(string text,ConversationUsage result){
            long? priorOutput=null,lastOutput=null;DateTimeOffset priorTime=default(DateTimeOffset),lastTime=default(DateTimeOffset);bool hasRecord=false;
            foreach(string line in text.Split('\n')){
                if(line.IndexOf("token_count",StringComparison.Ordinal)<0&&line.IndexOf("token_usage_record",StringComparison.Ordinal)<0)continue;
                try{
                    var root=Obj(new JavaScriptSerializer{MaxJsonLength=4*1024*1024}.DeserializeObject(line));var payload=Obj(Get(root,"payload"));if(payload==null)continue;
                    DateTimeOffset at;if(!DateTimeOffset.TryParse(Text(root,"timestamp"),CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out at))continue;
                    string kind=Text(root,"type");Dictionary<string,object> total=null;
                    if(kind=="token_usage_record"){
                        if(Text(payload,"thread_id")!=result.ThreadId)continue;
                        total=Obj(Get(payload,"thread_token_usage"));if(total==null)continue;
                        result.Context=Number(Obj(Get(payload,"usage")),"total_tokens");
                        if(!hasRecord){hasRecord=true;priorOutput=null;lastOutput=null;}
                    }else if(kind=="event_msg"&&Text(payload,"type")=="token_count"){
                        var info=Obj(Get(payload,"info"));if(info==null)continue;
                        var last=Obj(Get(info,"last_token_usage"));result.Context=Number(last,"total_tokens");result.Window=Number(info,"model_context_window");
                        if(!hasRecord)total=Obj(Get(info,"total_token_usage"));
                    }
                    if(total==null)continue;
                    Totals(total,result);result.Updated=at;long? output=Number(total,"output_tokens");
                    // Ignore duplicate snapshots; never add cached input to input or reasoning to output.
                    if(output.HasValue&&(!lastOutput.HasValue||output.Value!=lastOutput.Value)){
                        priorOutput=lastOutput;priorTime=lastTime;lastOutput=output;lastTime=at;
                    }
                }catch{}
            }
            if(lastOutput.HasValue&&priorOutput.HasValue&&lastOutput>=priorOutput){
                double elapsed=(lastTime-priorTime).TotalSeconds;
                if(elapsed>=.1&&elapsed<=120)result.OutputRate=(lastOutput.Value-priorOutput.Value)/elapsed;
            }
        }
    }
    // Windows' built-in SQLite library; read only the explicit conversation name,
    // never first_user_message, preview, transcript bodies, credentials or auth state.
    static class ConversationTitle {
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_open_v2(byte[] name,out IntPtr db,int flags,IntPtr vfs);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_prepare_v2(IntPtr db,byte[] sql,int bytes,out IntPtr stmt,IntPtr tail);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_bind_text(IntPtr stmt,int index,byte[] text,int length,IntPtr destructor);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_step(IntPtr stmt);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern IntPtr sqlite3_column_text(IntPtr stmt,int index);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_column_bytes(IntPtr stmt,int index);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_busy_timeout(IntPtr db,int ms);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_finalize(IntPtr stmt);
        [DllImport("winsqlite3.dll",CallingConvention=CallingConvention.Cdecl)] static extern int sqlite3_close(IntPtr db);
        public static string Read(string home,string id){
            IntPtr db=IntPtr.Zero,stmt=IntPtr.Zero;
            try{
                var file=Directory.EnumerateFiles(home,"state_*.sqlite").Select(p=>new{Path=p,Match=System.Text.RegularExpressions.Regex.Match(Path.GetFileName(p),@"^state_(\d+)\.sqlite$")}).Where(p=>p.Match.Success).OrderByDescending(p=>Int32.Parse(p.Match.Groups[1].Value)).FirstOrDefault();
                if(file==null||sqlite3_open_v2(Encoding.UTF8.GetBytes(file.Path+"\0"),out db,1,IntPtr.Zero)!=0)return "";
                sqlite3_busy_timeout(db,60);
                if(sqlite3_prepare_v2(db,Encoding.UTF8.GetBytes("SELECT name FROM threads WHERE id=? LIMIT 1\0"),-1,out stmt,IntPtr.Zero)!=0)return "";
                byte[] key=Encoding.UTF8.GetBytes(id);if(sqlite3_bind_text(stmt,1,key,key.Length,new IntPtr(-1))!=0||sqlite3_step(stmt)!=100)return "";
                IntPtr value=sqlite3_column_text(stmt,0);int count=sqlite3_column_bytes(stmt,0);if(value==IntPtr.Zero||count<=0||count>4096)return "";
                byte[] text=new byte[count];Marshal.Copy(value,text,0,count);return Encoding.UTF8.GetString(text);
            }catch{return "";}finally{if(stmt!=IntPtr.Zero)sqlite3_finalize(stmt);if(db!=IntPtr.Zero)sqlite3_close(db);}
        }
    }
    sealed class ConversationUsageCard : AlphaForm {
        readonly PetForm pet;readonly ContextMenuStrip taskMenu=new ContextMenuStrip();public ConversationUsage Value;public UsageTaskChoice[] Choices=new UsageTaskChoice[0];public Action<string> SelectTask;
        public bool Choosing {get{return taskMenu.Visible;}}
        public Rectangle WhiteBounds{get{return new Rectangle(Left+36,Top+36,Width-72,Height-72);}}
        public ConversationUsageCard(PetForm owner){pet=owner;Text="Codex 对话用量";Size=new Size(452,356);Cursor=Cursors.Default;
            taskMenu.Renderer=new ModernMenuRenderer();taskMenu.Font=new Font("Microsoft YaHei UI",9.5f);taskMenu.BackColor=Color.FromArgb(252,252,253);taskMenu.ForeColor=Design.Ink;taskMenu.Padding=new Padding(5,7,5,7);taskMenu.ShowImageMargin=false;taskMenu.ShowCheckMargin=true;taskMenu.Closed+=delegate{Invalidate();};

            MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left&&new Rectangle(50,96,352,34).Contains(e.Location))OpenTasks();};
            MouseMove+=delegate(object sender,MouseEventArgs e){Cursor=new Rectangle(50,96,352,34).Contains(e.Location)?Cursors.Hand:Cursors.Default;};
        }
        protected override void Dispose(bool disposing){if(disposing)taskMenu.Dispose();base.Dispose(disposing);}
        public void CloseTasks(){taskMenu.Close();}
        public void OpenTasks(){
            foreach(ToolStripItem old in taskMenu.Items.Cast<ToolStripItem>().ToArray())old.Dispose();taskMenu.Items.Clear();var auto=new ToolStripMenuItem("自动跟随最近操作"){Checked=String.IsNullOrEmpty(pet.Settings.TokenUsageSelectedThread)&&pet.Settings.TokenUsageFollowRecent};auto.Click+=delegate{if(SelectTask!=null)SelectTask("");};taskMenu.Items.Add(auto);taskMenu.Items.Add(new ToolStripSeparator());
            foreach(var choice in Choices){var current=choice;string title=current.Caption;if(title.Length>36)title=title.Substring(0,35)+"…";var item=new ToolStripMenuItem(title){Checked=String.Equals(pet.Settings.TokenUsageSelectedThread,current.Id,StringComparison.OrdinalIgnoreCase),ToolTipText=current.Title};item.Click+=delegate{if(SelectTask!=null)SelectTask(current.Id);};taskMenu.Items.Add(item);}
            if(Choices.Length==0)taskMenu.Items.Add(new ToolStripMenuItem("正在读取进行中与最近的任务…"){Enabled=false});
            taskMenu.Show(this,new Point(50,132));
        }
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
        protected override void WndProc(ref Message m){if(m.Msg==0x84){long v=m.LParam.ToInt64();var point=new Point((short)(v&0xffff),(short)((v>>16)&0xffff));if(!WhiteBounds.Contains(point)){m.Result=new IntPtr(-1);return;}}base.WndProc(ref m);}
        public void Place(Rectangle badge){
            var work=Screen.FromRectangle(badge).WorkingArea;int whiteWidth=Width-72,whiteHeight=Height-72;
            int x=badge.Left-whiteWidth-10;if(x<work.Left+8)x=badge.Right+10;if(x+whiteWidth>work.Right-8)x=work.Right-whiteWidth-8;
            int y=badge.Top+badge.Height/2-whiteHeight/2;y=Math.Max(work.Top+8,Math.Min(y,work.Bottom-whiteHeight-8));
            var target=new Point(x-36,y-36);if(Location!=target)Location=target;
        }
        public static string Format(long? value){return value.HasValue?value.Value.ToString("N0",CultureInfo.InvariantCulture):"—";}
        protected override void PaintSurface(Graphics g){
            Bubble(g,new RectangleF(36,36,Width-72,Height-72),20,pet.Settings);
            using(var title=new Font("Microsoft YaHei UI",11,FontStyle.Bold))using(var label=new Font("Microsoft YaHei UI",9))using(var tiny=new Font("Microsoft YaHei UI",8))using(var ink=new SolidBrush(Design.Ink))using(var gray=new SolidBrush(Color.FromArgb(125,125,132)))using(var trim=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap}){
                var v=Value;g.DrawString("Codex 对话用量",title,ink,54,50);
                g.DrawString(v==null?"正在读取…":v.Source,tiny,gray,new RectangleF(54,76,340,18),trim);
                using(var fill=new SolidBrush(Color.FromArgb(243,245,250)))using(var path=Design.Round(new RectangleF(50,96,352,34),10))g.FillPath(fill,path);
                g.DrawString(v==null?"选择任务":v.Title,label,ink,new RectangleF(60,102,310,22),trim);g.DrawString("▾",label,gray,380,102);
                if(v==null||v.Total==null){g.DrawString(v==null?"正在读取本机统计":v.Error,label,gray,new RectangleF(54,151,340,58));g.DrawString("点击上方标题切换任务",tiny,gray,54,257);return;}
                string rate=v.OutputRate.HasValue?"~ "+v.OutputRate.Value.ToString("0.0",CultureInfo.InvariantCulture)+" tok/s":"等待后续采样";
                if((DateTimeOffset.UtcNow-v.Updated).TotalSeconds>60)rate="等待新的 Token 采样";
                string context=Format(v.Context)+(v.Window.HasValue?" / "+Format(v.Window):"");if(v.Context.HasValue&&v.Window>0)context+=" · "+(100.0*v.Context.Value/v.Window.Value).ToString("0.0")+"%";
                string[] labels={"输出均速（估算）","本对话累计 Token","上下文（最近请求）"};string[] values={rate,Format(v.Total),context};
                for(int i=0;i<labels.Length;i++){int y=145+i*29;g.DrawString(labels[i],label,gray,54,y);g.DrawString(values[i],label,ink,new RectangleF(197,y,199,22),trim);}
                g.DrawString("输入 "+Format(v.Input)+" · 缓存 "+Format(v.Cached)+" · 输出 "+Format(v.Output),tiny,gray,new RectangleF(54,240,342,18),trim);
                string updated=v.Updated==default(DateTimeOffset)?"":v.Updated.ToLocalTime().ToString("HH:mm:ss");g.DrawString("均速含工具 / 等待时间 · 记录于 "+updated,tiny,gray,54,278);
            }
        }
    }
    sealed partial class TaskBadge {
        ConversationUsageCard usageHover;ConversationUsage cachedUsage;UsageTaskChoice[] cachedChoices=new UsageTaskChoice[0];DateTime hoverLeft=DateTime.MinValue,nextRead=DateTime.MinValue;int reading,hoverGeneration;
        protected override void OnShown(EventArgs e){base.OnShown(e);ReadUsage(false);}
        protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hoverLeft=DateTime.MinValue;UpdateUsageHover();}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);HideUsageHover();}
        protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(!Visible)HideUsageHover(true);}
        protected override void OnFormClosed(FormClosedEventArgs e){hoverGeneration++;if(usageHover!=null)usageHover.Dispose();base.OnFormClosed(e);}
        void HideUsageHover(bool force=false){hoverGeneration++;if(usageHover!=null){if(force)usageHover.CloseTasks();if(!usageHover.Choosing)usageHover.Hide();}}
        void UpdateUsageHover(){
            if(usageHover!=null&&usageHover.Choosing){hoverLeft=DateTime.MinValue;return;}
            if(!Visible||IsDisposed||Control.MouseButtons!=MouseButtons.None){HideUsageHover();return;}
            var white=new Rectangle(Left+36,Top+36,Width-72,Height-72);bool inside=white.Contains(Cursor.Position)||(usageHover!=null&&usageHover.Visible&&usageHover.WhiteBounds.Contains(Cursor.Position));
            if(!inside){if(hoverLeft==DateTime.MinValue)hoverLeft=DateTime.UtcNow;if((DateTime.UtcNow-hoverLeft).TotalMilliseconds>100)HideUsageHover();ReadUsage(false);return;}
            hoverLeft=DateTime.MinValue;if(usageHover==null||usageHover.IsDisposed){usageHover=new ConversationUsageCard(pet);usageHover.SelectTask=ChooseUsageTask;}
            usageHover.Choices=cachedChoices;usageHover.Place(white);if(!usageHover.Visible){usageHover.Value=cachedUsage;usageHover.Show(this);nextRead=DateTime.MinValue;}ReadUsage(true);
        }
        void ChooseUsageTask(string id){
            pet.Settings.TokenUsageSelectedThread=id??"";if(String.IsNullOrEmpty(id))pet.Settings.TokenUsageFollowRecent=true;pet.Settings.Save();hoverLeft=DateTime.MinValue;hoverGeneration++;nextRead=DateTime.MinValue;cachedUsage=null;
            if(usageHover!=null){usageHover.Value=null;usageHover.Invalidate();}ReadUsage(true);
        }
        void ReadUsage(bool hovering){
            if(DateTime.UtcNow<nextRead||System.Threading.Interlocked.CompareExchange(ref reading,1,0)!=0)return;
            nextRead=DateTime.UtcNow.AddMilliseconds(hovering?1200:4000);int generation=hoverGeneration;
            string home=Environment.GetEnvironmentVariable("CODEX_HOME")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");
            string bound=Program.BridgeOrigin(pet.Settings),selected=pet.Settings.TokenUsageSelectedThread;bool follow=pet.Settings.TokenUsageFollowRecent;
            var appChoices=pet.UsageTaskChoices;var active=pet.ActiveUsageThreads;
            Task.Run(delegate{
                var choices=UsageTaskSelection.Merge(appChoices,active,selected,bound,home);
                Guid fixedId;bool pinned=Guid.TryParse(selected,out fixedId);var value=ConversationUsageReader.Read(home,bound,follow,selected);
                var choice=choices.FirstOrDefault(c=>c.Id==value.ThreadId);if(choice!=null)value.Title=choice.Title;
                if(pinned)value.Source="已固定 · "+(choice!=null&&choice.Running?"进行中":"已结束 / 空闲")+" · 点击标题切换";
                else value.Source=(choice!=null&&choice.Running?"进行中 · ":"")+value.Source+" · 点击标题切换";
                System.Threading.Interlocked.Exchange(ref reading,0);
                if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke(new Action(delegate{
                    if(selected!=pet.Settings.TokenUsageSelectedThread){nextRead=DateTime.MinValue;return;}
                    cachedUsage=value;cachedChoices=choices;if(generation==hoverGeneration&&usageHover!=null&&!usageHover.IsDisposed&&usageHover.Visible){usageHover.Value=value;usageHover.Choices=choices;usageHover.Invalidate();}
                }));}catch{}
            });
        }
    }
    static class ConversationUsageChecks {
        public static void Run(){
            string id="11111111-1111-4111-8111-111111111111",other="22222222-2222-4222-8222-222222222222";
            var json=new JavaScriptSerializer();
            Func<string,int,int,string> record=delegate(string at,int total,int output){return json.Serialize(new{timestamp=at,type="token_usage_record",payload=new{thread_id=id,thread_token_usage=new{total_tokens=total,input_tokens=total-output,cached_input_tokens=60,output_tokens=output},usage=new{total_tokens=170}}});};
            string one=record("2026-01-01T00:00:00Z",100,20),two=record("2026-01-01T00:00:10Z",200,50);
            string context=json.Serialize(new{timestamp="2026-01-01T00:00:05Z",type="event_msg",payload=new{type="token_count",info=new{total_token_usage=new{total_tokens=9999,output_tokens=9999},last_token_usage=new{total_tokens=150},model_context_window=1000}}});
            var value=ConversationUsageReader.Parse("partial line\n"+one+"\n"+one+"\n"+context+"\n"+two+"\n"+two.Replace(id,other)+"\n{broken token_count",id);
            if(value.Total!=200||value.Input!=150||value.Cached!=60||value.Output!=50)throw new Exception("Token counters mix or double count");
            if(value.Context!=170||value.Window!=1000||value.OutputRate!=3)throw new Exception("Token context or estimated rate");
            if(ConversationUsageReader.Parse("",id).Total.HasValue)throw new Exception("Missing usage must remain unknown");
            var empty=ConversationUsageReader.Parse(json.Serialize(new{timestamp="2026-01-01T00:00:00Z",type="event_msg",payload=new{type="token_count",info=(object)null}}),id);
            if(empty.Total.HasValue)throw new Exception("Unknown usage became zero");
            var activity=new Dictionary<string,object>{{json.Serialize(new[]{"local",id}),100L},{json.Serialize(new[]{"remote",other}),999L},{"invalid",1000L}};
            string state=json.Serialize(new Dictionary<string,object>{{"electron-persisted-atom-state",new Dictionary<string,object>{{"thread-user-activity-times-v1",activity}}}});
            if(ConversationUsageReader.RecentThread(state)!=id)throw new Exception("Wrong recent local conversation");
        }
    }
}
