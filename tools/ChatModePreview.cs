using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace PetBridge {
    sealed partial class PetForm {
        public void PrepareCompactCheck(){clock.Stop();Usage.Dispose();tray.Visible=false;settings.ChatTarget="codex";}
    }
    static class CompactChatChecks {
        [DllImport("user32.dll")]static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
        static void Call(object o,string n,params object[] args){o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
        static void Need(bool value,string label){if(!value)throw new Exception(label);}
        static Bitmap Capture(Control c){var form=c as Form;if(form!=null&&!form.Visible){form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-16000,-16000);form.TopMost=false;form.Show();}c.CreateControl();foreach(Control child in c.Controls)child.CreateControl();c.PerformLayout();var image=new Bitmap(c.Width,c.Height);c.DrawToBitmap(image,new Rectangle(Point.Empty,c.Size));return image;}
        static Bitmap Render(ChatForm f,PetSettings settings){
            var output=new Bitmap(1120,420);var menu=(ContextMenuStrip)Field(f,"modeMenu");var input=(TextBox)Field(f,"input");
            using(var g=Graphics.FromImage(output)){
                g.Clear(Color.FromArgb(243,245,250));g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                for(int i=0;i<2;i++){
                    Call(f,"SelectTarget",i==0?"codex":"chatgpt");input.Text=i==0?"一起开始今天的工作":"把这个想法带到普通 ChatGPT";
                    using(var font=new Font("Microsoft YaHei UI",13,FontStyle.Bold))using(var ink=new SolidBrush(Design.Ink))g.DrawString(i==0?"Codex · 桌宠内回复":"ChatGPT 普通 · 桌面草稿",font,ink,32+i*560,20);
                    int left=30+i*560,top=78;AlphaForm.Bubble(g,new RectangleF(left,top,f.Width,f.Height),28,settings);
                    using(var image=Capture(f)){var saved=g.Save();using(var p=Design.Round(new RectangleF(left,top,f.Width,f.Height),28))g.SetClip(p);g.DrawImageUnscaled(image,left,top);g.Restore(saved);}
                    menu.PerformLayout();menu.Size=menu.GetPreferredSize(Size.Empty);AlphaForm.Bubble(g,new RectangleF(left+16,top+80,menu.Width,menu.Height),11,settings);
                    using(var image=Capture(menu)){var saved=g.Save();using(var p=Design.Round(new RectangleF(left+16,top+80,menu.Width,menu.Height),11))g.SetClip(p);g.DrawImageUnscaled(image,left+16,top+80);g.Restore(saved);}
                    using(var font=new Font("Microsoft YaHei UI",9))using(var muted=new SolidBrush(Design.Muted))g.DrawString(i==0?"Alt+1 · 保留独立桌宠会话":"Alt+2 · 仍需在桌面点击发送",font,muted,left+18,322);
                }
                using(var font=new Font("Microsoft YaHei UI",8))using(var muted=new SolidBrush(Design.Muted))g.DrawString("产品控件离线渲染 · 菜单选择只切换模式，不发送内容",font,muted,32,380);
            }
            return output;
        }
        [STAThread]public static int Main(string[] args){
            try{
                try{SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{}
                Program.Qa=true;
#if !PRIVATE_PET
                Program.Testing=true;Program.Offline=true;Program.CharacterOverride=Path.Combine(args[0],"characters","default","character.json");
                Environment.SetEnvironmentVariable("PETBRIDGE_DATA_DIR",Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"control-data"));Directory.CreateDirectory(Program.DataRoot);
#endif
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(var pet=new PetForm()){
                    pet.PrepareCompactCheck();using(var f=new ChatForm(pet)){
                        var target=(ChatModeButton)Field(f,"target");var input=(TextBox)Field(f,"input");var send=(IconButton)Field(f,"send");var menu=(ContextMenuStrip)Field(f,"modeMenu");var reply=(ReplyCard)Field(f,"reply");
                        Need(f.Height==68&&f.Controls.Count==4,"one compact row and four controls");
                        foreach(Control a in f.Controls){Need(!(a is ComboBox),"no top combo row");if(a is IconButton)Need(((IconButton)a).Kind!="plus","no plus button");foreach(Control b in f.Controls)if(a!=b)Need(!a.Bounds.IntersectsWith(b.Bounds),"nonoverlapping compact controls");}
                        Need(menu.Items.Count==4&&menu.Items[2] is ToolStripSeparator,"two modes and a separated options menu");
                        input.Text="保留草稿 😀 & ?";((ToolStripMenuItem)menu.Items[1]).PerformClick();
                        Need(f.SelectedChatTarget=="chatgpt"&&pet.Settings.ChatTarget=="chatgpt"&&target.Target=="chatgpt","mode icon and saved preference agree");
                        Need(((ToolStripMenuItem)menu.Items[1]).Checked&&!((ToolStripMenuItem)menu.Items[0]).Checked,"current mode is marked");Need(send.Enabled&&send.AccessibleName.Contains("仍需"),"ordinary branch explicitly unsent");
                        int ended=(int)Field(pet,"terminalHandled");int transcript=reply.Transcript.TextLength;
                        ((Task)typeof(ChatForm).GetMethod("SendAsync",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(f,null)).Wait();
                        Need(input.Text=="保留草稿 😀 & ?"&&!pet.IsThinking&&(int)Field(pet,"terminalHandled")==ended&&reply.Transcript.TextLength==transcript,"offline ordinary action keeps draft and causes no Codex thinking/completion/reply");Need(f.LastFeedback.Contains("离线预览"),"desktop launch blocked in offline preview");
                        var method=typeof(ChatForm).GetMethod("ProcessCmdKey",BindingFlags.Instance|BindingFlags.NonPublic);object[] key={new Message(),Keys.Alt|Keys.D1};Need((bool)method.Invoke(f,key)&&f.SelectedChatTarget=="codex","Alt+1 changes only destination");key=new object[]{new Message(),Keys.Alt|Keys.D2};Need((bool)method.Invoke(f,key)&&f.SelectedChatTarget=="chatgpt","Alt+2 changes only destination");Need(input.Text=="保留草稿 😀 & ?"&&(int)Field(pet,"terminalHandled")==ended,"mode shortcuts preserve text and never send");
                        typeof(ChatForm).GetField("busy",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(f,true);Call(f,"UpdateSend");Need(!target.Enabled&&send.Enabled&&send.AccessibleName.Contains("停止"),"Codex busy preserves stop and locks mode");Call(f,"SelectTarget","codex");Need(f.SelectedChatTarget=="chatgpt","busy selection is rejected");typeof(ChatForm).GetField("busy",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(f,false);
                        input.Clear();Call(f,"UpdateSend");Need(!send.Enabled&&!f.SendLit,"empty input stays unlit");
                        using(var image=Render(f,pet.Settings))image.Save(args[1],ImageFormat.Png);
                        f.End();
                    }
                }
                File.WriteAllText(args[2],"PASS: compact one-row controls; no plus/combo; no overlap; mode/menu/icon/saved settings; keyboard source shortcuts; unchanged draft; no inference, completion, reply or desktop launch in offline ordinary branch; busy mode lock and stop; empty send unlit.");
                Console.WriteLine("Compact chat control checks PASS");return 0;
            }catch(Exception e){File.WriteAllText(args[2]+".error.txt",e.ToString());Console.WriteLine(e);return 1;}
        }
    }
}
