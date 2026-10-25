using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
namespace PetBridge {
    sealed partial class PetForm {
        readonly TaskEndLedger taskEnds=new TaskEndLedger(System.IO.Path.Combine(Program.DataRoot,Program.Qa?"terminal-reminders.qa.json":"terminal-reminders.json"));
        int terminalHandled,terminalDuplicates;bool userHidden,fullscreenSuppressed,resetPreview;int pendingTaskQuota;DateTime fullscreenSeen=DateTime.MinValue;IntPtr fullscreenWindow;
        Icon petTrayIcon;string edgePreview="";
        readonly List<Form> suspendedForms=new List<Form>();
        readonly Stopwatch jumpWatch=new Stopwatch();
        int loggedJumpFrame=-2,jumpRepeats=3;
        int JumpOffset {get{return !bank.HasNativeJump&&NoticeJumpRunning?-(int)Math.Round(24*scale*NoticeJumpMotion.SimulatedPush(jumpWatch.Elapsed.TotalMilliseconds,bank.JumpCycle)):0;}}
        public bool NoticeJumpRunning {get{return jumpWatch.IsRunning&&jumpWatch.Elapsed.TotalMilliseconds<NoticeJumpMotion.Delay+bank.JumpCycle*jumpRepeats;}}
        int NoticeJumpFrame {get{return NoticeJumpRunning?NoticeJumpMotion.FrameAt(jumpWatch.Elapsed.TotalMilliseconds,bank.Counts[4],bank.Delays[4],bank.JumpCycle):-1;}}
        public int NoticeBubbleLift(int tailY,int available){if(!NoticeJumpRunning||NoticeJumpFrame<0)return 0;int head=Top+(int)Math.Round(bank.JumpHeadTops[NoticeJumpFrame]*scale)+JumpOffset;return Math.Max(0,Math.Min(available,tailY-head+2));}
        public void StopNoticeJump(){jumpWatch.Reset();Invalidate();}
        void TickNoticeJump(){if(!jumpWatch.IsRunning)return;int current=NoticeJumpFrame;if(Program.Qa&&(current!=loggedJumpFrame||!bank.HasNativeJump)){loggedJumpFrame=current;System.IO.File.AppendAllText(System.IO.Path.Combine(Program.DataRoot,"notice-jump.qa.jsonl"),new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(new{ms=jumpWatch.ElapsedMilliseconds,spriteRow=current>=0?4:row,spriteFrame=current,translationY=JumpOffset,bodyX=Left,bodyY=Top,bubbleLift=notice==null?0:notice.BumpPixels})+Environment.NewLine);}if(!NoticeJumpRunning)jumpWatch.Reset();Invalidate();}
        public void StartNoticeJump(bool below){jumpRepeats=3;jumpWatch.Reset();loggedJumpFrame=-2;if(Program.Qa)System.IO.File.WriteAllText(System.IO.Path.Combine(Program.DataRoot,"notice-jump.qa.jsonl"),"");if(!below)jumpWatch.Start();Invalidate();}
        Icon CreateTrayIcon(){try{petTrayIcon=new Icon(System.IO.Path.Combine(Program.Root,"assets","app.ico"));}catch{petTrayIcon=(Icon)SystemIcons.Application.Clone();}return petTrayIcon;}
        void DrawEdgePreview(Graphics g){string text=edgePreview=="left"?"松开后收进左边":"松开后收进右边";g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;var r=new RectangleF(Width/2-90,Math.Max(8,(int)(35*scale)),180,30);Bubble(g,r,15,settings);using(var f=new Font("Microsoft YaHei UI",9))using(var b=new SolidBrush(Design.Muted))using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(text,f,b,r,format);}
        void CollectVisible(Form parent){foreach(Form f in parent.OwnedForms){CollectVisible(f);if(f.Visible&&!suspendedForms.Contains(f))suspendedForms.Add(f);}}
        void SuspendPet(){CollectVisible(this);foreach(var f in suspendedForms)if(!f.IsDisposed)f.Hide();Hide();DumpState();}
        void ResumePet(){if(userHidden||fullscreenSuppressed)return;Show();for(int i=suspendedForms.Count-1;i>=0;i--){var f=suspendedForms[i];if(!f.IsDisposed)f.Show();}suspendedForms.Clear();EnsureUsage();SyncActivity();if((notice==null||!notice.Visible)&&pendingNotices.Count>0){var next=pendingNotices[0];pendingNotices.RemoveAt(0);BeginInvoke(next.Show);}DumpState();}
        public void TemporarilyHide(){userHidden=true;SuspendPet();}
        public void RestorePet(){userHidden=false;CheckFullscreen();ResumePet();}
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassName(IntPtr h,StringBuilder name,int count);
        public static bool CoversMonitor(Rectangle window,Rectangle monitor){return Math.Abs(window.Left-monitor.Left)<=2&&Math.Abs(window.Top-monitor.Top)<=2&&Math.Abs(window.Right-monitor.Right)<=2&&Math.Abs(window.Bottom-monitor.Bottom)<=2;}
        bool OtherAppFullscreen(){var h=GetForegroundWindow();if(h==IntPtr.Zero||!Program.IsWindowVisible(h)||IsIconic(h))return false;uint pid;GetWindowThreadProcessId(h,out pid);if(pid==(uint)Process.GetCurrentProcess().Id){if(!fullscreenSuppressed||fullscreenWindow==IntPtr.Zero||!Program.IsWindowVisible(fullscreenWindow)||IsIconic(fullscreenWindow))return false;h=fullscreenWindow;}var name=new StringBuilder(256);GetClassName(h,name,256);string cls=name.ToString();if(cls=="Progman"||cls=="WorkerW"||cls=="Shell_TrayWnd"||cls=="Shell_SecondaryTrayWnd")return false;Program.NativeRect r;if(!Program.GetWindowRect(h,out r))return false;bool covers=CoversMonitor(Rectangle.FromLTRB(r.Left,r.Top,r.Right,r.Bottom),Screen.FromHandle(h).Bounds);if(covers)fullscreenWindow=h;return covers;}
        void CheckFullscreen(){bool next=settings.AvoidFullscreen&&OtherAppFullscreen();if(next)fullscreenSeen=DateTime.UtcNow;else if(settings.AvoidFullscreen&&fullscreenSuppressed&&(DateTime.UtcNow-fullscreenSeen).TotalMilliseconds<700)return;if(next==fullscreenSuppressed)return;fullscreenSuppressed=next;if(next)SuspendPet();else ResumePet();}
        void ShowDecision(string key,QuotaDecision decision,string link){
            if(userHidden||fullscreenSuppressed){foreach(var n in pendingNotices)if(n.Key==key)return;if(pendingNotices.Count<12)pendingNotices.Add(new PendingNotice{Key=key,Show=delegate{ShowDecision(key,decision,link);}});return;}
            if(decision.Kind=="input"&&notice!=null&&notice.Visible&&notice.Kind!="input"){if(currentNoticeShow!=null&&pendingNotices.Count<12&&!pendingNotices.Exists(n=>n.Key==noticeKey))pendingNotices.Insert(0,new PendingNotice{Key=noticeKey,Show=currentNoticeShow});notice.Hide();}
            if(QueueNotice(key,delegate{ShowDecision(key,decision,link);}))return;
            currentNoticeShow=delegate{ShowDecision(key,decision,link);};
            if(docked||docking)Undock(true);unread=true;
            if(notice==null||notice.IsDisposed)notice=new NoticeCard(this);
            notice.PresentMessage(decision.Title,decision.Message,link,decision.Kind);DumpState();
        }
        public void PreviewResetOrEmpty(){resetPreview=!resetPreview;HandleCommand(resetPreview?"reset-preview":"empty-preview");}
        public void LocalChatEnded(){OnTaskEnded(new CompletionEvent{ThreadId="",TurnId="local:"+Guid.NewGuid().ToString(),Status="local"});}
        static string CompletionLink(CompletionEvent evt){return !String.IsNullOrEmpty(evt.Link)?evt.Link:String.IsNullOrEmpty(evt.ThreadId)?"":"codex://threads/"+evt.ThreadId;}
        public void RefreshAppBinding(){if(appChats!=null){appChats.Dispose();appChats=null;}appActiveTasks=0;activeTasks=localActiveTasks;SyncAppMonitoring();DumpState();}
        void SyncAppMonitoring(){
            if(Program.Offline)return;
            if(!AppSourcePolicy.NeedsBridge(settings)){if(appChats!=null){appChats.Dispose();appChats=null;}appActiveTasks=0;activeTasks=localActiveTasks;SyncActivity();return;}
            if(appChats!=null){appChats.RefreshPolicy();return;}appChats=new AppChatWatcher(Program.BridgeOrigin(settings),delegate(int count){if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(new Action(delegate{if(!settings.AppChatMonitoring)return;appActiveTasks=appChats==null?0:appChats.ActiveCount;activeTasks=localActiveTasks+appActiveTasks;SyncActivity();DumpState();}));}catch(InvalidOperationException){}},delegate(CompletionEvent e){if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(new Action(delegate{if(settings.AppChatMonitoring)OnTaskEnded(e);}));}catch(InvalidOperationException){}},delegate{if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(new Action(DumpState));}catch(InvalidOperationException){}},null,delegate(InputEvent input){if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(new Action(delegate{OnInputChanged(input);}));}catch(InvalidOperationException){}},t=>AppSourcePolicy.Allows(t,settings));
        }
        void OnTaskEnded(CompletionEvent evt){
            if(!AppSourcePolicy.Allows(evt,settings))return;
            if(!taskEnds.TryClaim(evt)){terminalDuplicates++;DumpState();return;}terminalHandled++;
            if(!evt.UsesCodexQuota){if(CompletionNoticePolicy.CanShow(evt,settings))NotifyCompletion(evt,false);return;}
            // Only a read started after this terminal event may determine its quota notice.
            pendingTaskQuota++;
            Usage.RefreshFresh(delegate(UsageSnapshot snapshot){if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke(new Action(delegate{
                pendingTaskQuota=Math.Max(0,pendingTaskQuota-1);QuotaDecision low=settings.QuotaReminders?alarm.AfterTask(snapshot,settings.FiveHourWarning,settings.WeeklyWarning,DateTimeOffset.UtcNow.ToUnixTimeSeconds()):null;settings.Save();
                string key="task:"+evt.ThreadId+":"+evt.TurnId;
                string link=CompletionLink(evt);
                if(low!=null)ShowDecision(key,low,link);
                else if(CompletionNoticePolicy.CanShow(evt,settings))NotifyCompletion(evt,false);
            }));}catch(InvalidOperationException){} });
        }
    }
}
