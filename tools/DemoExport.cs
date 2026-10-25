// Offline documentation renderer. Compile separately with scripts/Export-Demos.ps1.
// Uses real product paint/control code with synthetic state; never opens conversations.
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
namespace PetBridge {
    sealed partial class PetForm {
        public void PrepareDocumentation(){clock.Stop();tray.Visible=false;scale=1.5;settings.Scale=scale;settings.ToolbarGap=-4;SetSize();Location=new Point(300,300);Usage.Snapshot=new UsageSnapshot{Primary=new Quota{Remaining=63,WindowMinutes=300,ResetsAt=new DateTimeOffset(2030,10,21,2,11,0,TimeSpan.FromHours(8)).ToUnixTimeSeconds()},Secondary=new Quota{Remaining=82,WindowMinutes=10080,ResetsAt=new DateTimeOffset(2030,10,27,14,50,0,TimeSpan.FromHours(8)).ToUnixTimeSeconds()}};}
        public Bitmap DocumentationBody(int animation,int index,bool expanded){docked=false;docking=false;busy=animation==7;lookIndex=-1;row=animation;frame=index;toolbarAmount=expanded?1:0;var b=new Bitmap(Width,Height);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);PaintSurface(g);}return b;}
        public Bitmap DocumentationComposerBody(ChatForm value){composer=value;docked=false;busy=false;row=0;frame=0;lookIndex=8;var b=new Bitmap(Width,Height);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);PaintSurface(g);}composer=null;lookIndex=-1;return b;}
        public Bitmap DocumentationPeek(string side){var pose=bank.PeekPose(side);var source=bank.PeekSource(side);var b=new Bitmap((int)(source.Width*scale),(int)(source.Height*scale));using(var g=Graphics.FromImage(b)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(pose,new Rectangle(0,0,b.Width,b.Height),source,GraphicsUnit.Pixel);}return b;}
        public Bitmap DocumentationMenu(){menu.PerformLayout();menu.Size=menu.GetPreferredSize(Size.Empty);var b=new Bitmap(menu.Width,menu.Height);menu.DrawToBitmap(b,new Rectangle(Point.Empty,menu.Size));return b;}
    }
    static class DemoExport {
        [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        static string output;
        static Font font=new Font("Microsoft YaHei UI",13),title=new Font("Microsoft YaHei UI",22,FontStyle.Bold),small=new Font("Microsoft YaHei UI",10);
        static void Set(object obj,string field,object value){obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);}
        static Bitmap Painted(AlphaForm form){var b=new Bitmap(form.Width,form.Height);using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;form.GetType().GetMethod("PaintSurface",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,new object[]{g});}return b;}
        static Bitmap ControlsImage(Control c){var form=c as Form;if(form!=null&&!form.Visible){form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-16000,-16000);form.TopMost=false;form.Show();}c.CreateControl();foreach(Control child in c.Controls)child.CreateControl();c.PerformLayout();var b=new Bitmap(c.Width,c.Height);c.DrawToBitmap(b,new Rectangle(Point.Empty,c.Size));return b;}
        static Bitmap Canvas(int w,int h,string heading){var b=new Bitmap(w,h);using(var g=Graphics.FromImage(b)){g.Clear(Color.FromArgb(242,244,250));g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.DrawString(heading,title,Brushes.Black,32,22);g.DrawString("界面渲染演示 · 额度和通知为示例数据",small,Brushes.Gray,34,h-34);}return b;}
        static void Label(Graphics g,string s,int x,int y){g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;g.DrawString(s,font,Brushes.DimGray,x,y);}
        static void Save(Bitmap b,string name){b.Save(Path.Combine(output,name),ImageFormat.Png);b.Dispose();}
        static Bitmap Notification(PetForm p,string kind,string headline,string detail){using(var n=new NoticeCard(p)){Set(n,"kind",kind);Set(n,"headline",headline);Set(n,"detail",detail);Set(n,"progress",1.0);Set(n,"anchorX",194f);return Painted(n);}}
        [STAThread] public static void Main(string[] args){
            if(args.Length<2||args.Length>3)throw new ArgumentException("project root and media output required");
            try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{}
            output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);
            Environment.SetEnvironmentVariable("PETBRIDGE_DATA_DIR",Path.Combine(Path.GetTempPath(),"petbridge-docs-"+Guid.NewGuid().ToString("N")));
            Program.Qa=true;Program.Testing=true;Program.Offline=true;Program.CharacterOverride=Path.Combine(Path.GetFullPath(args[0]),"characters","default","character.json");
            Directory.CreateDirectory(Program.DataRoot);
            using(var dpiProbe=new Bitmap(10,10))using(var display=Graphics.FromHwnd(IntPtr.Zero)){File.WriteAllText(Path.Combine(Program.DataRoot,"render-dpi.txt"),"bitmap="+dpiProbe.HorizontalResolution+", screen="+display.DpiX);}
            Directory.CreateDirectory(Program.DataRoot);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            using(var p=new PetForm()){
                p.PrepareDocumentation();
                var hero=Canvas(1400,570,"ChatGPT 桌面端宠物  /  陪你聊天和工作");using(var g=Graphics.FromImage(hero))using(var pet=p.DocumentationBody(0,0,true)){g.DrawImageUnscaled(pet,970,100);Label(g,"你的角色 · 你的 Codex · 你的本机配置",56,132);Label(g,"任务状态   /   提问提醒   /   Codex 额度   /   自定义动画",56,205);Label(g,"Windows x64     开放源码     中文适配指南",56,278);Label(g,"先看演示，再把项目交给自己的 ChatGPT / Codex。",56,365);}Save(hero,"hero.png");if(args.Length==3&&args[2]=="--hero-only")return;
                var toolbar=Canvas(1000,660,"悬停展开工具条 · 额度跟随布局");using(var g=Graphics.FromImage(toolbar)){using(var body=p.DocumentationBody(0,0,false))g.DrawImageUnscaled(body,80,95);using(var body=p.DocumentationBody(0,0,true))g.DrawImageUnscaled(body,570,95);Label(g,"平时收起，贴近脚边",52,565);Label(g,"悬停展开：聊天 / 额度 / 菜单",530,565);using(var quota=new UsageCard(p,p.Usage))using(var b=Painted(quota)){g.DrawImage(b,new Rectangle(495,430,b.Width,b.Height));}}Save(toolbar,"toolbar-quota.png");
                using(var quota=new UsageCard(p,p.Usage))using(var closeup=Painted(quota)){closeup.Save(Path.Combine(output,"quota-closeup.png"),ImageFormat.Png);}
                for(int mode=0;mode<2;mode++){
                    var native=new Bitmap(440,485);using(var g=Graphics.FromImage(native)){
                        g.Clear(Color.FromArgb(242,244,250));using(var body=p.DocumentationBody(0,0,mode==1))g.DrawImageUnscaled(body,76,15);
                        using(var quota=new UsageCard(p,p.Usage))using(var image=Painted(quota))g.DrawImageUnscaled(image,(440-image.Width)/2,15+p.ToolbarBottom+p.Settings.UsageGap-36);
                    }Save(native,mode==1?"toolbar-expanded-native.png":"toolbar-collapsed-native.png");
                }
                var notifications=Canvas(1260,460,"五种通知状态 · 颜色与柔光可调");string[] kinds={"complete","low","empty","reset","input"},heads={"任务完成啦","额度低于设定值","5 小时已用完啦","5 小时额度已重置","有问题需要你回答"},details={"点击收起；可改成跳转对话","5 小时剩余 15% · 本周 82%","等待 2 小时 15 分钟后重置","现在可以继续工作啦","点击回到桌面对应对话"};using(var g=Graphics.FromImage(notifications)){for(int i=0;i<5;i++)using(var b=Notification(p,kinds[i],heads[i],details[i]))g.DrawImageUnscaled(b,10+(i%3)*414,75+(i/3)*160);}Save(notifications,"notifications.png");
                var activity=Canvas(1000,670,"任务进行中 · 输入时注视光标");using(var g=Graphics.FromImage(activity)){using(var b=p.DocumentationBody(7,1,false))g.DrawImageUnscaled(b,90,90);using(var badge=new TaskBadge(p)){badge.Place();using(var b=Painted(badge))g.DrawImageUnscaled(b,320,210);}using(var chat=new ChatForm(p)){var input=(TextBox)typeof(ChatForm).GetField("input",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(chat);input.Text="你好，陪我完成今天的任务吧";using(var b=ControlsImage(chat)){AlphaForm.Bubble(g,new RectangleF(467,440,500,chat.Height),22,p.Settings);using(var clip=Design.Round(new RectangleF(467,440,500,chat.Height),22)){var state=g.Save();g.SetClip(clip);g.DrawImageUnscaled(b,467,440);g.Restore(state);}}using(var body=p.DocumentationComposerBody(chat))g.DrawImageUnscaled(body,625,90);}Label(g,"持续思考 + 三点加载状态",45,595);Label(g,"可选择聊天目标 · 此图没有发送消息",507,595);}Save(activity,"activity-chat.png");
                var edges=Canvas(1060,490,"左右贴边 · 点击展开 · 任务通知唤出");using(var g=Graphics.FromImage(edges)){using(var l=p.DocumentationPeek("left"))g.DrawImageUnscaled(l,25,105);using(var r=p.DocumentationPeek("right"))g.DrawImageUnscaled(r,920,105);Label(g,"左边：状态显示在人物右侧",170,200);Label(g,"右边：状态显示在人物左侧",530,200);using(var badge=new TaskBadge(p)){badge.Place();using(var b=Painted(badge)){g.DrawImageUnscaled(b,105,195);g.DrawImageUnscaled(b,810,195);}}using(var card=new DockQuotaCard(p))using(var b=Painted(card)){g.DrawImageUnscaled(b,180,280);g.DrawImageUnscaled(b,535,280);}Label(g,"悬停只显示极简额度；上下边缘不触发隐藏",230,390);}Save(edges,"edges.png");
                var menus=Canvas(1080,650,"精简右键菜单 · 数字、字体和位置可手调");using(var g=Graphics.FromImage(menus)){using(var b=p.DocumentationMenu())g.DrawImageUnscaled(b,40,115);using(var editor=new LayoutEditor(p))using(var b=ControlsImage(editor))g.DrawImage(b,new Rectangle(330,100,710,485));}Save(menus,"menu-layout.png");
                using(var settings=new SettingsForm(p)){
                    settings.TopMost=false;settings.StartPosition=FormStartPosition.Manual;settings.Location=new Point(-16000,-16000);settings.Show();
                    var buttons=(List<SettingsNavButton>)typeof(SettingsShell).GetField("buttons",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(settings);
                    for(int group=0;group<3;group++){var b=Canvas(1580,1550,"详细设置 / "+(group+1)+" · 拖动顶部移动窗口");using(var g=Graphics.FromImage(b)){for(int i=0;i<4;i++){int index=group*4+i;buttons[index].PerformClick();settings.PerformLayout();using(var page=ControlsImage(settings))g.DrawImageUnscaled(page,20+(i%2)*790,85+(i/2)*700);}}Save(b,"settings-"+(group+1)+".png");}settings.Close();
                }
                p.Dispose();
            }
        }
    }
}
