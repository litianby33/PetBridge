using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PetBridge {
    // Per-pixel alpha preserves both hard sprite pixels and soft UI edges.
    class AlphaForm : Form {
        [StructLayout(LayoutKind.Sequential)] struct XY {public int x,y;public XY(int a,int b){x=a;y=b;}}
        [StructLayout(LayoutKind.Sequential)] struct WH {public int w,h;public WH(int a,int b){w=a;h=b;}}
        [StructLayout(LayoutKind.Sequential,Pack=1)] struct Blend {public byte op,flags,alpha,format;}
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h,IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dst,ref XY location,ref WH size,IntPtr src,ref XY origin,uint key,ref Blend blend,uint flags);
        bool rendering;
        public AlphaForm(){FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;ShowInTaskbar=Program.DebugTaskbar;AutoScaleMode=AutoScaleMode.None;TopMost=true;}
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x80000;if(!Program.DebugTaskbar){p.ExStyle|=0x80;p.ExStyle&=~0x40000;}return p;}}
        protected override bool ShowWithoutActivation {get{return true;}}
        protected virtual void PaintSurface(Graphics g){}
        protected override void OnInvalidated(InvalidateEventArgs e){base.OnInvalidated(e);RefreshSurface();}
        protected override void OnShown(EventArgs e){base.OnShown(e);RefreshSurface();}
        public void RefreshSurface(){if(rendering||!IsHandleCreated||!Visible||Width<1||Height<1)return;rendering=true;try{
            using(var b=new Bitmap(Width,Height,PixelFormat.Format32bppPArgb)){
                using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;PaintSurface(g);}
                IntPtr screen=GetDC(IntPtr.Zero),mem=CreateCompatibleDC(screen),bitmap=b.GetHbitmap(Color.FromArgb(0)),old=SelectObject(mem,bitmap);
                try{var pos=new XY(Left,Top);var sz=new WH(Width,Height);var origin=new XY(0,0);var blend=new Blend{alpha=255,format=1};if(!UpdateLayeredWindow(Handle,screen,ref pos,ref sz,mem,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception();}
                finally{SelectObject(mem,old);DeleteObject(bitmap);DeleteDC(mem);ReleaseDC(IntPtr.Zero,screen);}
            }
        }finally{rendering=false;}}
        public static void Bubble(Graphics g,RectangleF rect,float radius,PetSettings s){
            float blur=s.ShadowBlur;int strength=s.ShadowOpacity;
            if(strength>0&&blur>0){int steps=Math.Max(4,(int)blur);for(int i=steps;i>=1;i--){float grow=blur*i/steps;float a=(1f-i/(float)(steps+1));int alpha=Math.Max(1,(int)(strength*.10*a*a));using(var path=Design.Round(new RectangleF(rect.X-grow,rect.Y-grow+s.ShadowOffset,rect.Width+grow*2,rect.Height+grow*2),radius+grow))using(var brush=new SolidBrush(Color.FromArgb(alpha,35,37,43)))g.FillPath(brush,path);}}
            using(var path=Design.Round(rect,radius))using(var brush=new SolidBrush(Color.White))using(var pen=new Pen(Color.FromArgb(235,235,235),.8f)){g.FillPath(brush,path);g.DrawPath(pen,path);}
        }
    }
    // The transparent skin draws just the rounded perimeter; native text controls
    // remain in the opaque interior, with no color-key pixels along the outside.
    sealed class CardSkin : AlphaForm {
        readonly RoundedForm parent;
        public CardSkin(RoundedForm p){parent=p;ShowInTaskbar=false;}
        protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x20|0x8000000;return p;}}
        protected override void PaintSurface(Graphics g){g.TranslateTransform(12,12);Bubble(g,new RectangleF(24,24,parent.Width,parent.Height),Math.Min(22,parent.Height/2f),parent.VisualSettings);g.CompositingMode=CompositingMode.SourceCopy;using(var b=new SolidBrush(Color.Transparent))g.FillRectangle(b,38,38,parent.Width-28,parent.Height-28);}
        public void Sync(){if(parent.IsDisposed||!parent.Visible){Hide();return;}Bounds=new Rectangle(parent.Left-36,parent.Top-36,parent.Width+72,parent.Height+72);if(!Visible)Show(parent);RefreshSurface();}
    }
}
