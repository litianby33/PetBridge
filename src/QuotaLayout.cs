using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace PetBridge {
    sealed class QuotaTextStyle {
        public float X,Y,Size;public string Family="Microsoft YaHei UI";public bool Bold;
        public QuotaTextStyle(){}
        public QuotaTextStyle(float x,float y,float size,bool bold){X=x;Y=y;Size=size;Bold=bold;}
        public Font MakeFont(){try{return new Font(Family,Math.Max(6,Math.Min(32,Size)),Bold?FontStyle.Bold:FontStyle.Regular);}catch{return new Font("Microsoft YaHei UI",9);}}
    }
    sealed class QuotaLayout {
        // Directions use separate settings objects; never share Items arrays.
        public bool Vertical=false;
        public int Width=325,Height=55,OffsetX=0,OffsetY=0;
        public QuotaTextStyle[] Items={new QuotaTextStyle(35,10,9,true),new QuotaTextStyle(92,6,12.5f,true),new QuotaTextStyle(52,32,8,false),new QuotaTextStyle(200,10,9,true),new QuotaTextStyle(244,6,12.5f,true),new QuotaTextStyle(176,32,8,false),new QuotaTextStyle(111,54,8,false)};
        public static readonly string[] Names={"5 小时标签","5 小时百分比","5 小时重置时间","本周标签","本周百分比","本周重置时间","更新时间"};
        public static QuotaLayout CreateVertical(){return new QuotaLayout{Vertical=true,Width=184,Height=144,Items=new[]{new QuotaTextStyle(22,15,9,false),new QuotaTextStyle(102,10,13,true),new QuotaTextStyle(22,42,8,false),new QuotaTextStyle(22,87,9,false),new QuotaTextStyle(102,82,13,true),new QuotaTextStyle(12,114,8,false),new QuotaTextStyle(22,144,8,false)}};}
        public void Normalize(){
            Width=Math.Max(Vertical?148:280,Math.Min(Vertical?360:620,Width));Height=Math.Max(Vertical?120:54,Math.Min(Vertical?300:180,Height));
            OffsetX=Math.Max(-180,Math.Min(180,OffsetX));OffsetY=Math.Max(-80,Math.Min(120,OffsetY));
            var defaults=Vertical?CreateVertical():new QuotaLayout();if(Items==null||Items.Length!=7)Items=defaults.Items;
            for(int i=0;i<7;i++){if(Items[i]==null)Items[i]=defaults.Items[i];if(float.IsNaN(Items[i].Size)||float.IsInfinity(Items[i].Size))Items[i].Size=defaults.Items[i].Size;Items[i].Size=Math.Max(6,Math.Min(32,Items[i].Size));if(float.IsNaN(Items[i].X)||float.IsInfinity(Items[i].X))Items[i].X=defaults.Items[i].X;if(float.IsNaN(Items[i].Y)||float.IsInfinity(Items[i].Y))Items[i].Y=defaults.Items[i].Y;}
        }
        public string[] Texts(UsageService service){var snap=service.Snapshot;return new[]{"5 小时",snap==null||snap.Primary==null?"—":snap.Primary.Remaining.ToString("0")+"%",snap==null||snap.Primary==null?"正在读取…":snap.Primary.ResetText,"本周",snap==null||snap.Secondary==null?"—":snap.Secondary.Remaining.ToString("0")+"%",snap==null||snap.Secondary==null?"正在读取…":snap.Secondary.ResetText,service.Refreshing?"正在刷新…":service.LastError?"刷新失败 · 保留上次数据":"更新于 "+service.UpdatedAt.ToLocalTime().ToString("HH:mm:ss")};}
        public int DisplayHeight(bool updated){return Height+(updated?24:0);}
        public RectangleF Bounds(Graphics g,string text,int i){var s=Items[i];using(var f=s.MakeFont()){var size=g.MeasureString(text,f,1000,StringFormat.GenericTypographic);return new RectangleF(s.X,s.Y,Math.Max(12,size.Width+4),Math.Max(16,size.Height+4));}}
        // Content coordinates start at the white capsule's top-left. Its alpha
        // window supplies the shadow margin; offsets affect placement only.
        public void PaintBubble(Graphics g,PetSettings settings,bool editor){
            int h=DisplayHeight(settings.ShowUpdatedAt||editor);AlphaForm.Bubble(g,new RectangleF(0,0,Width,h),Vertical?28:h/2f,settings);
            using(var pen=new Pen(Design.Line)){if(Vertical)g.DrawLine(pen,18,Height/2f,Width-18,Height/2f);else g.DrawLine(pen,Width/2f,14,Width/2f,Height-14);}
        }
        public void PaintBubble(Graphics g,string[] texts,PetSettings settings,bool editor){PaintBubble(g,settings,editor);Draw(g,texts,settings,editor);}
        public void Draw(Graphics g,string[] texts,PetSettings settings,bool editor){
            var state=g.Save();g.SetClip(new RectangleF(8,4,Width-16,DisplayHeight(settings.ShowUpdatedAt||editor)-8));
            for(int i=0;i<7;i++){if(i==6&&!settings.ShowUpdatedAt&&!editor)continue;var s=Items[i];using(var font=s.MakeFont())using(var b=new SolidBrush(i==1||i==4?Design.Ink:Design.Muted))g.DrawString(texts[i],font,b,new PointF(s.X,s.Y),StringFormat.GenericTypographic);}g.Restore(state);
        }
    }
    sealed class LayoutCanvas : Control {
        readonly PetForm pet;public int Selected;public bool Vertical;bool drag;Point start;float x,y;public event Action SelectionChanged,LayoutChanged;
        public LayoutCanvas(PetForm p){pet=p;DoubleBuffered=true;BackColor=Color.FromArgb(241,242,244);Cursor=Cursors.SizeAll;AccessibleName="额度排版拖动预览";}
        public QuotaLayout ActiveLayout {get{return Vertical?pet.Settings.VerticalQuotaLayout:pet.Settings.QuotaLayout;}}
        // Picking and dragging use the inverse preview transform as well.
        public float PreviewScale {get{return Math.Min(1f,Math.Min(Math.Max(1,Width-20)/(ActiveLayout.Width+72f),Math.Max(1,Height-20)/(ActiveLayout.DisplayHeight(true)+72f)));}}
        RectangleF Pill {get{float scale=PreviewScale;return new RectangleF((Width-ActiveLayout.Width*scale)/2f,(Height-ActiveLayout.DisplayHeight(true)*scale)/2f,ActiveLayout.Width*scale,ActiveLayout.DisplayHeight(true)*scale);}}
        protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;var r=Pill;var state=g.Save();g.TranslateTransform(r.X,r.Y);g.ScaleTransform(PreviewScale,PreviewScale);var layout=ActiveLayout;var texts=layout.Texts(pet.Usage);layout.PaintBubble(g,texts,pet.Settings,true);var b=layout.Bounds(g,texts[Selected],Selected);using(var pen=new Pen(Color.FromArgb(224,148,99),1/PreviewScale)){pen.DashStyle=DashStyle.Dash;g.DrawRectangle(pen,b.X-2,b.Y-2,b.Width+4,b.Height+4);}g.Restore(state);}
        protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;var r=Pill;float scale=PreviewScale;var point=new PointF((e.X-r.X)/scale,(e.Y-r.Y)/scale);var layout=ActiveLayout;using(var g=CreateGraphics()){var texts=layout.Texts(pet.Usage);for(int i=6;i>=0;i--)if(layout.Bounds(g,texts[i],i).Contains(point)){Selected=i;if(SelectionChanged!=null)SelectionChanged();drag=true;Capture=true;start=e.Location;x=layout.Items[i].X;y=layout.Items[i].Y;break;}}}
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!drag)return;var layout=ActiveLayout;var s=layout.Items[Selected];s.X=Math.Max(-100,Math.Min(layout.Width-10,x+(e.X-start.X)/PreviewScale));s.Y=Math.Max(-50,Math.Min(layout.DisplayHeight(true)-10,y+(e.Y-start.Y)/PreviewScale));if(LayoutChanged!=null)LayoutChanged();Invalidate();}
        protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);drag=false;Capture=false;}
    }
    sealed class LayoutEditor : RoundedForm {
        readonly PetForm pet;readonly LayoutCanvas canvas;readonly ComboBox direction=new ComboBox(),item=new ComboBox();readonly NumericUpDown x=new NumericUpDown(),y=new NumericUpDown(),size=new NumericUpDown(),width=new NumericUpDown(),height=new NumericUpDown(),offsetX=new NumericUpDown(),offsetY=new NumericUpDown();readonly Label face=new Label(),directionHint=new Label();readonly CheckBox bold=new CheckBox();bool syncing;
        public LayoutEditor(PetForm p){pet=p;VisualSettings=p.Settings;Text="搭档 · 额度布局编辑器";AccessibleName="额度布局编辑器";Size=new Size(740,610);var a=Screen.FromControl(pet).WorkingArea;Location=new Point(Math.Max(a.Left+10,Math.Min(pet.Left,a.Right-Width-10)),Math.Max(a.Top+10,Math.Min(pet.Top-90,a.Bottom-Height-10)));
            var title=Label("额度排版 · 横排与竖排分别保存",24,24,680,26);title.Font=new Font(Font,FontStyle.Bold);EnableDrag(title);
            direction.DropDownStyle=ComboBoxStyle.DropDownList;direction.Items.AddRange(new object[]{"编辑横向胶囊","编辑竖向胶囊"});direction.AccessibleName="选择横排或竖排编辑";direction.SetBounds(24,62,208,30);Controls.Add(direction);directionHint.SetBounds(250,63,466,35);directionHint.ForeColor=Design.Muted;directionHint.Text="仅切换编辑对象；实际显示方向在额度设置中选择";Controls.Add(directionHint);
            canvas=new LayoutCanvas(p);canvas.SetBounds(24,103,692,215);Controls.Add(canvas);canvas.SelectionChanged+=delegate{item.SelectedIndex=canvas.Selected;Sync();};canvas.LayoutChanged+=delegate{Sync();pet.ApplySettings();};
            item.DropDownStyle=ComboBoxStyle.DropDownList;item.Items.AddRange(QuotaLayout.Names);item.AccessibleName="选择额度文字";item.SetBounds(24,332,208,30);item.SelectedIndexChanged+=delegate{if(item.SelectedIndex<0)return;canvas.Selected=item.SelectedIndex;Sync();canvas.Invalidate();};Controls.Add(item);
            Number(size,"字号",258,332,6,32);size.DecimalPlaces=1;size.Increment=.5m;bold.Text="加粗";bold.AccessibleName="选中文字加粗";bold.SetBounds(492,334,110,28);bold.CheckedChanged+=delegate{if(syncing)return;Style.Bold=bold.Checked;Apply();};Controls.Add(bold);
            Number(x,"文字 X",24,376,-100,700);Number(y,"文字 Y",258,376,-50,350);
            var font=new Button{Text="选择字体…",AccessibleName="选择额度文字字体",FlatStyle=FlatStyle.Flat};font.SetBounds(492,376,154,32);font.Click+=delegate{using(var dialog=new FontDialog())using(var current=Style.MakeFont()){dialog.Font=current;dialog.MinSize=6;dialog.MaxSize=32;dialog.ShowEffects=false;if(dialog.ShowDialog(this)==DialogResult.OK){Style.Family=dialog.Font.FontFamily.Name;Style.Size=dialog.Font.SizeInPoints;Style.Bold=dialog.Font.Bold;Apply();Sync();}}};Controls.Add(font);face.SetBounds(492,415,214,32);face.AutoEllipsis=true;face.ForeColor=Design.Muted;Controls.Add(face);
            Number(width,"胶囊宽度",24,428,148,620);Number(height,"基础高度",258,428,54,300);Number(offsetX,"整体左右",24,480,-180,180);Number(offsetY,"整体上下",258,480,-80,120);Label("负数向左 / 向上\n仅影响当前方向",492,478,214,46).ForeColor=Design.Muted;
            x.ValueChanged+=delegate{if(!syncing){Style.X=(float)x.Value;Apply();}};y.ValueChanged+=delegate{if(!syncing){Style.Y=(float)y.Value;Apply();}};size.ValueChanged+=delegate{if(!syncing){Style.Size=(float)size.Value;Apply();}};
            width.ValueChanged+=delegate{if(!syncing){ActiveLayout.Width=(int)width.Value;Apply();}};height.ValueChanged+=delegate{if(!syncing){ActiveLayout.Height=(int)height.Value;Apply();}};offsetX.ValueChanged+=delegate{if(!syncing){ActiveLayout.OffsetX=(int)offsetX.Value;Apply();}};offsetY.ValueChanged+=delegate{if(!syncing){ActiveLayout.OffsetY=(int)offsetY.Value;Apply();}};
            direction.SelectedIndexChanged+=delegate{canvas.Vertical=direction.SelectedIndex==1;Sync();};
            var reset=new Button{Text="重置当前方向",AccessibleName="恢复当前方向默认排版",FlatStyle=FlatStyle.Flat};reset.SetBounds(24,550,150,34);reset.Click+=delegate{if(canvas.Vertical)pet.Settings.VerticalQuotaLayout=QuotaLayout.CreateVertical();else pet.Settings.QuotaLayout=new QuotaLayout();Apply();Sync();};var close=new Button{Text="完成",AccessibleName="完成额度排版编辑",FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(250,227,211)};close.SetBounds(604,550,112,34);close.Click+=delegate{Close();};Controls.AddRange(new Control[]{reset,close});Label("立即保存 · 拖动文字微调\n更新时间按显示开关生效",194,550,400,42).ForeColor=Design.Muted;item.SelectedIndex=1;direction.SelectedIndex=0;
        }
        QuotaLayout ActiveLayout {get{return canvas.ActiveLayout;}}
        QuotaTextStyle Style {get{return ActiveLayout.Items[Math.Max(0,item.SelectedIndex)];}}
        Label Label(string text,int x,int y,int w,int h){var l=new Label{Text=text,ForeColor=Design.Ink};l.SetBounds(x,y,w,h);Controls.Add(l);return l;}
        void Number(NumericUpDown c,string name,int left,int top,int min,int max){Label(name,left,top+5,82,26);c.AccessibleName=name;c.Minimum=min;c.Maximum=max;c.SetBounds(left+84,top,94,30);Controls.Add(c);}
        static decimal Within(NumericUpDown control,float value){return Math.Max(control.Minimum,Math.Min(control.Maximum,(decimal)value));}
        void Sync(){syncing=true;var layout=ActiveLayout;layout.Normalize();var s=Style;x.Value=Within(x,s.X);y.Value=Within(y,s.Y);size.Value=Within(size,s.Size);bold.Checked=s.Bold;face.Text=s.Family;width.Minimum=canvas.Vertical?148:280;width.Maximum=canvas.Vertical?360:620;height.Minimum=canvas.Vertical?120:54;height.Maximum=canvas.Vertical?300:180;width.Value=layout.Width;height.Value=layout.Height;offsetX.Value=layout.OffsetX;offsetY.Value=layout.OffsetY;syncing=false;canvas.Invalidate();}
        void Apply(){pet.ApplySettings();canvas.Invalidate();}
        void EnableDrag(Control header){bool dragging=false;Point start=Point.Empty,origin=Point.Empty;header.Cursor=Cursors.SizeAll;header.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button!=MouseButtons.Left)return;dragging=true;start=header.PointToScreen(e.Location);origin=Location;header.Capture=true;};header.MouseMove+=delegate(object sender,MouseEventArgs e){if(!dragging)return;var at=header.PointToScreen(e.Location);Location=new Point(origin.X+at.X-start.X,origin.Y+at.Y-start.Y);};header.MouseUp+=delegate{dragging=false;header.Capture=false;};}
    }
}

