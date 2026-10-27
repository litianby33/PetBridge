using System;
using System.Drawing;
using System.Windows.Forms;
namespace PetBridge {
    sealed class SettingsForm : SettingsShell {
        readonly PetForm pet;
        public SettingsForm(PetForm p):base(p,"PetBridge"){pet=p;
            var general=Page("常用");
            var visual=Page("外观");var alerts=Page("提醒");var behavior=Page("动作");var quota=Page("排版");var activity=Page("状态");var bubbles=Page("气泡");var tests=Page("测试");var connections=Page("联动");var edgePage=Page("贴边");var glow=Page("颜色");var binding=Page("绑定");var s=pet.Settings;
            SetupBindings(binding);
            Check(connections,"普通 ChatGPT 聊天联动",s.AppChatMonitoring&&s.OrdinaryChatMonitoring,25,delegate(bool v){s.AppChatMonitoring=true;s.OrdinaryChatMonitoring=v;});
            Check(connections,"Work / 云端 Codex 任务联动",s.AppChatMonitoring&&s.WorkAppMonitoring,66,delegate(bool v){s.AppChatMonitoring=true;s.WorkAppMonitoring=v;});
            Check(connections,"需要选择或输入回复时提醒我",s.InputReminders,107,delegate(bool v){s.AppChatMonitoring=true;s.InputReminders=v;});
            var bridgeInfo=new Label{ForeColor=Design.Muted};bridgeInfo.SetBounds(18,158,410,320);connections.Controls.Add(bridgeInfo);Action updateBridge=delegate{bridgeInfo.Text=pet.AppBridgeDescription;};updateBridge();var bridgeTimer=new System.Windows.Forms.Timer{Interval=2000};bridgeTimer.Tick+=delegate{updateBridge();};bridgeTimer.Start();FormClosed+=delegate{bridgeTimer.Dispose();};
            Slider(general,"人物大小",50,300,pet.SettingsScalePercent,25,v=>pet.SetSettingsScale(v),"%");
            Check(general,"跟随 ChatGPT 窗口",s.FollowChatGPT,108,v=>s.FollowChatGPT=v);
            Check(general,"随 ChatGPT 启动",s.AutoStartWithChatGPT,158,delegate(bool v){s.AutoStartWithChatGPT=v;if(v)Program.StartWatcher();});
            ActionButton(general,"回到当前 ChatGPT 对话",18,224,390,delegate{pet.OpenCurrent();});
            ActionButton(general,"把人物移回主屏",18,276,390,delegate{pet.MoveToPrimary();});
            var generalHint=new Label{Text="人物也可以用 Ctrl + 鼠标滚轮缩放。\n\n动作、提醒预览和边缘测试已收进“测试工具”。\n关闭窗口会记住你拖动后的位置。",ForeColor=SettingsTheme.Muted};generalHint.SetBounds(18,343,414,110);general.Controls.Add(generalHint);
            ActionButton(tests,"预览跳跃",18,465,180,delegate{pet.PreviewJump();});
            ActionButton(tests,"预览招手",216,465,180,delegate{pet.PreviewWave();});
            ActionButton(tests,"预览向右行走",18,255,180,delegate{pet.Play(1,4);});
            ActionButton(tests,"预览向左行走",216,255,180,delegate{pet.Play(2,4);});
            Slider(visual,"胶囊投影深浅",0,100,s.ShadowOpacity,25,v=>s.ShadowOpacity=v,"%");
            Slider(visual,"投影柔和范围",0,20,s.ShadowBlur,96,v=>s.ShadowBlur=v," px");
            Slider(visual,"投影向下偏移",0,12,s.ShadowOffset,167,v=>s.ShadowOffset=v," px");
            Slider(visual,"工具条与人物距离",-30,60,s.ToolbarGap,238,v=>s.ToolbarGap=v," px");
            Slider(visual,"额度胶囊间距",0,40,s.UsageGap,309,v=>s.UsageGap=v," px");
            var gapHint=new Label{Text="距离可设为负值；收起时贴近脚边，展开向下生长。",ForeColor=Design.Muted};gapHint.SetBounds(18,376,408,32);visual.Controls.Add(gapHint);
            Check(quota,"显示额度更新时间",s.ShowUpdatedAt,25,v=>s.ShowUpdatedAt=v);
            ActionButton(quota,"编辑横排 / 竖排文字…",18,80,350,delegate{pet.EditQuotaLayout();});
            var directionLabel=new Label{Text="额度胶囊方向",ForeColor=Design.Ink};directionLabel.SetBounds(18,132,350,25);quota.Controls.Add(directionLabel);
            var quotaDirection=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,AccessibleName="额度胶囊方向"};quotaDirection.Items.AddRange(new object[]{"靠近左右边缘时自动竖排","始终横排","始终竖排"});quotaDirection.SetBounds(18,162,374,32);quotaDirection.SelectedIndex=s.QuotaLayoutMode=="horizontal"?1:s.QuotaLayoutMode=="vertical"?2:0;quotaDirection.SelectedIndexChanged+=delegate{s.QuotaLayoutMode=quotaDirection.SelectedIndex==1?"horizontal":quotaDirection.SelectedIndex==2?"vertical":"auto";pet.ApplySettings();};quota.Controls.Add(quotaDirection);
            Slider(quota,"自动竖排的边缘余量",0,120,s.QuotaEdgeMargin,218,v=>s.QuotaEdgeMargin=v," px");
            Check(quota,"关闭完整额度后，悬停人物显示极简额度",s.HoverUsage,322,v=>s.HoverUsage=v);
            var layoutHint=new Label{Text="横排和竖排分别保存，可单独设置宽高、位置，\n拖动标签、百分比和重置时间，或输入精确坐标。\n\n竖排使用上下两组额度和水平分隔线。\n自动切换有缓冲，靠近临界位置不会频繁闪动。\n修改后立即应用；恢复默认只影响正在编辑的方向。",ForeColor=Design.Muted};layoutHint.SetBounds(18,378,414,146);quota.Controls.Add(layoutHint);
            Check(alerts,"显示下方额度胶囊",s.ShowUsage,25,v=>s.ShowUsage=v);
            Check(alerts,"额度不足时让搭档提醒我",s.QuotaReminders,66,v=>s.QuotaReminders=v);
            Number(alerts,"5 小时剩余少于",s.FiveHourWarning,116,0,100,v=>s.FiveHourWarning=v,"% 提醒");
            Number(alerts,"本周剩余少于",s.WeeklyWarning,169,0,100,v=>s.WeeklyWarning=v,"% 提醒");
            Check(alerts,"Work / Codex 任务完成时提醒",s.TaskReminders,226,v=>s.TaskReminders=v);
            Check(alerts,"普通聊天完成提醒（需开启联动）",s.ChatGPTCompletionReminders,270,v=>s.ChatGPTCompletionReminders=v);
            var hint=new Label{Text="只在任务结束后判断是否低于阈值。\n同一次完成、失败或中止最多提醒一次。\n定时刷新不会反复提醒已用完。\n阈值 0% 关闭低额提醒，耗尽仍说明重置时间。",ForeColor=Design.Muted};hint.SetBounds(18,321,410,76);alerts.Controls.Add(hint);
            var preview=new SettingsButton{Text="预览额度提醒",FlatStyle=FlatStyle.Flat};preview.SetBounds(18,415,170,35);preview.Click+=delegate{pet.PreviewQuota();};alerts.Controls.Add(preview);
            Check(behavior,"左右贴边时跑进去，露出半身",s.AutoDock,25,v=>s.AutoDock=v);
            Number(behavior,"无人碰触时，间隔",s.IdleWaveSeconds,130,0,600,v=>s.IdleWaveSeconds=v,"秒招手");
            Check(activity,"显示任务进行中的三点胶囊",s.ShowActivity,25,v=>s.ShowActivity=v);
            var sideLabel=new Label{Text="展开时放在哪边",ForeColor=Design.Ink};sideLabel.SetBounds(18,73,180,26);activity.Controls.Add(sideLabel);var side=new ComboBox{FlatStyle=FlatStyle.Flat,DropDownStyle=ComboBoxStyle.DropDownList};side.Items.AddRange(new object[]{"自动","左边","右边"});side.SelectedIndex=s.ActivitySide=="left"?1:s.ActivitySide=="right"?2:0;side.SetBounds(199,68,180,30);side.SelectedIndexChanged+=delegate{s.ActivitySide=side.SelectedIndex==1?"left":side.SelectedIndex==2?"right":"auto";pet.ApplySettings();};activity.Controls.Add(side);
            Number(activity,"三点胶囊水平偏移",s.ActivityOffsetX,118,-240,240,v=>s.ActivityOffsetX=v,"px");
            Number(activity,"三点胶囊垂直偏移",s.ActivityOffsetY,170,-300,300,v=>s.ActivityOffsetY=v,"px");
            Number(activity,"三点胶囊大小",(int)Math.Round(s.ActivityScale*100),222,50,150,v=>s.ActivityScale=v/100.0,"%");
            Number(activity,"与人物的基础距离",s.ActivityGap,269,0,100,v=>s.ActivityGap=v,"px");
            ActionButton(activity,"预览三点加载状态（15 秒）",18,316,330,delegate{pet.PreviewActivity();});
            var activityHint=new Label{Text="悬停三点看用量，点击卡片标题切换任务。\n贴在左边时，三点显示在右边；反之亦然。\n监听本地 Codex 及桌面应用内的 Chat / Work。",ForeColor=Design.Muted};activityHint.SetBounds(18,363,408,50);activity.Controls.Add(activityHint);Check(activity,"未固定任务时，跟随最近操作的 Codex 对话",s.TokenUsageFollowRecent,426,v=>s.TokenUsageFollowRecent=v);
            Number(bubbles,"提醒气泡上下偏移",s.NoticeOffsetY,28,-500,300,v=>s.NoticeOffsetY=v,"px");
            Check(bubbles,"弹出时带轻微回弹",s.NoticeBounce,84,v=>s.NoticeBounce=v);
            var clickLabel=new Label{Text="任务提醒点击后",ForeColor=Design.Ink};clickLabel.SetBounds(18,137,180,26);bubbles.Controls.Add(clickLabel);
            var clickMode=new ComboBox{FlatStyle=FlatStyle.Flat,DropDownStyle=ComboBoxStyle.DropDownList,AccessibleName="完成气泡点击行为"};clickMode.Items.AddRange(new object[]{"仅关闭气泡（默认）","关闭并跳转到对话"});clickMode.SelectedIndex=s.NavigateOnTaskNoticeClick?1:0;clickMode.SetBounds(199,132,210,30);clickMode.SelectedIndexChanged+=delegate{s.NavigateOnTaskNoticeClick=clickMode.SelectedIndex==1;pet.ApplySettings();};bubbles.Controls.Add(clickMode);
            var bubbleHint=new Label{Text="负值往上，正值往下。\n任务完成和低额度气泡会一直保留，\n任务与额度提醒按上方选择处理。\n提问提醒仍可点击回到对话回答。\n从边缘出来提醒后，未点击前不会藏回。",ForeColor=Design.Muted};bubbleHint.SetBounds(18,205,410,130);bubbles.Controls.Add(bubbleHint);
            ActionButton(bubbles,"预览任务完成气泡",18,365,340,delegate{pet.TestReminder();});
            ActionButton(bubbles,"预览低额度气泡",18,420,340,delegate{pet.PreviewQuota();});
            ActionButton(tests,"测试任务完成气泡",18,28,370,delegate{pet.TestReminder();});
            ActionButton(tests,"测试低额度气泡",18,85,370,delegate{pet.PreviewQuota();});
            ActionButton(tests,"收进左侧，4 秒后弹出提醒",18,142,370,delegate{Hide();pet.TestDockReminder();});
            ActionButton(tests,"预览三点加载状态（15 秒）",18,199,370,delegate{pet.PreviewActivity();});
            ActionButton(tests,"测试等待回答提醒气泡",18,405,370,delegate{pet.PreviewInputReminder();});
            var testHint=new Label{Text="这些按钮只模拟界面状态。\n不会发送聊天，也不消耗绘图额度。",ForeColor=Design.Muted};testHint.SetBounds(18,304,410,40);tests.Controls.Add(testHint);
            Check(behavior,"贴边悬停时显示小额度提示",s.DockHoverUsage,175,v=>s.DockHoverUsage=v);
            Check(behavior,"贴边额度提示放在人物下方",s.DockQuotaBelow,216,v=>s.DockQuotaBelow=v);
            var desc=new Label{Text="输入时，搭档会看向光标位置；\n发送后切换为思考动作。\n\n工具条离开人物后立即收缩。\n贴边只在点击后展开，悬停仅显示额度。\n\n0 秒表示关闭自动招手。",ForeColor=Design.Muted};desc.SetBounds(18,261,400,140);behavior.Controls.Add(desc);
            Number(edgePage,"贴边三点与人物距离",s.DockActivityGap,25,-20,80,v=>s.DockActivityGap=v,"px");
            Number(edgePage,"贴边三点垂直偏移",s.DockActivityOffsetY,78,-300,300,v=>s.DockActivityOffsetY=v,"px");
            Number(edgePage,"贴边触发距离",s.DockThreshold,131,0,20,v=>s.DockThreshold=v,"px");
            Check(edgePage,"其他应用全屏时自动避让",s.AvoidFullscreen,188,v=>s.AvoidFullscreen=v);
            var edgeHint=new Label{Text="按人物实际轮廓判断左右边缘。\n拖动接近边缘时会显示收起预告。\n贴边期间保留探头姿势，任务用三点表示。\n\n右键人物可临时隐藏；点击托盘搭档图标恢复。",ForeColor=Design.Muted};edgeHint.SetBounds(18,240,410,135);edgePage.Controls.Add(edgeHint);
            Slider(glow,"状态圆点发光强度",0,100,s.NoticeGlow,25,v=>s.NoticeGlow=v,"%");
            Slider(glow,"状态圆点发光范围",0,24,s.NoticeGlowRadius,96,v=>s.NoticeGlowRadius=v,"px");
            ColorSetting(glow,"任务完成",s.CompleteColor,178,v=>s.CompleteColor=v);
            ColorSetting(glow,"额度偏低",s.LowColor,227,v=>s.LowColor=v);
            ColorSetting(glow,"额度用完",s.EmptyColor,276,v=>s.EmptyColor=v);
            ColorSetting(glow,"5 小时重置",s.ResetColor,325,v=>s.ResetColor=v);
            ActionButton(tests,"预览额度用完 / 重置（轮流点击）",18,350,370,delegate{pet.PreviewResetOrEmpty();});

        }
        void SetupBindings(Control page){
            var description=new Label{Text="把一个本机 Codex 对话的技术编号或桌面链接贴到这里。\n它仅用于本机状态查询，不会发送聊天或复制登录信息。",ForeColor=Design.Muted};description.SetBounds(18,20,410,56);page.Controls.Add(description);
            var thread=new TextBox{BorderStyle=BorderStyle.FixedSingle,Text=pet.Settings.AppBridgeThreadId,AccessibleName="本机 Codex 对话编号或链接"};thread.SetBounds(18,84,390,28);page.Controls.Add(thread);
            ActionButton(page,"保存绑定",18,128,180,delegate{string value=thread.Text.Trim().TrimEnd('/');int slash=value.LastIndexOf('/');if(slash>=0)value=value.Substring(slash+1);Guid id;if(!Guid.TryParse(value,out id)){MessageBox.Show(this,"请输入本机 Codex 对话的编号或 codex://threads/ 链接。","绑定");return;}pet.Settings.AppBridgeThreadId=id.ToString();pet.Settings.ChatLink="codex://threads/"+id;pet.Settings.Save();pet.RefreshAppBinding();MessageBox.Show(this,"绑定已保存。可在联动页查看连接状态。","绑定");});
            var character=new Label{Text="已保存的角色：\n"+(pet.Settings.CharacterPath??"内置基础搭档"),ForeColor=Design.Muted};character.SetBounds(18,186,400,62);page.Controls.Add(character);
            ActionButton(page,"选择自己的 character.json…",18,258,350,delegate{using(var dialog=new OpenFileDialog{Filter="Character manifest|*.json",Title="选择角色配置"})if(dialog.ShowDialog(this)==DialogResult.OK)try{using(var bank=new SpriteBank(dialog.FileName)){}pet.Settings.CharacterPath=dialog.FileName;pet.Settings.Save();character.Text="角色已保存，下次打开生效：\n"+dialog.FileName;}catch(Exception error){MessageBox.Show(this,error.Message,"角色配置无法读取");}});
            var footer=new Label{Text="让 Codex 按 AGENTS.md 配置角色。\n缺失动作自动回退；没有跳跃帧就模拟跳。",ForeColor=Design.Muted};footer.SetBounds(18,327,405,62);page.Controls.Add(footer);
        }
        void ColorSetting(Control page,string text,string hex,int y,Action<string> set){var label=new Label{Text=text,ForeColor=Design.Ink};label.SetBounds(18,y+5,175,28);var b=new SettingsButton{Text=hex,BackColor=NoticeCard.ParseColor(hex),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};b.SetBounds(199,y,180,33);b.Click+=delegate{using(var dialog=new ColorDialog{Color=b.BackColor,FullOpen=true})if(dialog.ShowDialog(this)==DialogResult.OK){string value="#"+dialog.Color.R.ToString("x2")+dialog.Color.G.ToString("x2")+dialog.Color.B.ToString("x2");b.BackColor=dialog.Color;b.Text=value;set(value);pet.ApplySettings();}};page.Controls.AddRange(new Control[]{label,b});}
        void ActionButton(Control page,string text,int x,int y,int w,Action action){var b=new SettingsButton{Text=text,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(248,248,247),ForeColor=Design.Ink};b.SetBounds(x,y,w,38);b.Click+=delegate{action();};page.Controls.Add(b);}

