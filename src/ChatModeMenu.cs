using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PetBridge {
    // Compact source picker, like the engine icon on a search bar. No artwork files.
    static class ChatModeGlyph {
        public static void Draw(Graphics g,string target,RectangleF rect,Color color){
            var state=g.Save();g.TranslateTransform(rect.X,rect.Y);g.ScaleTransform(rect.Width/20f,rect.Height/20f);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var pen=new Pen(color,1.5f)){
                pen.StartCap=pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
                if(ChatRouting.IsOrdinary(target)){
                    using(var path=Design.Round(new RectangleF(2,3,16,12),4))g.DrawPath(pen,path);
                    g.DrawLines(pen,new[]{new PointF(6,15),new PointF(5,18),new PointF(10,15)});
                    using(var b=new SolidBrush(color))for(int i=0;i<3;i++)g.FillEllipse(b,5+i*4,8,2,2);
                }else{
                    using(var path=Design.Round(new RectangleF(1.5f,3,17,14),3))g.DrawPath(pen,path);
                    g.DrawLines(pen,new[]{new PointF(5,7),new PointF(8,10),new PointF(5,13)});
                    g.DrawLine(pen,11,13,15,13);
                }
            }
            g.Restore(state);
        }
        public static Bitmap Image(string target){var b=new Bitmap(24,24);using(var g=Graphics.FromImage(b))Draw(g,target,new RectangleF(2,2,20,20),SettingsTheme.Muted);return b;}
    }
    sealed class ChatModeButton : Button {
        public string Target="codex";
        public ChatModeButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;Cursor=Cursors.Hand;TabStop=true;}
        protected override void OnPaint(PaintEventArgs e){
            var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
            bool hover=Enabled&&ClientRectangle.Contains(PointToClient(Cursor.Position));
            if(hover||Focused)SettingsTheme.Fill(g,new RectangleF(0,1,Width-1,Height-2),9,SettingsTheme.Hover);
            var color=Enabled?SettingsTheme.Muted:Color.FromArgb(190,194,201);
            ChatModeGlyph.Draw(g,Target,new RectangleF(3,Height/2f-10,20,20),color);
            using(var pen=new Pen(color,1.4f)){pen.StartCap=pen.EndCap=LineCap.Round;float cx=Width-7,cy=Height/2f;g.DrawLines(pen,new[]{new PointF(cx-2.5f,cy-1),new PointF(cx,cy+1.5f),new PointF(cx+2.5f,cy-1)});}
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,3,Width-5,Height-7));
        }
    }
    sealed class ChatModeMenuRenderer : ToolStripProfessionalRenderer {
        public ChatModeMenuRenderer():base(new ProfessionalColorTable()){RoundedEdges=false;}
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Color.White);}
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using(var pen=new Pen(SettingsTheme.Line))using(var p=Design.Round(new RectangleF(0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1),11))e.Graphics.DrawPath(pen,p);}
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){var item=e.Item as ToolStripMenuItem;if(e.Item.Selected||item!=null&&item.Checked)SettingsTheme.Fill(e.Graphics,new RectangleF(5,1,e.Item.Width-10,e.Item.Height-2),7,e.Item.Selected?SettingsTheme.Hover:Color.FromArgb(242,245,250));}
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){} // Keep each source's icon visible.
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e){if(e.Image!=null)e.Graphics.DrawImage(e.Image,e.ImageRectangle);}
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){var r=e.ArrowRectangle;float x=r.X+r.Width/2f,y=r.Y+r.Height/2f;using(var pen=new Pen(SettingsTheme.Muted,1.4f)){pen.StartCap=pen.EndCap=LineCap.Round;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.DrawLines(pen,new[]{new PointF(x-2,y-4),new PointF(x+2,y),new PointF(x-2,y+4)});}}
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var pen=new Pen(SettingsTheme.Line))e.Graphics.DrawLine(pen,12,4,e.Item.Width-12,4);}
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){var item=e.Item as ToolStripMenuItem;e.TextColor=!e.Item.Enabled||item!=null&&e.Text==item.ShortcutKeyDisplayString?SettingsTheme.Muted:SettingsTheme.Ink;base.OnRenderItemText(e);}
    }
}
