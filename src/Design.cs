using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PetBridge {
    static class Design {
        public static readonly Color Ink=Color.FromArgb(54,54,54), Muted=Color.FromArgb(139,139,139), Line=Color.FromArgb(234,233,231), Peach=Color.FromArgb(247,195,164), Key=Color.FromArgb(1,2,3);
        public static GraphicsPath Round(RectangleF r,float radius){var p=new GraphicsPath();float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
        public static void Pill(Graphics g,Rectangle r,Color fill,Color stroke){using(var p=Round(new RectangleF(r.X,r.Y,r.Width-1,r.Height-1),r.Height/2f))using(var b=new SolidBrush(fill))using(var pen=new Pen(stroke)){g.FillPath(b,p);g.DrawPath(pen,p);}}
        public static void Icon(Graphics g,string kind,Rectangle r,Color color){
            g.SmoothingMode=SmoothingMode.AntiAlias;float x=r.X,y=r.Y,w=r.Width,h=r.Height,cx=x+w/2,cy=y+h/2;
            using(var pen=new Pen(color,1.8f)){pen.StartCap=pen.EndCap=LineCap.Round;
                if(kind=="plus"){g.DrawLine(pen,cx-6,cy,cx+6,cy);g.DrawLine(pen,cx,cy-6,cx,cy+6);}
                else if(kind=="send"){g.DrawLine(pen,cx,cy+7,cx,cy-7);g.DrawLine(pen,cx,cy-7,cx-5,cy-2);g.DrawLine(pen,cx,cy-7,cx+5,cy-2);}
                else if(kind=="stop"){using(var b=new SolidBrush(color))g.FillRectangle(b,cx-5,cy-5,10,10);}
                else if(kind=="close"){g.DrawLine(pen,cx-4,cy-4,cx+4,cy+4);g.DrawLine(pen,cx+4,cy-4,cx-4,cy+4);}
                else if(kind=="pencil"){g.DrawPolygon(pen,new PointF[]{new PointF(cx-7,cy+7),new PointF(cx-5,cy+1),new PointF(cx+4,cy-8),new PointF(cx+8,cy-4),new PointF(cx-1,cy+5)});g.DrawLine(pen,cx+2,cy-6,cx+6,cy-2);g.DrawLine(pen,cx-5,cy+1,cx-1,cy+5);}
                else if(kind=="usage"){g.DrawArc(pen,cx-7,cy-7,14,14,130,280);g.DrawLine(pen,cx,cy,cx+4,cy-4);using(var b=new SolidBrush(color))g.FillEllipse(b,cx-1.5f,cy-1.5f,3,3);}
                else if(kind=="chevron"){g.DrawLine(pen,cx-4,cy-2,cx,cy+2);g.DrawLine(pen,cx,cy+2,cx+4,cy-2);}
                else if(kind=="link"){g.DrawLine(pen,cx-4,cy+4,cx+5,cy-5);g.DrawLine(pen,cx+5,cy-5,cx-1,cy-5);g.DrawLine(pen,cx+5,cy-5,cx+5,cy+1);}
                else {using(var b=new SolidBrush(color))for(int i=-1;i<2;i++)g.FillEllipse(b,cx+i*5-1,cy-1,2,2);}
            }
        }
    }
    class RoundedForm : Form {
        CardSkin skin;
        protected override bool ShowWithoutActivation {get{return true;}}
        public PetSettings VisualSettings=new PetSettings();
        public RoundedForm(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=Program.DebugTaskbar;TopMost=true;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;BackColor=Color.White;DoubleBuffered=true;Font=new Font("Microsoft YaHei UI",10);}
        protected override CreateParams CreateParams {get {var c=base.CreateParams;if(!Program.DebugTaskbar){c.ExStyle|=0x80;c.ExStyle&=~0x40000;}return c;}}
        protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width>24&&Height>24){var old=Region;Region=new Region(new Rectangle(12,12,Width-24,Height-24));if(old!=null)old.Dispose();}SyncSkin();}
        public void SyncSkin(){if(skin!=null)skin.Sync();}
        protected override void OnShown(EventArgs e){base.OnShown(e);if(skin==null)skin=new CardSkin(this);SyncSkin();}
        protected override void OnLocationChanged(EventArgs e){base.OnLocationChanged(e);SyncSkin();}
        protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);SyncSkin();}
        protected override void OnFormClosed(FormClosedEventArgs e){if(skin!=null)skin.Dispose();base.OnFormClosed(e);}
    }
    sealed class IconButton : Button {
        public string Kind;public bool Peach,AccentActive;
        public IconButton(string kind,string name){Kind=kind;AccessibleName=name;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;Cursor=Cursors.Hand;TabStop=true;Size=new Size(36,36);}
        protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var b=new SolidBrush(Peach?(AccentActive?Color.FromArgb(245,139,76):Color.FromArgb(247,216,197)):(ClientRectangle.Contains(PointToClient(Cursor.Position))?Color.FromArgb(245,245,245):BackColor)))e.Graphics.FillEllipse(b,2,2,Width-4,Height-4);Design.Icon(e.Graphics,Kind,ClientRectangle,Peach?Color.White:Design.Ink);}
    }
    sealed class ReplyCard : RoundedForm {
        protected override bool ShowWithoutActivation {get{return true;}}
        public readonly RichTextBox Transcript=new RichTextBox();
        public readonly Label Status=new Label();
        readonly PetForm pet;
        bool closing;
        public ReplyCard(PetForm parent){pet=parent;Text="搭档 · 回复";Size=new Size(410,280);
            Status.Text="搭档";Status.SetBounds(20,16,300,24);Status.Font=new Font(Font,FontStyle.Bold);Status.ForeColor=Design.Ink;
            var close=new IconButton("close","收起回复");close.SetBounds(Width-43,10,30,30);close.Click+=delegate{Hide();};
            Transcript.SetBounds(20,54,Width-40,Height-72);Transcript.BorderStyle=BorderStyle.None;Transcript.BackColor=Color.White;Transcript.ReadOnly=true;Transcript.DetectUrls=false;Transcript.Font=Font;Transcript.AccessibleName="聊天记录";Transcript.ForeColor=Design.Ink;
            Controls.AddRange(new Control[]{Status,close,Transcript});FormClosing+=delegate(object s,FormClosingEventArgs e){if(!closing&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
        }
        public void Place(){var a=Screen.FromControl(pet).WorkingArea;int left=pet.Right+10;if(left+Width>a.Right)left=pet.Left-Width-10;if(left<a.Left)left=a.Left+12;Location=new Point(left,Math.Max(a.Top+10,Math.Min(pet.Top+30,a.Bottom-Height-10)));}
        public void Append(string speaker,string text){Transcript.SelectionStart=Transcript.TextLength;Transcript.SelectionColor=speaker=="你"?Color.FromArgb(188,126,85):Design.Ink;Transcript.SelectionFont=new Font(Font,FontStyle.Bold);Transcript.AppendText(speaker+"\n");Transcript.SelectionFont=Font;Transcript.SelectionColor=Design.Ink;Transcript.AppendText(text+"\n\n");Transcript.ScrollToCaret();}
        public void End(){if(IsDisposed)return;closing=true;Close();}
    }
    sealed class UsageCard : AlphaForm {
        readonly UsageService service;readonly PetForm pet;
        public UsageCard(PetForm p,UsageService s){pet=p;service=s;Text="搭档 · 剩余用量";Cursor=Cursors.Hand;UpdateSize();Click+=delegate{service.Refresh();};MouseDown+=delegate(object o,MouseEventArgs e){if(e.Button==MouseButtons.Right)pet.OpenSettings();};service.Changed+=delegate{if(!IsDisposed&&IsHandleCreated)try{BeginInvoke(new Action(Invalidate));}catch(InvalidOperationException){}};}
        public Rectangle WhiteBounds {get{var l=pet.CurrentQuotaLayout;return new Rectangle(Left+36,Top+36,l.Width,l.DisplayHeight(pet.Settings.ShowUpdatedAt));}}
        void UpdateSize(){var l=pet.CurrentQuotaLayout;Size=new Size(l.Width+72,l.DisplayHeight(pet.Settings.ShowUpdatedAt)+72);}
        public void Place(){UpdateSize();var a=Screen.FromControl(pet).WorkingArea;var l=pet.CurrentQuotaLayout;int footer=pet.ComposerVisible?pet.ComposerBottom:pet.Top+pet.ToolbarBottom;var white=QuotaPlacement.WhiteBounds(pet.Bounds,a,l,pet.Settings.ShowUpdatedAt,footer,pet.Settings.UsageGap);Location=new Point(white.Left-36,white.Top-36);Invalidate();}
        protected override void PaintSurface(Graphics g){var s=pet.Settings;var l=pet.CurrentQuotaLayout;g.TranslateTransform(36,36);l.PaintBubble(g,l.Texts(service),s,false);}
    }
}