        void Check(Control page,string label,bool value,int y,Action<bool> set){var c=new SettingsSwitch{Text=label,Checked=value,AutoSize=false,ForeColor=Design.Ink};c.Location=new Point(18,y);c.CheckedChanged+=delegate{set(c.Checked);pet.ApplySettings();SyncSkin();};page.Controls.Add(c);}
        void Slider(Control page,string label,int min,int max,int value,int y,Action<int> set,string unit){var l=new Label{Text=label,ForeColor=Design.Ink};l.SetBounds(18,y,290,23);var n=new NumericUpDown{BorderStyle=BorderStyle.FixedSingle,Minimum=min,Maximum=max,Increment=1,Value=Math.Max(min,Math.Min(max,value)),AccessibleName=label+"数值"};n.SetBounds(309,y-2,72,28);var u=new Label{Text=unit.Trim(),ForeColor=Design.Muted};u.SetBounds(383,y+2,35,23);var c=new SettingsSlider{Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),SmallChange=1,LargeChange=1,TickFrequency=1,TickStyle=TickStyle.None,AutoSize=false,AccessibleName=label};c.SetBounds(12,y+27,399,30);bool syncing=false;Action<int> change=delegate(int v){if(syncing)return;syncing=true;c.Value=v;n.Value=v;syncing=false;set(v);pet.ApplySettings();SyncSkin();};c.ValueChanged+=delegate{change(c.Value);};n.ValueChanged+=delegate{change((int)n.Value);};page.Controls.AddRange(new Control[]{l,n,u,c});}
        void Number(Control page,string label,int value,int y,int min,int max,Action<int> set,string unit){var l=new Label{Text=label,ForeColor=Design.Ink};l.SetBounds(18,y+5,180,26);var c=new NumericUpDown{BorderStyle=BorderStyle.FixedSingle,Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value))};c.SetBounds(199,y,84,30);var u=new Label{Text=unit,ForeColor=Design.Muted};u.SetBounds(298,y+5,100,26);c.ValueChanged+=delegate{set((int)c.Value);pet.ApplySettings();};page.Controls.AddRange(new Control[]{l,c,u});}
    }
}

