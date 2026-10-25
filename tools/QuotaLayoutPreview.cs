// Offline preview using the product's own drawing code and synthetic quota text.
// Compile with /main:PetBridge.QuotaLayoutPreview alongside src/*.cs.
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace PetBridge {
    sealed partial class PetForm {
        public Bitmap RenderQuotaComparison(){
            clock.Stop();Usage.Dispose();tray.Visible=false;settings.ShowUsage=true;
            scale=1.2;settings.Scale=scale;SetSize();docked=false;docking=false;
            busy=false;activeTasks=0;lookIndex=-1;row=0;frame=0;toolbarAmount=0;
            var result=new Bitmap(1000,550);result.SetResolution(120,120);
            using(var g=Graphics.FromImage(result)){
                g.Clear(Color.FromArgb(243,245,250));g.SmoothingMode=SmoothingMode.AntiAlias;
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                for(int i=0;i<2;i++){
                    settings.QuotaLayoutMode=i==0?"horizontal":"vertical";
                    using(var f=new Font("Microsoft YaHei UI",12,FontStyle.Bold))using(var brush=new SolidBrush(Design.Ink))
                        g.DrawString(i==0?"宽敞位置 · 横排":"靠近左右边缘 · 竖排",f,brush,38+i*500,20);
                    using(var body=new Bitmap(Width,Height)){
                        body.SetResolution(120,120);
                        using(var bg=Graphics.FromImage(body)){bg.Clear(Color.Transparent);PaintSurface(bg);}
                        int x=250+i*500-Width/2;g.DrawImageUnscaled(body,x,72);
                    }
                    var l=CurrentQuotaLayout;var saved=g.Save();
                    g.TranslateTransform(250+i*500-l.Width/2+l.OffsetX,72+ToolbarBottom+settings.UsageGap+l.OffsetY);
                    l.PaintBubble(g,new[]{"5 小时","50%","19:36 重置","本周","84%","10月30日 09:18 重置","更新于 16:00:00"},settings,false);
                    g.Restore(saved);
                }
                using(var f=new Font("Microsoft YaHei UI",8))using(var brush=new SolidBrush(Design.Muted))
                    g.DrawString("产品界面离线渲染 · 额度为示例数值 · 横竖排独立保存",f,brush,38,510);
            }
            return result;
        }
    }
    static class QuotaLayoutPreview {
        [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        [STAThread] public static void Main(string[] args){
            try{
                if(args.Length!=2)throw new ArgumentException("project directory and output PNG required");
                try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{}
                Program.Qa=true;
#if !PRIVATE_PET
                Program.Testing=true;Program.Offline=true;
                Program.CharacterOverride=Path.Combine(Path.GetFullPath(args[0]),"characters","default","character.json");
#endif
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(var p=new PetForm())using(var image=p.RenderQuotaComparison())image.Save(Path.GetFullPath(args[1]),ImageFormat.Png);
            }catch(Exception e){File.WriteAllText(Path.GetFullPath(args[1])+".error.txt",e.ToString());Environment.ExitCode=1;}
        }
    }
}
