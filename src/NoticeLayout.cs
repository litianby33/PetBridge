using System;
using System.Drawing;
namespace PetBridge {
    sealed class NoticePlacement {
        public Rectangle WhiteBounds;public bool Below;
    }
    static class NoticeLayout {
        public const int Width=316,Height=76,FooterGap=24;
        public static int FooterBottom(int bodyBottom,int toolBottom,int usageGap,int quotaHeight){return Math.Max(bodyBottom,toolBottom+(quotaHeight>0?usageGap+quotaHeight:0));}
        public static NoticePlacement Arrange(Rectangle area,Rectangle body,int footerBottom,int offset){
            int aboveTop=body.Top-98+offset;
            bool below=NoticeCard.NeedsBelow(body.Top,area.Top,offset)||aboveTop+Height/2>=body.Top+body.Height/2;
            int top=below?Math.Max(footerBottom+FooterGap,body.Bottom+14+offset):aboveTop;
            top=Math.Max(area.Top,Math.Min(top,area.Bottom-Height));
            int left=Math.Max(area.Left,Math.Min(body.Left+body.Width/2-Width/2,area.Right-Width));
            return new NoticePlacement{Below=below,WhiteBounds=new Rectangle(left,top,Width,Height)};
        }
        public static float TailCenter(float anchor,float left,float width){float pad=Math.Min(28,width/2);return Math.Max(left+pad,Math.Min(anchor,left+width-pad));}
        public static PointF[] Tail(RectangleF bubble,bool below,float anchor){float x=TailCenter(anchor,bubble.Left,bubble.Width),edge=below?bubble.Top:bubble.Bottom;return new[]{new PointF(x-9,edge+(below?1:-1)),new PointF(x,edge+(below?-8:8)),new PointF(x+9,edge+(below?1:-1))};}
    }
    static class NoticeLayoutChecks {
        static void Require(bool good,string name){if(!good)throw new Exception("Notice layout: "+name);}
        public static void Run(){
            var area=new Rectangle(0,0,1920,1040);var body=new Rectangle(500,10,141,228);
            int footer=NoticeLayout.FooterBottom(body.Bottom,283,9,40);var p=NoticeLayout.Arrange(area,body,footer,10);
            Require(p.Below&&p.WhiteBounds.Top>=footer+24,"below reserved toolbar and hover quota");
            var compact=new Rectangle(p.WhiteBounds.Left+43,292,230,40);Require(!p.WhiteBounds.IntersectsWith(compact),"hover quota must not cover reminder");
            var expanded=NoticeLayout.Arrange(area,body,330,10);var collapsed=NoticeLayout.Arrange(area,body,249,10);Require(expanded.WhiteBounds.Top-collapsed.WhiteBounds.Top==81&&collapsed.WhiteBounds.Top==273,"below reminder follows actual expanded or collapsed footer height");
            var upper=NoticeLayout.Arrange(area,new Rectangle(500,450,141,228),850,10);Require(!upper.Below&&upper.WhiteBounds.Bottom<450,"normal reminder stays above head");
            var r=new RectangleF(36,36,316,76);var up=NoticeLayout.Tail(r,true,90);var down=NoticeLayout.Tail(r,false,260);Require(up[1].Y<r.Top&&down[1].Y>r.Bottom,"tail flips upward below and downward above");
            Require(up[1].X==90&&down[1].X==260,"tail follows actual pet horizontal anchor");
            Require(NoticeLayout.Tail(r,true,-100)[1].X==64&&NoticeLayout.Tail(r,false,1000)[1].X==324,"tail avoids rounded corners when screen-clamped");
            var left=NoticeLayout.Arrange(area,new Rectangle(0,10,141,228),footer,10);var right=NoticeLayout.Arrange(area,new Rectangle(1779,10,141,228),footer,10);Require(left.WhiteBounds.Left==0&&right.WhiteBounds.Right==area.Right,"both screen edges stay inside monitor");
            var other=new Rectangle(-1920,-1080,1920,1040);var secondary=NoticeLayout.Arrange(other,new Rectangle(-1900,-1070,141,228),-740,10);Require(secondary.Below&&secondary.WhiteBounds.Top>=-716&&secondary.WhiteBounds.Left>=-1920,"secondary monitor negative origin");
            var shifted=NoticeLayout.Arrange(area,body,footer,-400);Require(shifted.WhiteBounds.Top>=footer+24,"manual offset cannot overlap controls below");
            var manualBelow=NoticeLayout.Arrange(area,new Rectangle(500,300,141,228),600,300);Require(manualBelow.Below&&manualBelow.WhiteBounds.Top>=624,"large downward offset also uses an upward tail and avoids footer");
        }
    }
}
