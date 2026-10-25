using System;
using System.Drawing;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PetBridge {
    // Geometry uses the unclamped horizontal card, including its transparent
    // shadow padding. The extra return margin prevents flicker near the edge.
    static class QuotaPlacement {
        public const int ShadowPadding=36,ScreenInset=8,ReturnBuffer=24;
        public static bool UseVertical(Rectangle petBounds,Rectangle workingArea,int horizontalWidth,string mode,int margin,bool wasVertical){
            if(String.Equals(mode,"vertical",StringComparison.OrdinalIgnoreCase))return true;
            if(String.Equals(mode,"horizontal",StringComparison.OrdinalIgnoreCase))return false;
            int gap=Math.Max(0,margin)+(wasVertical?ReturnBuffer:0),width=Math.Max(1,horizontalWidth)+ShadowPadding*2;
            int left=petBounds.Left+petBounds.Width/2-width/2;
            return left<workingArea.Left+gap||left+width>workingArea.Right-gap;
        }
        public static void Normalize(PetSettings settings){
            if(settings.QuotaLayout==null)settings.QuotaLayout=new QuotaLayout();
            if(settings.VerticalQuotaLayout==null)settings.VerticalQuotaLayout=QuotaLayout.CreateVertical();
            settings.QuotaLayout.Vertical=false;settings.VerticalQuotaLayout.Vertical=true;
            settings.QuotaLayout.Normalize();settings.VerticalQuotaLayout.Normalize();
            if(settings.QuotaLayoutMode!="horizontal"&&settings.QuotaLayoutMode!="vertical")settings.QuotaLayoutMode="auto";
            settings.QuotaEdgeMargin=Math.Max(0,Math.Min(120,settings.QuotaEdgeMargin));
        }
        // footerBottom is the toolbar/composer's screen coordinate. Clamp the
        // white card, not its invisible shadow window, so an edge needs only 8px.
        public static Rectangle WhiteBounds(Rectangle petBounds,Rectangle workingArea,QuotaLayout layout,bool updated,int footerBottom,int gap){
            int left=petBounds.Left+petBounds.Width/2-layout.Width/2+layout.OffsetX;
            int rightLimit=workingArea.Right-ScreenInset-layout.Width;
            left=Math.Max(workingArea.Left+ScreenInset,Math.Min(left,rightLimit));
            return new Rectangle(left,footerBottom+gap+layout.OffsetY,layout.Width,layout.DisplayHeight(updated));
        }
        public static int OccupiedHeight(int bodyBottom,int footerBottom,int gap,int quotaHeight,int offsetY,bool showQuota){
            return Math.Max(bodyBottom,footerBottom+(showQuota?gap+offsetY+quotaHeight+ScreenInset:0));
        }
    }
    sealed partial class PetForm {
        bool quotaWasVertical,changingQuotaGeometry;
        public void NormalizeQuotaLayouts(){QuotaPlacement.Normalize(settings);}
        QuotaLayout QuotaLayoutAt(Point location,Rectangle area,bool remember){
            var horizontal=settings.QuotaLayout;
            var candidate=new Rectangle(location.X+horizontal.OffsetX,location.Y,Width,Height);
            bool vertical=QuotaPlacement.UseVertical(candidate,area,horizontal.Width,settings.QuotaLayoutMode,settings.QuotaEdgeMargin,quotaWasVertical);
            if(remember)quotaWasVertical=vertical;
            return vertical?settings.VerticalQuotaLayout:horizontal;
        }
        public QuotaLayout CurrentQuotaLayout {get{return QuotaLayoutAt(Location,Screen.FromControl(this).WorkingArea,true);}}
        public bool QuotaIsVertical {get{return CurrentQuotaLayout.Vertical;}}
        int QuotaOccupiedHeightAt(Point location,Rectangle area,int bodyBottom){
            var layout=QuotaLayoutAt(location,area,false);
            int footer=ComposerVisible?ArtHeight+settings.ToolbarGap+composer.Height:ToolbarBottom;
            return QuotaPlacement.OccupiedHeight(bodyBottom,footer,settings.UsageGap,layout.DisplayHeight(settings.ShowUpdatedAt),layout.OffsetY,settings.ShowUsage);
        }
        void SyncQuotaGeometry(){
            if(changingQuotaGeometry||docked||docking)return;
            changingQuotaGeometry=true;
            try{
                var point=Constrain(Location);
                if(point!=Location){bool previous=changing;changing=true;try{Location=point;}finally{changing=previous;}}
                QuotaLayoutAt(Location,Screen.FromControl(this).WorkingArea,true);
            }finally{changingQuotaGeometry=false;}
        }
    }
    static class QuotaLayoutChecks {
        static void Require(bool value,string name){if(!value)throw new Exception("Quota layout: "+name);}
        public static void Run(){
            var area=new Rectangle(0,0,1920,1040);int width=325;
            Require(!QuotaPlacement.UseVertical(new Rectangle(850,100,192,260),area,width,"auto",12,false),"center stays horizontal");
            Require(QuotaPlacement.UseVertical(new Rectangle(0,100,192,260),area,width,"auto",12,false),"left edge switches");
            Require(QuotaPlacement.UseVertical(new Rectangle(1728,100,192,260),area,width,"auto",12,false),"right edge switches");
            // Complete horizontal width is 397px. Its left at x=115 is 13px.
            var boundary=new Rectangle(115,100,192,260);
            Require(!QuotaPlacement.UseVertical(boundary,area,width,"auto",12,false),"entry threshold outside");
            Require(QuotaPlacement.UseVertical(boundary,area,width,"auto",12,true),"return margin retains vertical");
            Require(!QuotaPlacement.UseVertical(new Rectangle(138,100,192,260),area,width,"auto",12,true),"return boundary clears");
            Require(QuotaPlacement.UseVertical(new Rectangle(113,100,192,260),area,width,"auto",12,false),"entry boundary inside");
            Require(!QuotaPlacement.UseVertical(new Rectangle(0,100,192,260),area,width,"horizontal",12,true),"forced horizontal");
            Require(QuotaPlacement.UseVertical(new Rectangle(850,100,192,260),area,width,"vertical",12,false),"forced vertical");
            var negative=new Rectangle(-1920,-400,1920,1080);
            Require(QuotaPlacement.UseVertical(new Rectangle(-1920,-100,192,260),negative,width,"auto",12,false),"negative screen edge");
            Require(!QuotaPlacement.UseVertical(new Rectangle(-1100,-100,192,260),negative,width,"auto",12,true),"negative screen center");

            var settings=new PetSettings();QuotaPlacement.Normalize(settings);
            var horizontal=settings.QuotaLayout;var vertical=settings.VerticalQuotaLayout;
            Require(!horizontal.Vertical&&vertical.Vertical,"separate orientation flags");
            var left=QuotaPlacement.WhiteBounds(new Rectangle(-40,50,192,260),area,vertical,false,300,9);
            var right=QuotaPlacement.WhiteBounds(new Rectangle(1800,50,192,260),area,vertical,false,300,9);
            Require(left.Left==area.Left+8&&right.Right==area.Right-8,"white frame can reach 8px inset");
            var negativeLeft=QuotaPlacement.WhiteBounds(new Rectangle(-1960,-100,192,260),negative,vertical,false,200,9);
            Require(negativeLeft.Left==-1912,"negative-screen white inset");
            vertical.OffsetX=13;vertical.OffsetY=17;
            var shifted=QuotaPlacement.WhiteBounds(new Rectangle(850,50,192,260),area,vertical,false,300,9);
            Require(shifted.Top==326&&shifted.Left==850+96-vertical.Width/2+13,"per-layout offsets");
            int occupied=QuotaPlacement.OccupiedHeight(202,216,9,vertical.DisplayHeight(false),17,true);
            Require(occupied==216+9+17+vertical.DisplayHeight(false)+8,"vertical target reserves actual height");
            int constrainedY=area.Bottom-occupied;
            var bottom=QuotaPlacement.WhiteBounds(new Rectangle(850,constrainedY,192,260),area,vertical,false,constrainedY+216,9);
            Require(bottom.Bottom==area.Bottom-8,"bottom card fits after target constraint");
            Require(QuotaPlacement.OccupiedHeight(202,216,9,vertical.DisplayHeight(false),17,false)==216,"hidden quota reserves no height");
            Require(QuotaPlacement.OccupiedHeight(202,216,9,vertical.DisplayHeight(true),17,true)==occupied+24,"updated line contributes to height");

            horizontal.Items[1].X=101;horizontal.Items[1].Size=14.5f;horizontal.Width=391;horizontal.OffsetY=-3;
            vertical.Items[1].X=79;vertical.Items[1].Size=16;vertical.Width=204;vertical.OffsetY=21;
            var serializer=new JavaScriptSerializer();var restored=serializer.Deserialize<PetSettings>(serializer.Serialize(settings));QuotaPlacement.Normalize(restored);
            Require(restored.QuotaLayout.Width==391&&restored.QuotaLayout.Items[1].X==101&&restored.QuotaLayout.Items[1].Size==14.5f&&restored.QuotaLayout.OffsetY==-3,"horizontal settings retained");
            Require(restored.VerticalQuotaLayout.Width==204&&restored.VerticalQuotaLayout.Items[1].X==79&&restored.VerticalQuotaLayout.Items[1].Size==16&&restored.VerticalQuotaLayout.OffsetY==21,"vertical settings retained separately");
            restored.VerticalQuotaLayout.Items[1].X=33;
            Require(restored.QuotaLayout.Items[1].X==101,"layouts share no item instances");
            var old=serializer.Deserialize<PetSettings>("{\"QuotaLayout\":{\"Width\":388,\"Height\":66},\"ShowUsage\":true}");QuotaPlacement.Normalize(old);
            Require(old.QuotaLayout.Width==388&&old.VerticalQuotaLayout.Vertical&&old.QuotaLayoutMode=="auto","old config gains vertical defaults");
            old.QuotaLayout=null;old.VerticalQuotaLayout=null;QuotaPlacement.Normalize(old);
            Require(old.QuotaLayout!=null&&old.VerticalQuotaLayout!=null,"null layouts recover");

            var defaults=QuotaLayout.CreateVertical();
            using(var bitmap=new Bitmap(640,280)){
                bitmap.SetResolution(120,120);
                using(var graphics=Graphics.FromImage(bitmap)){
                    var samples=new[]{"5 小时","100%","00:50 重置","本周","100%","10月27日 14:50 重置","更新于 12:30:59"};
                    for(int i=0;i<6;i++){var bounds=defaults.Bounds(graphics,samples[i],i);Require(bounds.Left>=8&&bounds.Top>=4&&bounds.Right<=defaults.Width-8&&bounds.Bottom<=defaults.Height-4,"vertical 100 percent and reset text "+i);}
                }
            }
        }
    }
}

