using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PetBridge {
    sealed class ChatForm : RoundedForm {
        readonly PetForm pet;readonly TextBox input=new TextBox();readonly IconButton send=new IconButton("send","发送消息");readonly ReplyCard reply;
        readonly ChatModeButton target=new ChatModeButton();readonly ContextMenuStrip modeMenu=new ContextMenuStrip();readonly ToolTip tips=new ToolTip();ToolStripMenuItem codexMode,ordinaryMode;string selectedTarget,feedback="";
        readonly List<string> history=new List<string>();Process pending;bool busy,closing;readonly Timer gaze=new Timer();
        [DllImport("user32.dll")] static extern bool GetCaretPos(out Point point);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,string text);
        public ChatForm(PetForm parent){pet=parent;VisualSettings=parent.Settings;reply=new ReplyCard(parent);reply.VisualSettings=parent.Settings;Text="搭档 · 输入";Name="PetBridgeComposer";Size=new Size(500,68);
            selectedTarget=ChatRouting.NormalizeTarget(parent.Settings.ChatTarget);
            target.SetBounds(16,15,40,38);target.Click+=delegate{ShowModeMenu();};
            var close=new IconButton("close","收起输入框");close.SetBounds(406,19,28,28);close.Click+=delegate{HideAll();};
            send.SetBounds(444,14,38,38);send.Peach=true;send.Enabled=false;input.TextChanged+=delegate{UpdateSend();SetHint(ModeHint,false);};
            input.SetBounds(68,22,326,27);input.BorderStyle=BorderStyle.None;input.Font=new Font("Microsoft YaHei UI",11);input.ForeColor=Design.Ink;input.MaxLength=12000;input.AccessibleName="聊天输入";
            input.HandleCreated+=delegate{UpdateTarget();};Controls.AddRange(new Control[]{target,input,close,send});
            BuildModeMenu();
            send.Click+=async delegate{if(busy){Cancel();return;}await SendAsync();};input.KeyDown+=async delegate(object s,KeyEventArgs e){if(e.KeyCode==Keys.Enter){e.SuppressKeyPress=true;if(!busy)await SendAsync();}else if(e.KeyCode==Keys.Escape){e.SuppressKeyPress=true;HideAll();}};
            gaze.Interval=80;gaze.Tick+=delegate{if(Visible&&input.Focused&&!busy){Point c;if(!GetCaretPos(out c))c=input.GetPositionFromCharIndex(Math.Max(0,input.SelectionStart-1));pet.LookAt(input.PointToScreen(new Point(c.X,c.Y+input.Font.Height/2)));}else pet.LookAt(null);};gaze.Start();
            VisibleChanged+=delegate{if(!Visible){pet.LookAt(null);modeMenu.Hide();if(!reply.IsDisposed)reply.Hide();}if(!pet.IsDisposed)pet.Invalidate();};FormClosing+=delegate(object s,FormClosingEventArgs e){if(!closing&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;HideAll();}};UpdateTarget();
        }
        void BuildModeMenu(){
            modeMenu.Renderer=new ChatModeMenuRenderer();modeMenu.Font=new Font("Microsoft YaHei UI",9.5f);modeMenu.ForeColor=SettingsTheme.Ink;modeMenu.BackColor=Color.White;modeMenu.Padding=new Padding(5,6,5,6);modeMenu.ShowImageMargin=true;modeMenu.ShowCheckMargin=false;modeMenu.ImageScalingSize=new Size(20,20);modeMenu.DropShadowEnabled=true;modeMenu.ShowItemToolTips=true;
            codexMode=ModeItem("Codex","codex","Alt+1","独立 Codex 会话，回复显示在桌宠里");
            ordinaryMode=ModeItem("ChatGPT 普通聊天","chatgpt","Alt+2","打开普通聊天的桌面草稿，仍需在桌面点击发送");
            modeMenu.Items.Add(new ToolStripSeparator{AutoSize=false,Height=9,Width=258});
            var options=new ToolStripMenuItem("聊天选项",SettingsTheme.Image("settings")){AutoSize=false,Size=new Size(258,36)};options.DropDown.Renderer=modeMenu.Renderer;options.DropDown.Padding=new Padding(5,6,5,6);modeMenu.Items.Add(options);
            options.DropDownItems.Add("复制输入内容",null,delegate{CopyInput();});
            options.DropDownItems.Add("回到绑定的桌面对话",null,delegate{pet.OpenCurrent();});
            options.DropDownItems.Add("带到 Codex 桌面草稿",null,delegate{OpenDraft("codex");});
            options.DropDownItems.Add("带到 ChatGPT 普通聊天草稿",null,delegate{OpenDraft("chatgpt");});
            options.DropDownItems.Add(new ToolStripSeparator());
            options.DropDownItems.Add("显示 Codex 桌宠聊天记录",null,delegate{reply.Place();reply.Show(pet);});
            options.DropDownItems.Add("剩余用量",null,delegate{pet.ToggleUsage();});
            options.DropDownItems.Add("详细设置…",null,delegate{pet.OpenSettings();});
            options.DropDownItems.Add("清空本次 Codex 桌宠对话",null,delegate{if(busy)return;history.Clear();reply.Transcript.Clear();reply.Hide();});
            modeMenu.Opening+=delegate(object sender,System.ComponentModel.CancelEventArgs e){if(busy)e.Cancel=true;SyncModeChecks();};
            modeMenu.SizeChanged+=delegate{if(modeMenu.Width>20&&modeMenu.Height>20){var old=modeMenu.Region;using(var p=Design.Round(new RectangleF(0,0,modeMenu.Width,modeMenu.Height),11))modeMenu.Region=new Region(p);if(old!=null)old.Dispose();}};
        }
        ToolStripMenuItem ModeItem(string name,string destination,string shortcut,string description){
            var item=new ToolStripMenuItem(name,ChatModeGlyph.Image(destination)){AutoSize=false,Size=new Size(258,36),ShortcutKeys=destination=="codex"?(Keys.Alt|Keys.D1):(Keys.Alt|Keys.D2),ShortcutKeyDisplayString=shortcut,ToolTipText=description,Tag=destination,AccessibleName=name+" · "+description};item.Click+=delegate{SelectTarget(destination);};modeMenu.Items.Add(item);return item;
        }
        void SyncModeChecks(){if(codexMode!=null)codexMode.Checked=selectedTarget=="codex";if(ordinaryMode!=null)ordinaryMode.Checked=selectedTarget=="chatgpt";}
        void SelectTarget(string destination){
            if(busy){SetHint("Codex 正在回复，请先等待或停止后再切换。",true);return;}
            selectedTarget=ChatRouting.NormalizeTarget(destination);pet.Settings.ChatTarget=selectedTarget;pet.Settings.Save();if(ChatRouting.IsOrdinary(selectedTarget))reply.Hide();UpdateTarget();
            // Keep selection and sending separate: never focus the text box here.
        }
        void ShowModeMenu(){
            if(busy)return;if(modeMenu.Visible){modeMenu.Hide();return;}SyncModeChecks();modeMenu.PerformLayout();modeMenu.Size=modeMenu.GetPreferredSize(Size.Empty);
            var work=Screen.FromControl(this).WorkingArea;var point=target.PointToScreen(new Point(0,target.Height+7));
            if(point.Y+modeMenu.Height>work.Bottom-8)point.Y=target.PointToScreen(Point.Empty).Y-modeMenu.Height-7;
            point.X=Math.Max(work.Left+8,Math.Min(point.X,work.Right-modeMenu.Width-8));point.Y=Math.Max(work.Top+8,point.Y);modeMenu.Show(point);
        }
        protected override bool ProcessCmdKey(ref Message message,Keys keys){if(keys==(Keys.Alt|Keys.D1)){SelectTarget("codex");return true;}if(keys==(Keys.Alt|Keys.D2)){SelectTarget("chatgpt");return true;}return base.ProcessCmdKey(ref message,keys);}
        public string SelectedChatTarget {get{return selectedTarget;}}
        public string LastFeedback {get{return feedback;}}
        string ModeHint {get{return ChatRouting.IsOrdinary(SelectedChatTarget)?"ChatGPT 普通聊天 · 桌面草稿，仍需在桌面点击发送":"Codex · 独立会话，在桌宠内回复";}}
        void SetHint(string text,bool error){feedback=text;target.AccessibleDescription=text;input.AccessibleDescription=text;tips.SetToolTip(target,ModeHint+"\n点击切换 · Alt+1 / Alt+2");if(Visible&&(error||text!=ModeHint))tips.Show(text,target,new Point(0,-30),5000);}
        void UpdateTarget(){target.Target=SelectedChatTarget;target.AccessibleName=ChatRouting.IsOrdinary(SelectedChatTarget)?"聊天模式：ChatGPT 普通聊天":"聊天模式：Codex";target.Invalidate();SyncModeChecks();if(input.IsHandleCreated)SendMessage(input.Handle,0x1501,IntPtr.Zero,ChatRouting.IsOrdinary(SelectedChatTarget)?"带到 ChatGPT 桌面草稿…":"和 Codex 聊聊…");SetHint(ModeHint,false);UpdateSend();}
        public void PlaceNearPet(){var work=Screen.FromControl(pet).WorkingArea;Location=new Point(Math.Max(work.Left+8,Math.Min(pet.Left+pet.Width/2-Width/2,work.Right-Width-8)),Math.Max(work.Top+8,Math.Min(pet.Top+pet.ArtHeight+pet.Settings.ToolbarGap,work.Bottom-Height-8)));if(reply.Visible)reply.Place();}
        public void FocusInput(){input.Focus();if(IsHandleCreated)BeginInvoke(new Action(delegate{if(Visible&&!IsDisposed){Activate();input.Focus();}}));}
        public void HideAll(){modeMenu.Hide();Hide();reply.Hide();pet.LookAt(null);pet.ApplySettings();}
        public void End(){if(IsDisposed)return;gaze.Stop();gaze.Dispose();tips.Dispose();closing=true;Cancel();reply.End();DisposeMenuImages(modeMenu.Items);modeMenu.Dispose();Close();}
        public bool SendLit {get{return send.AccentActive;}}
        static void DisposeMenuImages(ToolStripItemCollection items){foreach(ToolStripItem item in items){if(item.Image!=null)item.Image.Dispose();var menu=item as ToolStripMenuItem;if(menu!=null&&menu.HasDropDownItems)DisposeMenuImages(menu.DropDownItems);}}
        void UpdateSend(){bool ready=busy||input.Text.Trim().Length>0;send.Enabled=ready;send.AccentActive=ready;target.Enabled=!busy;string action=busy?"停止 Codex 回复":ChatRouting.IsOrdinary(SelectedChatTarget)?"带到普通 ChatGPT 桌面草稿，仍需在桌面点击发送":"发送给 Codex，在桌宠内回复";send.AccessibleName=action;tips.SetToolTip(send,action);send.Invalidate();}
        void CopyInput(){if(String.IsNullOrWhiteSpace(input.Text)){SetHint("请先输入要复制的内容。",true);return;}try{Clipboard.SetText(input.Text);SetHint("已复制输入内容，可在桌面聊天框粘贴。",false);}catch{SetHint("暂时无法复制，输入已保留，请稍后重试。",true);}}
        void OpenDraft(string destination){if(busy){SetHint("Codex 正在回复，请先等待或停止，再打开桌面草稿。",true);return;}string url,error;if(!ChatRouting.TryDraftUrl(input.Text,destination,out url,out error)){SetHint(error,true);if(input.Text.Trim().Length>0)MessageBox.Show(this,error,"桌面草稿",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            if(Program.Offline){SetHint("离线预览：不会打开桌面或发送消息；输入已保留。",false);return;}
            try{Program.OpenUrl(url);SetHint(ChatRouting.IsOrdinary(destination)?"已请求打开普通聊天草稿 · 请在桌面点击发送":"已请求打开 Codex 草稿 · 请在桌面点击发送",false);}
            catch{SetHint("未能请求打开桌面，输入已保留；可从左侧模式菜单复制后粘贴。",true);}
        }
        void Cancel(){try{if(pending!=null&&!pending.HasExited)pending.Kill();}catch{}}
        async Task SendAsync(){if(ChatRouting.IsOrdinary(SelectedChatTarget)){OpenDraft("chatgpt");return;}string text=input.Text.Trim();if(text.Length==0)return;busy=true;send.Kind="stop";UpdateSend();send.Invalidate();input.Clear();reply.Append("你",text);reply.Status.Text="搭档正在思考…";reply.Place();reply.Show(pet);input.Focus();pet.SetBusy(true);
            string prompt="你是 PetBridge 桌面搭档，一个友好的虚拟角色。角色外观由用户选择的素材决定，不要声称自己是固定的猫耳或某种服装。请用自然简洁的中文回答。只聊天，不调用工具、不读取文件、不执行命令。你在独立桌宠窗口中，不拥有当前ChatGPT应用对话的历史。下面是这次桌宠对话的近期上下文：\n"+String.Join("\n",history.ToArray())+"\n用户："+text+"\n搭档：";
            try{string result=await Task.Run(()=>ChatBackend.Run(prompt,pet.Settings,p=>pending=p));if(IsDisposed)return;reply.Append("搭档",result);reply.Status.Text="搭档";history.Add("用户："+text);history.Add("搭档："+result);while(history.Count>8)history.RemoveAt(0);pet.Usage.Refresh();}
            catch(Exception ex){if(!IsDisposed){reply.Append("Codex 连接提示",ex.Message);reply.Status.Text="稍后再试，或从模式菜单打开桌面草稿";input.Text=text;}}
            finally{pending=null;busy=false;if(!IsDisposed){send.Kind="send";UpdateSend();pet.SetBusy(false);pet.LocalChatEnded();input.Focus();}}
        }
    }
}
