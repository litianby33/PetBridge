using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;

namespace PetBridge {
    static class Program {
        public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
        public static bool Qa,Testing,Offline,QaWindow;public static bool DebugTaskbar {get{return Qa&&QaWindow;}}public static string CharacterOverride;
        public static string BaseDataRoot {get{return Environment.GetEnvironmentVariable("PETBRIDGE_DATA_DIR")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"PetBridge");}}
        public static string DataRoot {get{return Path.Combine(BaseDataRoot,Testing?"checks":Qa?"qa":"profile");}}
        public static string CodexHome {get{return Environment.GetEnvironmentVariable("CODEX_HOME")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".codex");}}
        [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string title);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr handle,out NativeRect rect);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr handle);
        public struct NativeRect {public int Left,Top,Right,Bottom;}
        public static bool PetWindowExists(){return FindWindow(null,"PetBridge · 桌面搭档")!=IntPtr.Zero;}
        [STAThread] static void Main(string[] args){
            Qa=Array.IndexOf(args,"--qa")>=0;QaWindow=Array.IndexOf(args,"--qa-window")>=0;Testing=Array.IndexOf(args,"--self-test")>=0;Offline=Qa||Testing||Array.IndexOf(args,"--offline")>=0;Directory.CreateDirectory(DataRoot);
            int roleArg=Array.IndexOf(args,"--character");if(roleArg>=0&&roleArg+1<args.Length)CharacterOverride=Path.GetFullPath(args[roleArg+1]);
            try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{}
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            try{
                if(DistributionSetup.Handle(args))return;
                int inputProbe=Array.IndexOf(args,"--input-history-probe");if(inputProbe>=0&&inputProbe+1<args.Length){InputReminderChecks.ProbeHistory(args[inputProbe+1]);return;}
                int navigationProbe=Array.IndexOf(args,"--navigation-probe");if(navigationProbe>=0&&navigationProbe+1<args.Length){AppConversationNavigation.Probe(BridgeOrigin(PetSettings.Load()),args[navigationProbe+1]);return;}
                if(Array.IndexOf(args,"--bridge-probe")>=0){AppChatWatcher.Probe(BridgeOrigin(PetSettings.Load()));return;}
                if(Array.IndexOf(args,"--self-test")>=0){Checks.Run();return;}
                if(Array.IndexOf(args,"--signal")>=0){int i=Array.IndexOf(args,"--signal");if(i+1<args.Length)PetIpc.Send(args[i+1]);return;}
                if(Array.IndexOf(args,"--watch")>=0){bool first;using(var m=new Mutex(true,"Local\\PetBridgeLaunchWatcherV1",out first)){if(first)Application.Run(new LaunchWatcher());}return;}
                bool created;using(var mutex=new Mutex(true,Qa?"Local\\PetBridgeV1QA":"Local\\PetBridgeV1",out created)){if(!created){PetIpc.Send("wake");return;}Application.Run(new PetForm());}
            }catch(Exception ex){Environment.ExitCode=1;File.WriteAllText(Path.Combine(DataRoot,"startup-error.txt"),ex.ToString(),Encoding.UTF8);if(!Array.Exists(args,a=>a=="--watch"||a=="--bridge-probe"||a=="--self-test"||a=="--navigation-probe"||a=="--input-history-probe"||a=="--bind"||a=="--set-character"||a=="--validate-character"||a=="--render-preview"))MessageBox.Show("搭档暂时没有启动成功："+ex.Message,"搭档");}
        }
        public static void OpenUrl(string value){var uri=new Uri(value);if(uri.Scheme!="codex"&&uri.Scheme!="https")return;Process.Start(new ProcessStartInfo(value){UseShellExecute=true});}
        public static string BridgeOrigin(PetSettings s){Guid id;if(Guid.TryParse(s.AppBridgeThreadId,out id))return id.ToString();var m=System.Text.RegularExpressions.Regex.Match(s.ChatLink??"",@"codex://threads/([0-9a-fA-F-]{36})");return m.Success?m.Groups[1].Value:"";}
        public static string DraftUrl(string text){return "codex://new?prompt="+Uri.EscapeDataString(text);}
        public static void StartWatcher(){if(Program.Offline)return;Process.Start(new ProcessStartInfo(Path.Combine(Root,"PetBridge.exe"),"--watch"){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=Root});}
    }
    sealed class PetSettings {
        public int X=-99999,Y=-99999,SettingsX=-99999,SettingsY=-99999;public double Scale=1;
        public bool AutoDock=true,FollowChatGPT=false,AutoStartWithChatGPT=false,TaskReminders=true,WasDocked=false;
        public string DockEdge="right";
        public int ShadowOpacity=100,ShadowBlur=20,ShadowOffset=3,ToolbarGap=-4,UsageGap=9,FiveHourWarning=20,WeeklyWarning=20,IdleWaveSeconds=60;
        public bool ShowUpdatedAt=false,ShowUsage=false,QuotaReminders=true,ShowActivity=true,DockHoverUsage=true,DockQuotaBelow=false;
        public int ActivityOffsetX=0,ActivityOffsetY=-67,ActivityGap=3,NoticeOffsetY=10;public double ActivityScale=.8;public bool NoticeBounce=true,HoverUsage=true;
        public int DockActivityGap=0,DockActivityOffsetY=0,DockThreshold=3,NoticeGlow=30,NoticeGlowRadius=10;
        public bool NavigateOnTaskNoticeClick=false;
        public bool ChatGPTCompletionReminders=false;
        public bool InputReminders=true;public bool AppChatMonitoring=true;public bool OrdinaryChatMonitoring=false;public bool WorkAppMonitoring=true;public bool AvoidFullscreen=true;public string CompleteColor="#30a46c",LowColor="#dd9632",EmptyColor="#7f181b",ResetColor="#388bff";
        public QuotaAlarmState AlarmState=new QuotaAlarmState();
        public string ActivitySide="auto";
        public QuotaLayout QuotaLayout=new QuotaLayout();
        public QuotaLayout VerticalQuotaLayout=QuotaLayout.CreateVertical();public string ChatTarget="codex";public string QuotaLayoutMode="auto";public int QuotaEdgeMargin=12;
        public string CharacterPath=null,AppBridgeThreadId="",CliPath="",Model="",ChatLink="codex://threads/new";
        public static PetSettings Load(){try{return new JavaScriptSerializer().Deserialize<PetSettings>(File.ReadAllText(Path.Combine(Program.DataRoot,"settings.json"),Encoding.UTF8))??new PetSettings();}catch{return new PetSettings();}}
        public void Save(){File.WriteAllText(Path.Combine(Program.DataRoot,Program.Qa?"settings.qa.json":"settings.json"),new JavaScriptSerializer().Serialize(this),Encoding.UTF8);}
    }
    sealed partial class PetForm : AlphaForm {
        readonly SpriteBank bank=new SpriteBank();readonly PetSettings settings=PetSettings.Load();
        readonly System.Windows.Forms.Timer clock=new System.Windows.Forms.Timer();readonly Stopwatch time=Stopwatch.StartNew();
        readonly ContextMenuStrip menu=new ContextMenuStrip();readonly NotifyIcon tray=new NotifyIcon();readonly ToolTip tips=new ToolTip();
        readonly Font dockFont=new Font("Microsoft YaHei UI",8);
        public readonly UsageService Usage=new UsageService();
        ChatForm composer;SettingsForm settingsForm;LayoutEditor layoutEditor;TaskBadge taskBadge;DockQuotaCard dockQuota;int activeTasks,localActiveTasks,appActiveTasks;AppChatWatcher appChats;DateTime testTaskUntil=DateTime.MinValue,testNoticeAt=DateTime.MinValue;UsageCard usageCard;NoticeCard notice;CompletionWatcher completion;
        readonly QuotaAlarm alarm;
        sealed class PendingNotice {public string Key;public Action Show;}
        readonly List<PendingNotice> pendingNotices=new List<PendingNotice>();string noticeKey="",lastCommand="";
        bool QueueNotice(string key,Action show){if(notice==null||!notice.Visible){noticeKey=key;return false;}if(noticeKey==key)return true;foreach(var item in pendingNotices)if(item.Key==key){item.Show=show;return true;}if(pendingNotices.Count<12)pendingNotices.Add(new PendingNotice{Key=key,Show=show});return true;}
        
        DateTime lastInteraction=DateTime.UtcNow,lastNear=DateTime.MinValue;
        float toolbarAmount;int lookIndex=-1;bool docking;Point dockFrom,dockTarget;DateTime dockStarted;Rectangle dockWork;
        int row,frame,loops,nextFrame;double scale;bool busy,held,moved,docked,unread,changing,pointerInside;
        Point downCursor,downLocation,restoreLocation;string edge="right";DateTime lastHop=DateTime.MinValue,redockAt=DateTime.MinValue;int tick;
        public PetSettings Settings {get{return settings;}}
        public bool IsDocked {get{return docked;}}
        public bool IsThinking {get{return busy||activeTasks>0||DateTime.UtcNow<testTaskUntil;}}
        public string DockSide {get{return edge;}}
        public Rectangle CharacterBounds {get{return docked?Bounds:new Rectangle(Left+(Width-(int)(192*scale))/2+(int)(bank.BodyBounds.X*scale),Top+(int)(bank.BodyBounds.Y*scale),(int)(bank.BodyBounds.Width*scale),(int)(bank.BodyBounds.Height*scale));}}
        public int ArtHeight {get{return (int)Math.Round(208*scale);}}
        public int ToolbarBottom {get{return ToolbarRect.Bottom;}}
        public int ComposerBottom {get{return ComposerVisible?composer.Bottom:Top+ToolbarBottom;}}
        public int NoticeFooterBottom {get{int bottom=Math.Max(CharacterBounds.Bottom,ComposerBottom);if(usageCard!=null&&usageCard.Visible)bottom=Math.Max(bottom,usageCard.WhiteBounds.Bottom);if(dockQuota!=null&&dockQuota.Visible)bottom=Math.Max(bottom,dockQuota.Top+36+40);return bottom;}}
        public int OccupiedHeight {get{return QuotaOccupiedHeightAt(Location,Screen.FromControl(this).WorkingArea,(int)(bank.BodyBounds.Bottom*scale));}}
        Point Constrain(Point location){var a=Screen.FromPoint(new Point(location.X+Width/2,location.Y+Height/2)).WorkingArea;int inset=(int)(bank.BodyBounds.Left*scale);var target=new Point(Math.Max(a.Left-inset,Math.Min(location.X,a.Right-Width+inset)),location.Y);int occupied=QuotaOccupiedHeightAt(target,a,(int)(bank.BodyBounds.Bottom*scale));target.Y=Math.Max(a.Top-(int)(bank.BodyBounds.Top*scale),Math.Min(location.Y,a.Bottom-occupied));return target;}
        Rectangle ToolbarRect {get{int w=(int)(32+108*toolbarAmount),h=(int)(8+32*toolbarAmount);return new Rectangle(Width/2-w/2,ArtHeight+settings.ToolbarGap,w,h);}}
        public bool ComposerVisible {get{return composer!=null&&composer.Visible;}}
        public PetForm(){
            alarm=new QuotaAlarm(settings.AlarmState??(settings.AlarmState=new QuotaAlarmState()));
            NormalizeQuotaLayouts();
            Text="PetBridge · 桌面搭档";Name="PetBridgePet";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=Program.DebugTaskbar;TopMost=true;AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.Manual;DoubleBuffered=true;
            edge=settings.DockEdge=="left"?"left":"right";scale=Math.Max(.5,Math.Min(3,settings.Scale));SetSize();var a=Screen.PrimaryScreen.WorkingArea;Location=new Point(a.Right-Width-50,a.Bottom-Height-25);var stored=new Rectangle(settings.X,settings.Y,Width,Height);foreach(var screen in Screen.AllScreens)if(screen.WorkingArea.IntersectsWith(stored)){Location=stored.Location;break;}Clamp();
            BuildModernMenu();
            clock.Interval=40;clock.Tick+=OnTick;clock.Start();MouseDown+=OnDown;MouseMove+=OnMove;MouseUp+=OnUp;MouseWheel+=delegate(object s,MouseEventArgs e){if((ModifierKeys&Keys.Control)!=0)SetScale(scale+(e.Delta>0?.1:-.1));};
            LocationChanged+=delegate{if(changing)return;SyncQuotaGeometry();if(ComposerVisible)composer.PlaceNearPet();if(usageCard!=null&&usageCard.Visible)usageCard.Place();if(taskBadge!=null&&taskBadge.Visible)taskBadge.Place();if(dockQuota!=null&&dockQuota.Visible)dockQuota.Place();if(notice!=null&&notice.Visible)notice.Place();};
            Usage.Changed+=delegate{if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(new Action(delegate{CheckQuota();DumpState();}));}catch(InvalidOperationException){}};
            Shown+=delegate{PetIpc.Listen(this);string sessions=Program.Offline?Path.Combine(Program.DataRoot,"qa-sessions"):Path.Combine(Program.CodexHome,"sessions");completion=new CompletionWatcher(sessions,delegate(CompletionEvent evt){if(!IsDisposed&&IsHandleCreated)BeginInvoke(new Action(delegate{OnTaskEnded(evt);}));},delegate(int count){if(!IsDisposed&&IsHandleCreated)BeginInvoke(new Action(delegate{localActiveTasks=count;activeTasks=localActiveTasks+appActiveTasks;SyncActivity();DumpState();}));},delegate(InputEvent input){if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(new Action(delegate{OnInputChanged(input);}));}catch(InvalidOperationException){}});SyncAppMonitoring();if(settings.WasDocked&&!Program.Qa)DockTo(settings.DockEdge);else EnsureUsage();DumpState();};
            FormClosing+=delegate{clock.Stop();if(completion!=null)completion.Dispose();if(appChats!=null)appChats.Dispose();Usage.Dispose();Save();tray.Visible=false;if(composer!=null&&!composer.IsDisposed)composer.End();if(usageCard!=null)usageCard.Dispose();if(notice!=null)notice.Dispose();if(settingsForm!=null)settingsForm.Dispose();if(layoutEditor!=null)layoutEditor.Dispose();if(taskBadge!=null)taskBadge.Dispose();if(dockQuota!=null)dockQuota.Dispose();};FormClosed+=delegate{bank.Dispose();menu.Dispose();tray.Dispose();if(petTrayIcon!=null)petTrayIcon.Dispose();tips.Dispose();clock.Dispose();dockFont.Dispose();};
        }
        protected override CreateParams CreateParams {get{var p=base.CreateParams;if(!Program.DebugTaskbar){p.ExStyle|=0x80;p.ExStyle&=~0x40000;}return p;}}
        protected override bool ShowWithoutActivation {get{return true;}}
        void AddToggle(string text,bool value,Action<bool> apply){var item=new ToolStripMenuItem(text){Checked=value,CheckOnClick=true};item.Click+=delegate{apply(item.Checked);Save();};menu.Items.Add(item);}
        void SetSize(){Size=new Size(Math.Max(140,(int)Math.Round(192*scale)),ArtHeight+settings.ToolbarGap+76);}
        int FrameDelay {get{return bank.Delays[row];}}
        void OnTick(object sender,EventArgs e){tick++;if(tick%5==0)CheckFullscreen();if(userHidden||fullscreenSuppressed){if(Program.Qa&&tick%25==0)DumpState();return;}var now=DateTime.UtcNow;var mouse=PointToClient(Cursor.Position);int ox=(Width-(int)Math.Round(192*scale))/2;int sx=(int)((mouse.X-ox)/scale),sy=(int)(mouse.Y/scale);bool hit=!docked&&!docking&&sx>=0&&sx<192&&sy>=0&&sy<208&&bank.Frames[0][0].GetPixel(sx,sy).A>20;
            bool peekHover=docked&&ClientRectangle.Contains(mouse);
            var bridge=new Rectangle(Width/2-78,Math.Min(ArtHeight-12,ArtHeight+settings.ToolbarGap),156,Math.Max(54,settings.ToolbarGap+54));
            bool near=hit||(!docked&&(ToolbarRect.Contains(mouse)||(toolbarAmount>.01f&&bridge.Contains(mouse))))||menu.Visible;
            bool compactAllowed=docked?settings.DockHoverUsage:!docking&&!settings.ShowUsage&&settings.HoverUsage;
            bool compactHover=docked?peekHover:near;
            if(compactAllowed&&(compactHover||(dockQuota!=null&&dockQuota.Visible&&dockQuota.Bounds.Contains(Cursor.Position)))){if(dockQuota==null||dockQuota.IsDisposed)dockQuota=new DockQuotaCard(this);dockQuota.Place();if(!dockQuota.Visible)dockQuota.Show(this);}else if(dockQuota!=null&&dockQuota.Visible)dockQuota.Hide();
            if(near){lastNear=now;lastInteraction=now;if(redockAt!=DateTime.MinValue)redockAt=now.AddSeconds(15);}
            float goal=!docked&&!docking&&near?1:0;
            float previous=toolbarAmount;toolbarAmount+=(goal-toolbarAmount)*.65f;if(Math.Abs(goal-toolbarAmount)<.01)toolbarAmount=goal;
            if(testNoticeAt!=DateTime.MinValue&&now>=testNoticeAt){testNoticeAt=DateTime.MinValue;testTaskUntil=DateTime.MinValue;NotifyCompletion(new CompletionEvent{ThreadId="",TurnId="preview"},true);}
            if(tick%5==0)SyncActivity();
            if(hit&&!pointerInside&&!NoticeJumpRunning&&!IsThinking&&!held&&lookIndex<0&&row==0&&(now-lastHop).TotalMilliseconds>1800){lastHop=now;Play(AnimationSlots.Jump,1);}pointerInside=hit;
            if(docking){double t=Math.Min(1,(now-dockStarted).TotalMilliseconds/600);Location=new Point(dockFrom.X+(int)((dockTarget.X-dockFrom.X)*t),dockFrom.Y);if(t>=1)FinishDock();}
            if(!docked&&time.ElapsedMilliseconds>=nextFrame){frame++;if(frame>=bank.Counts[row]){frame=0;if(loops>0&&--loops==0)row=IsThinking?7:0;}nextFrame=(int)time.ElapsedMilliseconds+FrameDelay;Invalidate();}
            if(previous!=toolbarAmount){if(settings.ShowUsage&&!held&&!docked&&!docking)Clamp();Invalidate();if(usageCard!=null&&usageCard.Visible)usageCard.Place();if(dockQuota!=null&&dockQuota.Visible)dockQuota.Place();}
            TickNoticeJump();if(notice!=null&&notice.Visible)notice.Place();
            if(settings.IdleWaveSeconds>0&&!NoticeJumpRunning&&!docked&&!docking&&!IsThinking&&!held&&!ComposerVisible&&row==0&&(now-lastInteraction).TotalSeconds>=settings.IdleWaveSeconds){lastInteraction=now;Play(AnimationSlots.Wave,2);}
            if(tick%25==0){if(redockAt!=DateTime.MinValue&&now>redockAt&&!held&&!ComposerVisible&&(notice==null||!notice.Visible)){redockAt=DateTime.MinValue;DockTo(edge);}if(settings.FollowChatGPT&&!held&&!docked&&!docking&&!ComposerVisible)FollowWindow();}
            if(Program.Qa&&tick%25==0)DumpState();
        }
        public static int LookDirection(Point origin,Point target){double degrees=Math.Atan2(target.X-origin.X,origin.Y-target.Y)*180/Math.PI;if(degrees<0)degrees+=360;return (int)Math.Round(degrees/22.5)%16;}
        public void LookAt(Point? target){int next=target.HasValue?LookDirection(new Point(Left+Width/2,Top+(int)(100*scale)),target.Value):-1;if(next!=lookIndex){lookIndex=next;Invalidate();}}
        public void Play(int value,int repeats){if(value==4&&!bank.HasNativeJump){jumpRepeats=Math.Max(1,repeats);jumpWatch.Restart();value=0;repeats=0;}row=value;frame=0;loops=repeats;nextFrame=(int)time.ElapsedMilliseconds+FrameDelay;Invalidate();}
        public void SetBusy(bool value){busy=value;SyncActivity();if(docked&&value)return;Play(IsThinking?7:8,IsThinking?0:2);}
        public void SetScale(double value){if(docked)Undock(false);int cx=Left+Width/2,bottom=Top+ArtHeight;scale=Math.Round(Math.Max(.5,Math.Min(3,value)),2);settings.Scale=scale;changing=true;SetSize();Location=new Point(cx-Width/2,bottom-ArtHeight);changing=false;Clamp();if(ComposerVisible)composer.PlaceNearPet();Save();Invalidate();}
        void OnDown(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;lastInteraction=DateTime.UtcNow;StopNoticeJump();held=true;moved=false;redockAt=DateTime.MinValue;downCursor=Cursor.Position;downLocation=Location;Capture=true;}
        void OnMove(object s,MouseEventArgs e){if(!held||docked)return;var c=Cursor.Position;int dx=c.X-downCursor.X,dy=c.Y-downCursor.Y;if(Math.Abs(dx)+Math.Abs(dy)>5)moved=true;if(moved){settings.FollowChatGPT=false;Location=Constrain(new Point(downLocation.X+dx,downLocation.Y+dy));edgePreview=settings.AutoDock?DetectEdge(CharacterBounds,Screen.FromPoint(Cursor.Position).WorkingArea,settings.DockThreshold+12):null;Invalidate();int next=dx>=0?1:2;if(row!=next)Play(next,0);}}
        void OnUp(object s,MouseEventArgs e){if(e.Button!=MouseButtons.Left||!held)return;held=false;Capture=false;if(docked){Undock(false);return;}if(moved){var work=Screen.FromPoint(Cursor.Position).WorkingArea;string target=DetectEdge(CharacterBounds,work,settings.DockThreshold);edgePreview="";Clamp();Play(IsThinking?7:0,0);Save();if(settings.AutoDock&&target!=null)DockTo(target);return;}if(ToolbarRect.Contains(e.Location)){int section=e.X-(Width/2-70);if(toolbarAmount<.5){lastNear=DateTime.UtcNow;return;}if(section>=46&&section<92)ToggleUsage();else if(section>=92)menu.Show(this,new Point(Math.Max(0,Width/2-15),ToolbarRect.Bottom));else ToggleChat();}else ToggleChat();}
        public static string DetectEdge(Rectangle pet,Rectangle work,int margin=3){if(pet.Left<=work.Left+margin)return "left";if(pet.Right>=work.Right-margin)return "right";return null;}
        public void DockTo(string direction){StopNoticeJump();if(docked||docking)return;if(direction!="left"&&direction!="right")return;Save();restoreLocation=Location;edge=direction;dockWork=Screen.FromControl(this).WorkingArea;docking=true;dockFrom=Location;dockTarget=new Point(edge=="left"?dockWork.Left-Width/2:dockWork.Right-Width/2,Top);dockStarted=DateTime.UtcNow;if(composer!=null)composer.HideAll();if(usageCard!=null)usageCard.Hide();Play(edge=="left"?2:1,0);}
        void FinishDock(){docking=false;docked=true;changing=true;Size=new Size(Math.Max(24,(int)Math.Round(bank.PeekSource(edge).Width*scale)),(int)Math.Round(bank.PeekSource(edge).Height*scale));Location=new Point(edge=="left"?dockWork.Left:dockWork.Right-Width,Math.Max(dockWork.Top+8,Math.Min(restoreLocation.Y+(int)(bank.BodyBounds.Top*scale),dockWork.Bottom-Height-8)));changing=false;Save();SyncActivity();Invalidate();DumpState();}
        public void Undock(bool temporary){if(!docked&&!docking)return;var work=docking?dockWork:Screen.FromControl(this).WorkingArea;docked=false;docking=false;changing=true;SetSize();Location=restoreLocation;Left=edge=="left"?work.Left+24:work.Right-Width-24;changing=false;Clamp();Play(IsThinking?7:0,0);lastNear=DateTime.UtcNow;if(temporary)redockAt=DateTime.UtcNow.AddSeconds(15);else redockAt=DateTime.MinValue;EnsureUsage();Save();Invalidate();DumpState();}
        void Clamp(){Location=Constrain(Location);SyncQuotaGeometry();}
        void Save(){if(!docked&&!docking){settings.X=Left;settings.Y=Top;}settings.WasDocked=docked;settings.DockEdge=edge;settings.Save();}
        void FollowWindow(){try{foreach(var p in Process.GetProcessesByName("ChatGPT")){var h=p.MainWindowHandle;Program.NativeRect r;if(h==IntPtr.Zero||!Program.IsWindowVisible(h)||!Program.GetWindowRect(h,out r)||r.Right-r.Left<500)continue;var a=Screen.FromPoint(new Point(r.Right-1,r.Bottom-1)).WorkingArea;Location=new Point(Math.Max(a.Left,Math.Min(r.Right-Width-12,a.Right-Width)),Math.Max(a.Top,Math.Min(r.Bottom-OccupiedHeight-70,a.Bottom-OccupiedHeight)));break;}}catch{}}
        public void ToggleChat(){if(ComposerVisible){composer.HideAll();EnsureUsage();Invalidate();}else ShowComposer();}
        public void ShowComposer(){redockAt=DateTime.MinValue;Undock(false);if(composer==null||composer.IsDisposed)composer=new ChatForm(this);composer.VisualSettings=settings;composer.PlaceNearPet();composer.Show(this);Clamp();composer.PlaceNearPet();EnsureUsage();composer.Activate();composer.FocusInput();Invalidate();}
        public void ToggleUsage(){settings.ShowUsage=!settings.ShowUsage;if(!docked&&!docking)Clamp();EnsureUsage();Save();}
        void EnsureUsage(){if(userHidden||fullscreenSuppressed||docked||docking||!settings.ShowUsage){if(usageCard!=null)usageCard.Hide();return;}if(usageCard==null||usageCard.IsDisposed)usageCard=new UsageCard(this,Usage);usageCard.Place();if(!usageCard.Visible)usageCard.Show(this);}
        public void OpenSettings(){if(settingsForm==null||settingsForm.IsDisposed)settingsForm=new SettingsForm(this);settingsForm.Show(this);settingsForm.Activate();}
        public void ApplySettings(){SyncAppMonitoring();NormalizeQuotaLayouts();Save();if(!docked&&!docking){SetSize();Clamp();}EnsureUsage();if(ComposerVisible){composer.VisualSettings=settings;composer.SyncSkin();composer.PlaceNearPet();}if(notice!=null&&notice.Visible){notice.Place();notice.Invalidate();}SyncActivity();if(dockQuota!=null&&dockQuota.Visible)dockQuota.Place();Invalidate();}
        public void EditQuotaLayout(){if(layoutEditor==null||layoutEditor.IsDisposed)layoutEditor=new LayoutEditor(this);layoutEditor.Show(this);layoutEditor.Activate();}
        void SyncActivity(){bool running=IsThinking;if(userHidden||fullscreenSuppressed){if(taskBadge!=null)taskBadge.Hide();return;}if(!docked&&!docking&&!held){if(running&&row!=7)Play(7,0);else if(!running&&row==7)Play(docked?0:8,docked?0:2);}if(settings.ShowActivity&&running){if(taskBadge==null||taskBadge.IsDisposed)taskBadge=new TaskBadge(this);taskBadge.Place();if(!taskBadge.Visible)taskBadge.Show(this);}else if(taskBadge!=null)taskBadge.Hide();}
        public void PreviewActivity(){testTaskUntil=DateTime.UtcNow.AddSeconds(15);SyncActivity();DumpState();}
        public void TestReminder(){NotifyCompletion(new CompletionEvent{ThreadId="",TurnId="preview"},true);}
        public void TestDockReminder(){if(docked||docking)Undock(false);DockTo("left");testTaskUntil=DateTime.UtcNow.AddSeconds(4);testNoticeAt=DateTime.UtcNow.AddSeconds(4);SyncActivity();}
        void CheckQuota(){if(pendingTaskQuota>0||Usage.Refreshing||Usage.LastError||Usage.Snapshot==null)return;var decision=alarm.Observe(Usage.Snapshot,settings.FiveHourWarning,settings.WeeklyWarning,DateTimeOffset.UtcNow.ToUnixTimeSeconds());settings.Save();if(decision!=null&&(decision.Kind=="reset"||settings.QuotaReminders))ShowDecision("quota:"+decision.Kind+":"+decision.Title+":"+decision.Message,decision,"");}
        void ShowQuotaNotice(string message,bool test){ShowDecision(test?"quota-test":"quota",new QuotaDecision{Kind="low",Title=test?"低额度提醒预览":"额度提醒",Message=message},"");}
        public void NoticeDismissed(){noticeKey="";if(pendingNotices.Count>0){var next=pendingNotices[0];pendingNotices.RemoveAt(0);BeginInvoke(next.Show);}unread=false;if(redockAt!=DateTime.MinValue)redockAt=DateTime.UtcNow.AddSeconds(3);Invalidate();DumpState();}
        public void PreviewQuota(){ShowQuotaNotice("5 小时剩余 15% · 提醒效果预览",true);}
        public void OpenCurrent(){unread=false;Invalidate();Program.OpenUrl(settings.ChatLink);}
        public void OpenTask(string link){if(Program.Qa){File.AppendAllText(Path.Combine(Program.DataRoot,"notice-navigation.qa.jsonl"),new JavaScriptSerializer().Serialize(new{target=link})+Environment.NewLine);return;}unread=false;Invalidate();string chatId;if(AppConversationNavigation.TryChatId(link,out chatId)){OpenDesktopChat(chatId,link);return;}Program.OpenUrl(link);}
        public void NotifyCompletion(CompletionEvent evt,bool test){ShowDecision(test?"task-test":"task:"+evt.ThreadId+":"+evt.TurnId,new QuotaDecision{Kind="complete",Title=test?"任务完成提醒测试":"ChatGPT 任务完成啦",Message=test?"测试成功 · 点击气泡收起":NoticeClickPolicy.CompletionText(settings.NavigateOnTaskNoticeClick)},test?"":CompletionLink(evt));}
        public void HandleCommand(string command){lastCommand=command;if(Program.Qa&&command=="completion-link-test")ShowDecision("click-policy-test",new QuotaDecision{Kind="complete",Title="完成气泡点击测试",Message="测试点击行为"},"codex://threads/11111111-1111-4111-8111-111111111111");else if(command=="wake"){RestorePet();ShowComposer();}else if(command=="hide")TemporarilyHide();else if(command=="restore")RestorePet();else if(command=="reset-preview")ShowDecision("reset-test",new QuotaDecision{Kind="reset",Title="5 小时额度已重置",Message="现在可以继续聊天啦"},"");else if(command=="empty-preview")ShowDecision("empty-test",new QuotaDecision{Kind="empty",Title="5 小时已用完啦",Message="等待 2 小时 15 分钟后重置"},"");else if(Program.Qa&&command=="notice-close"){if(notice!=null)notice.Dismiss();}else if(Program.Qa&&command=="near-top"){Undock(false);Location=new Point(Left,Screen.FromControl(this).WorkingArea.Top-(int)(45*scale));}else if(Program.Qa&&command=="near-bottom"){Undock(false);Location=Constrain(new Point(Left,Screen.FromControl(this).WorkingArea.Bottom));}else if(Program.Qa&&command=="empty-test")ShowDecision("empty-test",new QuotaDecision{Kind="empty",Title="5 小时已用完啦",Message="等待 2 小时 15 分钟后重置"},"");else if(Program.Qa&&command=="reset-test")ShowDecision("reset-test",new QuotaDecision{Kind="reset",Title="5 小时额度已重置",Message="现在可以继续聊天啦"},"");else if(command=="quit")Close();else if(Program.Qa&&command=="input-flow-test")OnInputChanged(new InputEvent{ThreadId="11111111-1111-4111-8111-111111111111",RequestId="flow-test",Link=""});else if(Program.Qa&&command=="input-flow-resolve")OnInputChanged(new InputEvent{ThreadId="11111111-1111-4111-8111-111111111111",RequestId="flow-test",Resolved=true});else if(command=="input-test")PreviewInputReminder();else if(command=="notify-test")NotifyCompletion(new CompletionEvent{ThreadId="",TurnId="preview"},true);else if(command=="usage")ToggleUsage();else if(command=="settings")OpenSettings();else if(command=="quota-test")PreviewQuota();else if(command=="layout")EditQuotaLayout();else if(command=="activity-test")PreviewActivity();else if(command=="dock-notify-test")TestDockReminder();else if(Program.Qa&&command=="dock-left")DockTo("left");else if(Program.Qa&&command=="dock-right")DockTo("right");else if(Program.Qa&&command=="undock")Undock(false);else if(Program.Qa&&command=="think-test")SetBusy(true);else if(Program.Qa&&command=="idle-test")SetBusy(false);else if(Program.Qa&&command=="walk-right-test")Play(1,4);else if(Program.Qa&&command=="walk-left-test")Play(2,4);}
        public void DumpState(){try{File.WriteAllText(Path.Combine(Program.DataRoot,Program.Qa?"runtime-state.qa.json":"runtime-state.json"),new JavaScriptSerializer().Serialize(new{version="1.3.1",character=bank.Spec.displayName,characterId=bank.Spec.id,hasNativeJump=bank.HasNativeJump,animationFallbacks=bank.ResolvedFrom,noticeTop=notice==null?0:notice.WhiteBounds.Top,noticeBottom=notice==null?0:notice.WhiteBounds.Bottom,noticeArrowX=notice==null?0:notice.ArrowTip.X,noticeArrowY=notice==null?0:notice.ArrowTip.Y,noticeArrowUp=notice!=null&&notice.Below,noticeFooterBottom=NoticeFooterBottom,noticeJumpFrame=NoticeJumpFrame,displayAnimationRow=NoticeJumpFrame>=0?4:row,bubbleLift=notice==null?0:notice.BumpPixels,userHidden=userHidden,fullscreenSuppressed=fullscreenSuppressed,noticeBelow=notice!=null&&notice.Below,noticeKind=notice==null?"":notice.Kind,jumpOffset=JumpOffset,dockActivityGap=settings.DockActivityGap,edgePreview=edgePreview,qa=Program.Qa,petVisible=Visible,showInTaskbar=ShowInTaskbar,docked=docked,docking=docking,edge=edge,scale=scale,lookIndex=lookIndex,animationRow=row,toolbarExpanded=toolbarAmount>.8,quotaVisible=usageCard!=null&&usageCard.Visible,dockQuotaVisible=dockQuota!=null&&dockQuota.Visible,taskBadgeVisible=taskBadge!=null&&taskBadge.Visible,thinking=IsThinking,activityScale=settings.ActivityScale,noticeOffsetY=settings.NoticeOffsetY,noticeAnimating=notice!=null&&notice.Animating,pendingNotices=pendingNotices.Count,noticeText=notice==null?"":notice.Headline,lastCommand=lastCommand,toolbarTop=Top+ToolbarRect.Top,toolbarHeight=ToolbarRect.Height,quotaTop=usageCard==null?0:usageCard.WhiteBounds.Top,quotaOrientation=QuotaIsVertical?"vertical":"horizontal",quotaWhiteBounds=usageCard==null?Rectangle.Empty:usageCard.WhiteBounds,compactTop=dockQuota==null?0:dockQuota.Top+36,activeTasks=activeTasks,localActiveTasks=localActiveTasks,appActiveTasks=appActiveTasks,ordinaryChatMonitoring=settings.AppChatMonitoring&&settings.OrdinaryChatMonitoring,workAppMonitoring=settings.AppChatMonitoring&&settings.WorkAppMonitoring,taskNavigation=taskNavigation,appBridgeState=appChats==null?"off":appChats.State,appBridgeConnected=appChats!=null&&appChats.Connected,appChatsKnown=appChats==null?0:appChats.KnownChats,remoteWorkKnown=appChats==null?0:appChats.KnownRemote,appCompletionEvents=appChats==null?0:appChats.Delivered,sendLit=composer!=null&&composer.SendLit,activityBadgeLeft=taskBadge==null?0:taskBadge.Left,activityBadgeTop=taskBadge==null?0:taskBadge.Top,showUpdatedAt=settings.ShowUpdatedAt,fiveHourWarning=settings.FiveHourWarning,weeklyWarning=settings.WeeklyWarning,idleFrameMs=500,occupiedHeight=OccupiedHeight,body=new{Left,Top,Width,Height},composerVisible=ComposerVisible,chatTarget=composer==null?ChatRouting.NormalizeTarget(settings.ChatTarget):composer.SelectedChatTarget,usageAvailable=Usage.Snapshot!=null,completionWatchActive=completion!=null,completionEvents=completion==null?0:completion.Delivered,terminalHandled=terminalHandled,terminalDuplicates=terminalDuplicates,inputReminderCount=inputNotices,pendingInputRequests=pendingInputs.Count,inputReminders=settings.InputReminders,noticeVisible=notice!=null&&notice.Visible}),Encoding.UTF8);}catch{}}
        protected override void PaintSurface(Graphics g){
            g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.SmoothingMode=SmoothingMode.None;
            if(docked){var pose=bank.PeekPose(edge);g.DrawImage(pose,new Rectangle(0,0,Width,Height),bank.PeekSource(edge),GraphicsUnit.Pixel);return;}
            int jumpFrame=NoticeJumpFrame;bool gaze=jumpFrame<0&&lookIndex>=0&&!IsThinking&&!held&&!docking;Bitmap art=jumpFrame>=0?bank.Frames[4][jumpFrame]:gaze?(bank.Looks[lookIndex]??bank.Frames[0][0]):bank.Frames[row][frame];Point offset=jumpFrame<0&&!gaze&&bank.ResolvedFrom[row]=="idle"?bank.IdleOffsets[frame]:Point.Empty;int artWidth=(int)Math.Round(192*scale);g.DrawImage(art,new Rectangle((Width-artWidth)/2+(int)(offset.X*scale),(int)(offset.Y*scale)+JumpOffset,artWidth,ArtHeight),0,0,192,208,GraphicsUnit.Pixel);
            if(held&&!String.IsNullOrEmpty(edgePreview))DrawEdgePreview(g);
            if(!ComposerVisible&&!docking){g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.Default;var pill=ToolbarRect;Bubble(g,pill,pill.Height/2f,settings);if(toolbarAmount>.8){Design.Icon(g,"pencil",new Rectangle(pill.X+8,pill.Y+4,32,32),Design.Ink);Design.Icon(g,"usage",new Rectangle(pill.X+54,pill.Y+4,32,32),Design.Ink);Design.Icon(g,"more",new Rectangle(pill.X+100,pill.Y+4,32,32),Design.Ink);using(var pen=new Pen(Design.Line)){g.DrawLine(pen,pill.X+47,pill.Y+13,pill.X+47,pill.Y+27);g.DrawLine(pen,pill.X+93,pill.Y+13,pill.X+93,pill.Y+27);}}if(unread)using(var b=new SolidBrush(Design.Peach))g.FillEllipse(b,pill.Right-7,pill.Top+2,4,4);}
        }
    }

    static class Checks {
        public static void Run(){CharacterChecks.Run();NoticeClickPolicy.Verify();CompletionNoticePolicy.Verify();V6Checks.Run();int count=0;using(var bank=new SpriteBank()){foreach(var row in bank.Frames)foreach(var frame in row){if(SpriteBank.Bounds(frame).IsEmpty)throw new Exception("Blank frame");count++;}}
            var example=new Dictionary<string,object>{{"rateLimits",new Dictionary<string,object>{{"primary",new Dictionary<string,object>{{"usedPercent",8},{"windowDurationMins",300},{"resetsAt",1792515015L}}},{"secondary",new Dictionary<string,object>{{"usedPercent",11},{"windowDurationMins",10080},{"resetsAt",1793083808L}}}}}};
            var parsed=UsageService.Parse(example);if(parsed.Primary.Remaining!=92||parsed.Secondary.Remaining!=89)throw new Exception("Remaining quota conversion");
            if(PetForm.DetectEdge(new Rectangle(-2,100,192,260),new Rectangle(0,0,1920,1080))!="left")throw new Exception("Left docking");if(PetForm.DetectEdge(new Rectangle(1728,100,192,260),new Rectangle(0,0,1920,1080))!="right")throw new Exception("Right docking");
            var area=new Rectangle(0,0,1920,1080);
            if(PetForm.DetectEdge(new Rectangle(600,0,192,260),area)!=null||PetForm.DetectEdge(new Rectangle(600,820,192,260),area)!=null)throw new Exception("Vertical edges must not hide");
            var origin=new Point(100,100);if(PetForm.LookDirection(origin,new Point(100,0))!=0||PetForm.LookDirection(origin,new Point(200,100))!=4||PetForm.LookDirection(origin,new Point(100,200))!=8||PetForm.LookDirection(origin,new Point(0,100))!=12)throw new Exception("Gaze directions");
            int looks=0;using(var bank=new SpriteBank()){foreach(var f in bank.Looks){if(f!=null){if(SpriteBank.Bounds(f).IsEmpty)throw new Exception("Blank gaze frame");looks++;}}}
            using(var image=new Bitmap(220,110,PixelFormat.Format32bppPArgb)){using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;AlphaForm.Bubble(g,new RectangleF(24,24,172,56),28,new PetSettings());}bool partial=false;for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++){int alpha=image.GetPixel(x,y).A;if(alpha>0&&alpha<255)partial=true;}if(!partial||image.GetPixel(110,50).A!=255||image.GetPixel(0,0).A!=0)throw new Exception("Smooth alpha edges");image.Save(Path.Combine(Program.DataRoot,"capsule-alpha-test.png"));}
            var activity=new TaskActivityReducer();
            string started="{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_started\",\"turn_id\":\"one\"}}";
            string completed="{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_complete\",\"turn_id\":\"one\"}}";
            if(!activity.Apply(started,"a")||activity.Count!=1)throw new Exception("Activity start");activity.Apply(started,"b");if(activity.Count!=2)throw new Exception("Concurrent activity");activity.Apply(started.Replace("one","two"),"a");activity.Apply(completed,"a");if(activity.Count!=2)throw new Exception("Old completion must not clear newer task");activity.Apply(completed,"b");activity.Apply(completed.Replace("one","two").Replace("task_complete","turn_aborted"),"a");if(activity.Count!=0)throw new Exception("Complete and abort clear activity");if(activity.Apply(started.Replace("event_msg","response_item"),"spoof"))throw new Exception("Only lifecycle records affect activity");
            string bootstrapPath=Path.Combine(Program.DataRoot,"qa-activity-bootstrap.jsonl");File.WriteAllText(bootstrapPath,started+"\n");var restored=new TaskActivityReducer();restored.Bootstrap(bootstrapPath);if(restored.Count!=1)throw new Exception("Restore in-progress task on startup");File.AppendAllText(bootstrapPath,completed+"\n");var finished=new TaskActivityReducer();finished.Bootstrap(bootstrapPath);if(finished.Count!=0)throw new Exception("Do not restore already finished task");
            var layout=new QuotaLayout();using(var bitmap=new Bitmap(640,160))using(var g=Graphics.FromImage(bitmap)){var samples=new[]{"5 小时","100%","00:50 重置","本周","100%","10月27日 14:50 重置","更新于 12:30:59"};for(int i=0;i<6;i++)if(layout.Bounds(g,samples[i],i).Right>layout.Width-8)throw new Exception("Default quota text overflows");}
            string sample="{\"type\":\"event_msg\",\"payload\":{\"type\":\"task_complete\",\"turn_id\":\"sample\"}}";var evt=CompletionWatcher.Parse(sample,"rollout-2026-10-20T00-00-00-11111111-1111-4111-8111-111111111111.jsonl");if(evt==null||evt.TurnId!="sample")throw new Exception("Completion parser");
            string qa=Path.Combine(Program.DataRoot,"qa-sessions");Directory.CreateDirectory(qa);File.WriteAllText(Path.Combine(qa,"rollout-test-11111111-1111-4111-8111-111111111111.jsonl"),"{\"type\":\"session_meta\",\"payload\":{\"source\":\"vscode\"}}\n",new System.Text.UTF8Encoding(false));
            File.WriteAllText(Path.Combine(Program.DataRoot,"verification-v6.json"),new JavaScriptSerializer().Serialize(new{passed=true,popup_bounce_endpoints=true,activity_start_complete_abort_multitask=true,activity_startup_restore=true,quota_default_text_fits=true,frames=count,lookFrames=looks,idleFps=2,vertical_docking_disabled=true,quota_alarm_threshold_dedup_reset=true,alpha_edges=true,gaze_directions=true,quota_remaining_conversion=true,edge_detection=true,completion_parser=true,image_generation_calls=0}),Encoding.UTF8);
        }
    }
}

