using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using System.Windows.Forms;
namespace PetBridge {
    sealed class NoticeCard : AlphaForm {
        readonly PetForm pet;string link="",headline="",detail="",kind="complete";readonly Timer pop=new Timer();readonly Stopwatch clock=new Stopwatch();double progress=1;
        public bool Animating {get{return pop.Enabled;}}
        public string Headline {get{return headline;}}
        public string Kind {get{return kind;}}
        public bool Below {get;private set;}public int BumpPixels {get;private set;}float anchorX=194;
        public Rectangle WhiteBounds {get{return new Rectangle(Left+36,Top+36,NoticeLayout.Width,NoticeLayout.Height);}}
        public PointF ArrowTip {get{var points=NoticeLayout.Tail(new RectangleF(36,36,NoticeLayout.Width,NoticeLayout.Height),Below,anchorX);return new PointF(Left+points[1].X,Top+points[1].Y);}}
        protected override bool ShowWithoutActivation {get{return true;}}
        public NoticeCard(PetForm p){pet=p;Text="搭档 · 提醒";Size=new Size(388,154);Cursor=Cursors.Hand;Click+=delegate{Dismiss();};pop.Interval=16;pop.Tick+=delegate{progress=pet.Settings.NoticeBounce?Math.Min(1,clock.Elapsed.TotalMilliseconds/620):1;Place();Invalidate();if(progress>=1&&!pet.NoticeJumpRunning)pop.Stop();};VisibleChanged+=delegate{if(!Visible){pop.Stop();pet.StopNoticeJump();}};FormClosed+=delegate{pop.Dispose();};}
        public void Dismiss(){if(!Visible)return;string target=link;bool navigate=NoticeClickPolicy.ShouldNavigate(kind,target,pet.Settings.NavigateOnTaskNoticeClick);Hide();pet.NoticeDismissed();if(navigate)pet.OpenTask(target);}
        public static double Expand(double t){t=Math.Max(0,Math.Min(1,t));double u=t-1;return 1+2.4*u*u*u+1.4*u*u;}
        public static bool NeedsBelow(int headTop,int areaTop,int offset){return headTop-98<areaTop;}
        public static Color ParseColor(string hex){try{return ColorTranslator.FromHtml(hex);}catch{return Color.FromArgb(48,164,108);}}
        public void PresentMessage(string title,string text,string target,string state){headline=title;detail=text;link=target;kind=state;pop.Stop();pet.StopNoticeJump();progress=pet.Settings.NoticeBounce?0:1;Place();Show(pet);Invalidate();pet.StartNoticeJump(Below||state=="input");clock.Restart();if(pet.Settings.NoticeBounce||pet.NoticeJumpRunning)pop.Start();}
        public void Place(){var a=Screen.FromControl(pet).WorkingArea;var body=pet.CharacterBounds;var placement=NoticeLayout.Arrange(a,body,pet.NoticeFooterBottom,pet.Settings.NoticeOffsetY);bool oldBelow=Below;float oldAnchor=anchorX;int oldBump=BumpPixels;Point oldLocation=Location;Below=placement.Below;if(Below&&pet.NoticeJumpRunning)pet.StopNoticeJump();var white=placement.WhiteBounds;BumpPixels=Below?0:pet.NoticeBubbleLift(white.Bottom+8,white.Top-a.Top);Location=new Point(white.Left-36,white.Top-36-BumpPixels);anchorX=body.Left+body.Width/2-Left;
            // Location changes move layered pixels but do not repaint their cached tail.
            if(Visible&&(oldBelow!=Below||oldAnchor!=anchorX||oldBump!=BumpPixels||oldLocation!=Location))Invalidate();
        }
        protected override void PaintSurface(Graphics g){
            double t=progress;float grow=(float)Expand(Math.Min(1,t/.87));float w=24+292*grow,h=24+52*grow;float cx=194,cy=74;var r=new RectangleF(cx-w/2,cy-h/2,w,h);
            Bubble(g,r,Math.Min(22,h/2),pet.Settings);
            int alpha=(int)(255*Math.Max(0,Math.Min(1,(t-.45)/.4)));
            if(alpha>0)using(var b=new SolidBrush(Color.FromArgb(alpha,Color.White))){g.FillPolygon(b,NoticeLayout.Tail(r,Below,anchorX));}
            string hex=kind=="empty"?pet.Settings.EmptyColor:(kind=="low"||kind=="input")?pet.Settings.LowColor:kind=="reset"?pet.Settings.ResetColor:pet.Settings.CompleteColor;Color c=ParseColor(hex);
            float dotX=r.Right-32,dotY=cy;int radius=pet.Settings.NoticeGlowRadius;
            if(alpha>0){for(int i=radius;i>0;i--){int glow=(int)(alpha*pet.Settings.NoticeGlow/100.0*.055*(1-i/(double)(radius+1)));using(var b=new SolidBrush(Color.FromArgb(glow,c)))g.FillEllipse(b,dotX-8-i,dotY-8-i,16+2*i,16+2*i);}using(var b=new SolidBrush(Color.FromArgb(alpha,c)))g.FillEllipse(b,dotX-8,dotY-8,16,16);}
            if(alpha>0)using(var f=new Font("Microsoft YaHei UI",10,FontStyle.Bold))using(var sm=new Font("Microsoft YaHei UI",9))using(var ink=new SolidBrush(Color.FromArgb(alpha,Design.Ink)))using(var muted=new SolidBrush(Color.FromArgb(alpha,Color.FromArgb(118,118,118))))using(var fmt=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap}){
                g.DrawString(headline,f,ink,new RectangleF(r.Left+20,cy-22,248,25),fmt);g.DrawString(kind=="complete"&&!String.IsNullOrEmpty(link)?NoticeClickPolicy.CompletionText(pet.Settings.NavigateOnTaskNoticeClick):detail,sm,muted,new RectangleF(r.Left+20,cy+3,244,26),fmt);
            }
        }
    }
}
