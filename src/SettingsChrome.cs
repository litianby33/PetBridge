using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PetBridge {
    // Shared UI chrome: keep this file identical between the public and private build,
    // except for the namespace. It contains no character assets or account bindings.
    static class SettingsTheme {
        public static readonly Color Ink=Color.FromArgb(36,41,49),Muted=Color.FromArgb(112,120,133),Line=Color.FromArgb(230,233,238),Accent=Color.FromArgb(75,102,205),Tint=Color.FromArgb(237,241,253),Hover=Color.FromArgb(244,246,249);
        public static void Fill(Graphics g,RectangleF r,float radius,Color color){using(var p=Design.Round(r,radius))using(var b=new SolidBrush(color))g.FillPath(b,p);}
        public static void Glyph(Graphics g,string kind,Rectangle r,Color color){
            float x=r.X,y=r.Y,w=r.Width,h=r.Height,cx=x+w/2,cy=y+h/2;g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var p=new Pen(color,1.55f)){p.StartCap=p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
                if(kind=="chat"){g.DrawRoundedRectangle(p,new RectangleF(cx-7,cy-6,14,11),3);g.DrawLines(p,new[]{new PointF(cx-3,cy+5),new PointF(cx-5,cy+8),new PointF(cx+1,cy+5)});}
                else if(kind=="settings"){g.DrawEllipse(p,cx-7,cy-7,14,14);g.DrawEllipse(p,cx-2,cy-2,4,4);for(int i=0;i<8;i++){double a=i*Math.PI/4;g.DrawLine(p,cx+(float)Math.Cos(a)*7,cy+(float)Math.Sin(a)*7,cx+(float)Math.Cos(a)*9,cy+(float)Math.Sin(a)*9);}}
                else if(kind=="eye"){g.DrawBezier(p,cx-8,cy,cx-3,cy-7,cx+3,cy-7,cx+8,cy);g.DrawBezier(p,cx-8,cy,cx-3,cy+7,cx+3,cy+7,cx+8,cy);g.DrawEllipse(p,cx-2,cy-2,4,4);}
                else if(kind=="edge"){g.DrawRoundedRectangle(p,new RectangleF(cx-8,cy-6,16,12),2);g.DrawLine(p,cx+3,cy-6,cx+3,cy+6);}
                else if(kind=="exit"){g.DrawArc(p,cx-7,cy-7,14,14,-45,270);g.DrawLine(p,cx,cy,cx+8,cy);g.DrawLines(p,new[]{new PointF(cx+5,cy-3),new PointF(cx+8,cy),new PointF(cx+5,cy+3)});}
                else if(kind=="spark"){g.DrawPolygon(p,new[]{new PointF(cx,cy-8),new PointF(cx+2,cy-2),new PointF(cx+8,cy),new PointF(cx+2,cy+2),new PointF(cx,cy+8),new PointF(cx-2,cy+2),new PointF(cx-8,cy),new PointF(cx-2,cy-2)});}
                else if(kind=="color"){g.DrawEllipse(p,cx-7,cy-7,14,14);g.DrawLine(p,cx,cy-7,cx,cy+7);g.DrawLine(p,cx-7,cy,cx+7,cy);}
                else if(kind=="layout"){g.DrawRoundedRectangle(p,new RectangleF(cx-7,cy-7,14,14),2);g.DrawLine(p,cx-7,cy-2,cx+7,cy-2);g.DrawLine(p,cx-2,cy-2,cx-2,cy+7);}
                else if(kind=="dots"){for(int i=-1;i<=1;i++)using(var b=new SolidBrush(color))g.FillEllipse(b,cx-1.5f,cy+i*6-1.5f,3,3);}
                else if(kind=="bell"){g.DrawArc(p,cx-5,cy-7,10,11,180,180);g.DrawLines(p,new[]{new PointF(cx-5,cy-2),new PointF(cx-6,cy+4),new PointF(cx+6,cy+4),new PointF(cx+5,cy-2)});g.DrawArc(p,cx-2,cy+4,4,4,0,180);}
                else Design.Icon(g,kind,r,color);
            }
        }
        public static Bitmap Image(string kind){var image=new Bitmap(24,24);using(var g=Graphics.FromImage(image))Glyph(g,kind,new Rectangle(2,2,20,20),Ink);return image;}
        static void DrawRoundedRectangle(this Graphics g,Pen p,RectangleF r,float radius){using(var path=Design.Round(r,radius))g.DrawPath(p,path);}
    }
    sealed class SettingsButton : Button {
        bool hover;public bool Accent;public string Glyph;
        public SettingsButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;ForeColor=SettingsTheme.Ink;Cursor=Cursors.Hand;Font=new Font("Microsoft YaHei UI",9.5f);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?Color.White:Parent.BackColor);var rect=new Rectangle(1,1,Width-3,Height-3);Color fill=Accent?(hover?Color.FromArgb(62,88,187):SettingsTheme.Accent):hover?SettingsTheme.Hover:BackColor;if(!Accent&&ForeColor==Color.White)fill=BackColor;SettingsTheme.Fill(g,rect,8,fill);if(!Accent)using(var p=new Pen(SettingsTheme.Line))using(var path=Design.Round(rect,8))g.DrawPath(p,path);if(Glyph!=null)SettingsTheme.Glyph(g,Glyph,ClientRectangle,Accent?Color.White:ForeColor);else TextRenderer.DrawText(g,Text,Font,rect,Accent?Color.White:ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(4,4,Width-9,Height-9));}
    }
    sealed class SettingsSwitch : CheckBox {
        bool hover;
        public SettingsSwitch(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);AutoSize=false;Size=new Size(410,34);Cursor=Cursors.Hand;Font=new Font("Microsoft YaHei UI",9.5f);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}protected override void OnCheckedChanged(EventArgs e){base.OnCheckedChanged(e);Invalidate();}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;if(hover)SettingsTheme.Fill(g,ClientRectangle,7,SettingsTheme.Hover);var text=new Rectangle(0,0,Width-55,Height);TextRenderer.DrawText(g,Text,Font,text,SettingsTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);var track=new Rectangle(Width-44,(Height-20)/2,38,20);SettingsTheme.Fill(g,track,10,Checked?SettingsTheme.Accent:Color.FromArgb(206,211,219));using(var b=new SolidBrush(Color.White))g.FillEllipse(b,track.Left+(Checked?20:3),track.Top+3,14,14);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(2,2,Width-5,Height-5));}
    }
    sealed class SettingsSlider : Control {
        int minimum,maximum=100,value;public event EventHandler ValueChanged;
        public int Minimum{get{return minimum;}set{minimum=value;}}public int Maximum{get{return maximum;}set{maximum=value;}}
        public int Value{get{return value;}set{int next=Math.Max(minimum,Math.Min(maximum,value));if(this.value==next)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
        public int SmallChange=1,LargeChange=1,TickFrequency=1;public TickStyle TickStyle;bool down;
        public SettingsSlider(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);AccessibleRole=AccessibleRole.Slider;TabStop=true;Cursor=Cursors.Hand;Size=new Size(399,30);}
        void At(int x){if(Width>24)Value=minimum+(int)Math.Round((maximum-minimum)*Math.Max(0,Math.Min(1,(x-10)/(double)(Width-20))));}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();down=true;Capture=true;At(e.X);}protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(down)At(e.X);}protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);down=false;Capture=false;}
        protected override bool IsInputKey(Keys key){return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Home||key==Keys.End||base.IsInputKey(key);}
        protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Value-=SmallChange;else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Value+=SmallChange;else if(e.KeyCode==Keys.Home)Value=minimum;else if(e.KeyCode==Keys.End)Value=maximum;else return;e.Handled=true;}
        protected override void OnMouseWheel(MouseEventArgs e){base.OnMouseWheel(e);Value+=(e.Delta>0?1:-1)*SmallChange;}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;float x=10+(Width-20)*(value-minimum)/(float)Math.Max(1,maximum-minimum),cy=Height/2f;SettingsTheme.Fill(g,new RectangleF(10,cy-2,Width-20,4),2,SettingsTheme.Line);if(x>10)SettingsTheme.Fill(g,new RectangleF(10,cy-2,x-10,4),2,SettingsTheme.Accent);if(Focused)using(var b=new SolidBrush(Color.FromArgb(28,SettingsTheme.Accent)))g.FillEllipse(b,x-10,cy-10,20,20);using(var b=new SolidBrush(Color.White))g.FillEllipse(b,x-6,cy-6,12,12);using(var p=new Pen(SettingsTheme.Accent,2))g.DrawEllipse(p,x-6,cy-6,12,12);}
    }
    sealed class SettingsNavButton : Button {
        public bool Selected;public string Glyph;bool hover;
        public SettingsNavButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;Font=new Font("Microsoft YaHei UI",9.5f);}
        protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
        protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;if(Selected||hover)SettingsTheme.Fill(g,new Rectangle(0,0,Width-1,Height-1),8,Selected?SettingsTheme.Tint:SettingsTheme.Hover);var c=Selected?SettingsTheme.Accent:SettingsTheme.Muted;SettingsTheme.Glyph(g,Glyph,new Rectangle(13,9,20,20),c);TextRenderer.DrawText(g,Text,Font,new Rectangle(43,0,Width-47,Height),Selected?SettingsTheme.Accent:SettingsTheme.Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(3,3,Width-7,Height-7));}
    }
    class SettingsShell : RoundedForm {
        readonly Panel navigation=new Panel(),content=new Panel();readonly Label heading=new Label(),description=new Label();
        readonly List<Panel> pages=new List<Panel>();readonly List<SettingsNavButton> buttons=new List<SettingsNavButton>();
        [DllImport("user32.dll")] static extern bool ReleaseCapture();[DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
        protected override bool ShowWithoutActivation{get{return false;}}
        public SettingsShell(PetForm pet,string product){
            VisualSettings=pet.Settings;Text=product+" · 设置";Font=new Font("Microsoft YaHei UI",9.5f);BackColor=Color.White;KeyPreview=true;
            var area=Screen.FromControl(pet).WorkingArea;Size=new Size(760,Math.Min(700,Math.Max(530,area.Height-56)));
            Point saved=new Point(pet.Settings.SettingsX,pet.Settings.SettingsY);bool visible=false;foreach(var screen in Screen.AllScreens)if(screen.WorkingArea.Contains(new Point(saved.X+Width/2,saved.Y+40))){area=screen.WorkingArea;visible=true;break;}
            int x=visible?saved.X:pet.Left-Width-24,y=visible?saved.Y:pet.Top-80;Location=new Point(Math.Max(area.Left+12,Math.Min(x,area.Right-Width-12)),Math.Max(area.Top+12,Math.Min(y,area.Bottom-Height-12)));
            var brand=new Label{Text=product,ForeColor=SettingsTheme.Ink,Font=new Font(Font.FontFamily,13,FontStyle.Bold)};brand.SetBounds(30,31,174,30);Controls.Add(brand);Drag(brand);
            var caption=new Label{Text="桌面搭档 · 偏好设置",ForeColor=SettingsTheme.Muted};caption.SetBounds(30,65,174,23);Controls.Add(caption);Drag(caption);
            heading.Font=new Font(Font.FontFamily,19,FontStyle.Bold);heading.ForeColor=SettingsTheme.Ink;heading.SetBounds(230,27,440,37);Controls.Add(heading);Drag(heading);
            description.ForeColor=SettingsTheme.Muted;description.SetBounds(231,72,440,26);Controls.Add(description);Drag(description);
            navigation.SetBounds(24,110,180,Height-180);navigation.BackColor=Color.White;navigation.AutoScroll=true;Controls.Add(navigation);
            content.SetBounds(216,110,520,Height-182);content.BackColor=Color.White;Controls.Add(content);
            var close=new SettingsButton{Glyph="close",AccessibleName="关闭设置"};close.SetBounds(701,27,34,34);close.Click+=delegate{Close();};Controls.Add(close);
            var done=new SettingsButton{Text="完成",Accent=true};done.SetBounds(642,Height-58,94,36);done.Click+=delegate{Close();};Controls.Add(done);
            var savedHint=new Label{Text="已自动保存  ·  拖动顶部可移动窗口",ForeColor=SettingsTheme.Muted};savedHint.SetBounds(232,Height-49,385,26);Controls.Add(savedHint);
            MouseDown+=delegate(object s,MouseEventArgs e){if(e.Y<104)MoveWindow(e);};KeyDown+=delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Escape){Close();e.Handled=true;}};
            FormClosed+=delegate{if(WindowState==FormWindowState.Normal){pet.Settings.SettingsX=Left;pet.Settings.SettingsY=Top;pet.Settings.Save();}};
        }
        void Drag(Control c){c.MouseDown+=delegate(object s,MouseEventArgs e){MoveWindow(e);};}
        void MoveWindow(MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}
        protected Panel Page(string name){
            string glyph="settings",note="调整后自动保存，立即生效";
            switch(name){case "常用":glyph="settings";note="常用开关、人物大小与屏幕位置";break;case "外观":glyph="spark";note="投影、工具条和胶囊之间的距离";break;case "提醒":glyph="bell";note="任务结束与额度不足时如何提醒";break;case "动作":glyph="spark";note="空闲动作、自动收起与启动行为";break;case "排版":glyph="layout";note="单独调整额度文字、字体与位置";break;case "状态":glyph="dots";note="任务进行中三点胶囊的大小与位置";break;case "气泡":glyph="chat";note="提醒消息的布局与弹出效果";break;case "贴边":glyph="edge";note="边缘收起判定、任务状态与全屏避让";break;case "颜色":glyph="color";note="不同状态的圆点颜色与柔光";break;case "联动":glyph="link";note="桌面聊天、云端 Work 与提问状态";break;case "绑定":glyph="link";note="绑定本机 Codex，选择自己的角色";break;case "测试":glyph="spark";note="预览效果，不发送聊天或调用绘图";break;}
            var page=new Panel{BackColor=Color.White,AutoScroll=true,AccessibleName=name+"设置",Dock=DockStyle.Fill,Visible=false};page.Paint+=delegate(object s,PaintEventArgs e){using(var p=new Pen(SettingsTheme.Line))e.Graphics.DrawLine(p,18,0,page.ClientSize.Width-18,0);};content.Controls.Add(page);pages.Add(page);
            var button=new SettingsNavButton{Text=name=="排版"?"额度排版":name=="状态"?"任务状态":name=="测试"?"测试工具":name=="绑定"?"绑定角色":name=="常用"?"常用设置":name+"设置",Glyph=glyph,AccessibleName=name+"设置"};button.SetBounds(0,buttons.Count*39,166,37);navigation.Controls.Add(button);buttons.Add(button);
            Action select=delegate{for(int i=0;i<pages.Count;i++){pages[i].Visible=pages[i]==page;buttons[i].Selected=pages[i]==page;buttons[i].Invalidate();}page.BringToFront();heading.Text=button.Text;description.Text=note;};button.Click+=delegate{select();};if(buttons.Count==1)select();return page;
        }
    }
    sealed class ModernMenuRenderer : ToolStripProfessionalRenderer {
        public ModernMenuRenderer():base(new ProfessionalColorTable()){RoundedEdges=false;}
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Color.FromArgb(252,252,253));}
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e){using(var p=new Pen(SettingsTheme.Line))using(var path=Design.Round(new RectangleF(0,0,e.ToolStrip.Width-1,e.ToolStrip.Height-1),10))e.Graphics.DrawPath(p,path);}
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(e.Item.Selected&&e.Item.Enabled)SettingsTheme.Fill(e.Graphics,new Rectangle(5,1,e.Item.Width-10,e.Item.Height-2),6,SettingsTheme.Hover);}
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e){using(var p=new Pen(SettingsTheme.Line))e.Graphics.DrawLine(p,12,4,e.Item.Width-12,4);}
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e){var item=e.Item as ToolStripMenuItem;if(item==null||!item.Checked)base.OnRenderItemImage(e);}
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e){var r=e.ImageRectangle;using(var p=new Pen(SettingsTheme.Accent,2)){p.StartCap=p.EndCap=LineCap.Round;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.DrawLines(p,new[]{new Point(r.X+4,r.Y+10),new Point(r.X+8,r.Y+14),new Point(r.X+16,r.Y+5)});}}
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e){var r=e.ArrowRectangle;using(var p=new Pen(SettingsTheme.Muted,1.5f)){e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;int x=r.X+r.Width/2,y=r.Y+r.Height/2;e.Graphics.DrawLines(p,new[]{new Point(x-2,y-4),new Point(x+2,y),new Point(x-2,y+4)});}}
    }
    sealed partial class PetForm {
        public void PreviewJump(){Play(4,1);}
        public void PreviewWave(){Play(3,2);}
        public int SettingsScalePercent{get{return (int)Math.Round(scale*100);}}
        public void SetSettingsScale(int percent){SetScale(percent/100.0);}
        public void MoveToPrimary(){Undock(false);var work=Screen.PrimaryScreen.WorkingArea;Location=new Point(work.Right-Width-50,work.Bottom-Height-25);Save();}
        ToolStripMenuItem MenuAction(ToolStripItemCollection items,string text,string icon,Action action){var item=new ToolStripMenuItem(text,SettingsTheme.Image(icon),delegate{if(action!=null)action();}){AutoSize=false,Size=new Size(238,36),Padding=new Padding(7,0,8,0)};items.Add(item);return item;}
        void BuildModernMenu(){
            menu.Renderer=new ModernMenuRenderer();menu.Font=new Font("Microsoft YaHei UI",9.5f);menu.BackColor=Color.FromArgb(252,252,253);menu.ForeColor=SettingsTheme.Ink;menu.Padding=new Padding(5,7,5,7);menu.ShowImageMargin=true;menu.ImageScalingSize=new Size(20,20);menu.DropShadowEnabled=true;
            MenuAction(menu.Items,"开始聊天","pencil",delegate{ToggleChat();});var usage=MenuAction(menu.Items,"显示剩余额度","usage",delegate{ToggleUsage();});MenuAction(menu.Items,"回到 ChatGPT 对话","link",delegate{OpenCurrent();});menu.Items.Add(new ToolStripSeparator{AutoSize=false,Height=10});
            var edgeItem=MenuAction(menu.Items,"收起到屏幕边缘","edge",null);edgeItem.DropDown.Renderer=menu.Renderer;edgeItem.DropDown.Padding=new Padding(5,7,5,7);MenuAction(edgeItem.DropDownItems,"收进左侧","edge",delegate{DockTo("left");});MenuAction(edgeItem.DropDownItems,"收进右侧","edge",delegate{DockTo("right");});
            var hide=MenuAction(menu.Items,"临时隐藏","eye",delegate{if(userHidden)RestorePet();else TemporarilyHide();});MenuAction(menu.Items,"详细设置…","settings",delegate{OpenSettings();});menu.Items.Add(new ToolStripSeparator{AutoSize=false,Height=10});MenuAction(menu.Items,"退出桌面搭档","exit",delegate{Close();});
            menu.Opening+=delegate{usage.Checked=settings.ShowUsage;hide.Text=userHidden?"显示人物":"临时隐藏";};menu.SizeChanged+=delegate{if(menu.Width>20&&menu.Height>20){var old=menu.Region;using(var path=Design.Round(new RectangleF(0,0,menu.Width,menu.Height),10))menu.Region=new Region(path);if(old!=null)old.Dispose();}};ContextMenuStrip=menu;
            tray.Icon=CreateTrayIcon();tray.Text="桌面搭档 · 点击恢复显示";tray.ContextMenuStrip=menu;tray.Visible=true;tray.DoubleClick+=delegate{RestorePet();};tray.MouseClick+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)RestorePet();};
        }
    }
}
