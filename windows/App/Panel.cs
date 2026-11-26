// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Drawing;
using System.Windows.Forms;
namespace Winintel.Windows {
internal sealed class Panel : Form {
    internal readonly TextBox TestBox=new TextBox{Multiline=true};
    readonly ComboBox microphones=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
    readonly Label status=new Label{AutoSize=false};
    readonly Settings settings;readonly Action saved;
    internal event Action MeetingRequested;
    internal Panel(Settings s,Action onSaved){
        settings=s;saved=onSaved;Text="数萌 AI 输入法";ClientSize=new Size(600,530);MinimumSize=MaximumSize=Size;MaximizeBox=false;Font=new Font("Microsoft YaHei UI",10);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.White;AutoScaleMode=AutoScaleMode.Dpi;
        var title=Skin.Label("数萌 AI 输入法",21,true);title.Location=new Point(28,22);Controls.Add(title);
        var sub=Skin.Label("winintel AI  ·  自然输入，专注表达");sub.ForeColor=Skin.Muted;sub.Location=new Point(30,67);Controls.Add(sub);
        var tabs=new MaterialTabs{Location=new Point(24,108),Size=new Size(552,350),Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};
        var input=new MaterialPage("键盘输入"){Padding=new Padding(14)};
        var voice=new MaterialPage("语音输入"){Padding=new Padding(14)};
        var office=new MaterialPage("翻译与字幕"){Padding=new Padding(14)};
        tabs.TabPages.AddRange(new[]{input,voice,office});Controls.Add(tabs);
        var tip=Skin.Label("候选栏跟随光标，空格选词，输入后自动收起。");tip.Location=new Point(18,20);input.Controls.Add(tip);
        var shortcut=Skin.Label("1–9 选词     ↑ ↓ 切换     PgUp / PgDn 翻页");shortcut.ForeColor=Skin.Muted;shortcut.Location=new Point(18,54);input.Controls.Add(shortcut);
        var testLabel=Skin.Label("试着输入");testLabel.Location=new Point(18,108);input.Controls.Add(testLabel);
        var testCard=new MaterialCard{Location=new Point(16,141),Size=new Size(508,112),Padding=new Padding(14)};
        Skin.Editor(TestBox);TestBox.Dock=DockStyle.Fill;TestBox.AccessibleName="输入测试区";testCard.Controls.Add(TestBox);input.Controls.Add(testCard);
        var hint=Skin.Label("Win + 空格，切换到数萌 AI 输入法");hint.Location=new Point(18,268);hint.ForeColor=Skin.Muted;input.Controls.Add(hint);
        var micLabel=Skin.Label("麦克风",12,true);micLabel.Location=new Point(18,22);voice.Controls.Add(micLabel);
        microphones.SetBounds(18,63,490,32);microphones.Font=Font;voice.Controls.Add(microphones);
        foreach(var mic in Microphone.List()){int index=microphones.Items.Add(mic);if(mic.Id==s.Microphone)microphones.SelectedIndex=index;}if(microphones.SelectedIndex<0&&microphones.Items.Count>0)microphones.SelectedIndex=0;
        var voiceCard=new MaterialCard{Location=new Point(16,120),Size=new Size(508,118)};
        var vTitle=Skin.Label("按 F8，说出你想写的内容",12,true);vTitle.Location=new Point(18,18);voiceCard.Controls.Add(vTitle);
        var vText=Skin.Label("再按 F8 结束。检查草稿后，点击「确认上屏」。");vText.Location=new Point(18,57);vText.ForeColor=Skin.Muted;voiceCard.Controls.Add(vText);voice.Controls.Add(voiceCard);
        var save=Skin.Button("保存设置",true);save.Location=new Point(16,254);voice.Controls.Add(save);save.Click+=(a,b)=>{Apply();Status("麦克风设置已保存");};
        var officeCard=new MaterialCard{Location=new Point(16,20),Size=new Size(508,238)};
        var oTitle=Skin.Label("让语言自然流动",16,true);oTitle.Location=new Point(22,25);officeCard.Controls.Add(oTitle);
        var oText=Skin.Label("中英互译、双语字幕与会议记录");oText.Location=new Point(22,75);oText.ForeColor=Skin.Muted;officeCard.Controls.Add(oText);
        var privacy=Skin.Label("文字与语音在本机处理");privacy.Location=new Point(22,111);privacy.ForeColor=Skin.Muted;officeCard.Controls.Add(privacy);
        var meeting=Skin.Button("打开工作台",true);meeting.Location=new Point(18,170);officeCard.Controls.Add(meeting);meeting.Click+=(a,b)=>{if(MeetingRequested!=null)MeetingRequested();};office.Controls.Add(officeCard);
        status.SetBounds(30,477,535,30);status.ForeColor=Skin.Muted;status.Text="离线运行  ·  选词习惯仅保存在本机";Controls.Add(status);
        Shown+=(a,b)=>Skin.RoundWindow(this);
        FormClosing+=(a,b)=>{if(b.CloseReason==CloseReason.UserClosing){b.Cancel=true;Hide();}};
    }
    void Apply(){if(microphones.SelectedItem!=null)settings.Microphone=((Microphone)microphones.SelectedItem).Id;settings.Save();saved();}
    internal void Status(string text){if(!IsDisposed&&!Disposing&&!status.IsDisposed)status.Text=text;}
}
internal sealed class VoicePreview:Form {
    bool hiddenByUser;
    bool updating;
    bool sessionVisible,expanded;IntPtr target;
    readonly Timer visibility=new Timer{Interval=250};
    readonly TextBox editor=new TextBox{Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Font=new Font("Microsoft YaHei UI",12)};
    readonly Label label=new Label{Dock=DockStyle.Fill,Padding=new Padding(12,11,32,0),ForeColor=Skin.Muted,AutoEllipsis=true,Font=new Font("Microsoft YaHei UI",9)};
    readonly MaterialButton confirm=Skin.Button("确认上屏",true);
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr h,uint flags);
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool IsIconic(IntPtr h);
    [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
    internal event Action<string> Edited;
    internal event Action ClearRequested,UndoRequested,ConfirmRequested,StopRequested;
    internal VoicePreview(){
        Text="数萌 AI · 语音草稿";FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=false;ClientSize=new Size(420,158);Padding=new Padding(1);StartPosition=FormStartPosition.Manual;BackColor=Color.White;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;DoubleBuffered=true;
        var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(6),WrapContents=false,BackColor=Color.White};
        var clear=Skin.Button("清空");clear.Width=52;var undo=Skin.Button("撤销");undo.Width=52;var copy=Skin.Button("复制");copy.Width=52;var expand=Skin.Button("展开");expand.Width=52;confirm.Width=108;
        clear.Warm=undo.Warm=copy.Warm=expand.Warm=confirm.Warm=true;
        bar.Controls.AddRange(new Control[]{clear,undo,copy,expand,confirm});
        var card=new System.Windows.Forms.Panel{Dock=DockStyle.Fill,Padding=new Padding(12,4,12,4)};Skin.Editor(editor);card.Controls.Add(editor);
        var header=new System.Windows.Forms.Panel{Dock=DockStyle.Top,Height=36,BackColor=QuickStyle.Pale};header.Controls.Add(label);
        var close=new QuickButton{Symbol="close",Dock=DockStyle.Right,Width=32,AccessibleName="关闭语音草稿"};header.Controls.Add(close);close.Click+=(s,e)=>Close();
        Controls.Add(card);Controls.Add(header);Controls.Add(bar);
        expand.Click+=(s,e)=>{expanded=!expanded;ClientSize=new Size(420,expanded?280:158);expand.Text=expanded?"收起":"展开";Position();};
        clear.Click+=(s,e)=>{if(ClearRequested!=null)ClearRequested();};
        undo.Click+=(s,e)=>{if(UndoRequested!=null)UndoRequested();};
        copy.Click+=(s,e)=>{if(editor.Text.Length>0)Clipboard.SetText(editor.Text);};
        confirm.Click+=(s,e)=>{if(editor.ReadOnly){if(StopRequested!=null)StopRequested();}else if(ConfirmRequested!=null)ConfirmRequested();};
        editor.TextChanged+=(s,e)=>{if(!updating&&Edited!=null)Edited(editor.Text);};
        visibility.Tick+=(s,e)=>RefreshVisibility();
        Shown+=(s,e)=>Skin.RoundWindow(this);
    }
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);QuickStyle.Border(e.Graphics,ClientRectangle);}
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var value=base.CreateParams;value.ExStyle|=0x08000000|0x80;return value;}}
    protected override void WndProc(ref Message message){
        if(message.Msg==0x21){
            if(editor.ReadOnly){message.Result=new IntPtr(3);return;}
            // Only an intentional click on a finished draft may enter edit mode.
            message.Result=new IntPtr(1);return;
        }
        base.WndProc(ref message);
    }
    protected override void OnFormClosing(FormClosingEventArgs e){
        if(e.CloseReason==CloseReason.UserClosing&&!Disposing){e.Cancel=true;hiddenByUser=true;Hide();}
        base.OnFormClosing(e);
    }
    internal void BeginSession(IntPtr window){if(IsDisposed||Disposing)return;target=window;sessionVisible=true;hiddenByUser=false;Position();visibility.Start();}
    internal void EndSession(){sessionVisible=false;visibility.Stop();Hide();}
    void Position(){
        var work=Screen.FromHandle(target).WorkingArea;
        // A small corner panel stays clear of the application's current text line.
        Location=new Point(Math.Max(work.Left,work.Right-Width-12),Math.Max(work.Top,work.Bottom-Height-60));
    }
    void RefreshVisibility(){
        if(IsDisposed||Disposing||!sessionVisible||hiddenByUser)return;
        var foreground=GetForegroundWindow();
        bool relevant=IsWindow(target)&&!IsIconic(target)&&(foreground==Handle||GetAncestor(foreground,2)==GetAncestor(target,2));
        if(relevant){if(!Visible)Show();}else if(Visible)Hide();
    }
    internal void Draft(string text,bool recording){
        if(IsDisposed||Disposing||editor.IsDisposed)return;
        updating=true;try{if(editor.Text!=text){editor.Text=text;editor.SelectionStart=editor.TextLength;}}finally{updating=false;}
        editor.ReadOnly=recording;confirm.Enabled=recording||text.Length>0;confirm.Text=recording?"结束录音":"确认上屏";
    }
    internal void UpdateText(string text){
        if(IsDisposed||Disposing||label.IsDisposed)return;
        label.Text=text;RefreshVisibility();
    }
    protected override void Dispose(bool disposing){if(disposing)visibility.Dispose();base.Dispose(disposing);}
}
}
