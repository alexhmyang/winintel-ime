// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Winintel.Windows {
internal static class QuickStyle {
    internal static readonly Color Orange=Color.FromArgb(242,113,35),Pale=Color.FromArgb(255,244,235);
    internal static void Border(Graphics g,Rectangle bounds){g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Skin.Round(new Rectangle(0,0,bounds.Width-1,bounds.Height-1),8))using(var pen=new Pen(Skin.Outline))g.DrawPath(pen,path);}
}
internal sealed class QuickButton:Button {
    internal string Symbol="",Subtitle="";
    internal bool Accent;
    bool hover;
    internal QuickButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;Cursor=Cursors.Hand;Font=new Font("Microsoft YaHei UI",10);TabStop=false;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint,true);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        var g=e.Graphics;g.Clear(Parent==null?Color.White:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=Skin.Round(new Rectangle(1,1,Width-2,Height-2),6))using(var brush=new SolidBrush(!Enabled?Skin.Surface:Accent?QuickStyle.Orange:hover?QuickStyle.Pale:Color.White))g.FillPath(brush,path);
        Color color=!Enabled?Skin.Muted:Accent?Color.White:hover?QuickStyle.Orange:Skin.Ink;
        int size=Math.Max(12,Math.Min(18,Height-14));int x=Text.Length>0&&Symbol.Length>0?12:(Width-size)/2;int y=(Height-size)/2;
        if(Symbol.Length>0)DrawSymbol(g,Symbol,new Rectangle(x,y,size,size),color);
        if(Subtitle.Length>0){TextRenderer.DrawText(g,Text,Font,new Rectangle(46,5,Width-54,22),color,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);TextRenderer.DrawText(g,Subtitle,Font,new Rectangle(46,28,Width-54,17),Skin.Muted,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);}
        else if(Text.Length>0){var r=ClientRectangle;if(Symbol.Length>0){r.X=38;r.Width-=44;}TextRenderer.DrawText(g,Text,Font,r,color,TextFormatFlags.VerticalCenter|TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);}
    }
    internal static void DrawSymbol(Graphics g,string symbol,Rectangle r,Color color){
        using(var pen=new Pen(color,1.6f)){
            if(symbol=="mic"){g.DrawArc(pen,r.X+5,r.Y,r.Width-10,r.Height-6,180,180);g.DrawArc(pen,r.X+5,r.Y+2,r.Width-10,r.Height-6,0,180);g.DrawLine(pen,r.X+5,r.Y+3,r.X+5,r.Bottom-7);g.DrawLine(pen,r.Right-5,r.Y+3,r.Right-5,r.Bottom-7);g.DrawArc(pen,r.X+2,r.Y+4,r.Width-4,r.Height-6,0,180);g.DrawLine(pen,r.X+r.Width/2,r.Bottom-3,r.X+r.Width/2,r.Bottom);g.DrawLine(pen,r.X+5,r.Bottom,r.Right-5,r.Bottom);}
            else if(symbol=="gear"){g.DrawEllipse(pen,r.X+3,r.Y+3,r.Width-6,r.Height-6);g.DrawEllipse(pen,r.X+7,r.Y+7,r.Width-14,r.Height-14);for(int i=0;i<8;i++){double a=i*Math.PI/4;float cx=r.X+r.Width/2f,cy=r.Y+r.Height/2f;g.DrawLine(pen,cx+(float)Math.Cos(a)*(r.Width/2-4),cy+(float)Math.Sin(a)*(r.Height/2-4),cx+(float)Math.Cos(a)*r.Width/2,cy+(float)Math.Sin(a)*r.Height/2);}}
            else if(symbol=="tools"){for(int row=0;row<2;row++)for(int col=0;col<2;col++)g.DrawRectangle(pen,r.X+col*10,r.Y+row*10,6,6);}
            else if(symbol=="close"){g.DrawLine(pen,r.X+4,r.Y+4,r.Right-4,r.Bottom-4);g.DrawLine(pen,r.Right-4,r.Y+4,r.X+4,r.Bottom-4);}
            else if(symbol=="hide"){g.DrawLine(pen,r.X+4,r.Y+r.Height/2,r.Right-4,r.Y+r.Height/2);}
            else if(symbol=="keyboard"){g.DrawRectangle(pen,r.X,r.Y+3,r.Width,r.Height-6);for(int row=0;row<2;row++)for(int col=0;col<4;col++)g.DrawLine(pen,r.X+3+col*4,r.Y+6+row*4,r.X+4+col*4,r.Y+6+row*4);}
        }
    }
}
internal abstract class PassivePopup:Form {
    protected PassivePopup(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=false;StartPosition=FormStartPosition.Manual;BackColor=Color.White;AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;DoubleBuffered=true;}
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
    protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=new IntPtr(3);return;}base.WndProc(ref m);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);QuickStyle.Border(e.Graphics,ClientRectangle);}
    protected override void OnShown(EventArgs e){base.OnShown(e);Skin.RoundWindow(this);}
}
internal sealed class QuickBar:PassivePopup {
    readonly ToolTip tips=new ToolTip{InitialDelay=500,ReshowDelay=200};
    internal event Action VoiceRequested,ToolsRequested,SettingsRequested,KeyboardRequested,Dismissed;
    internal QuickBar(){
        Text="数萌 AI · 快捷状态栏";ClientSize=new Size(256,42);
        var brand=new Label{Text="数",Font=new Font("Microsoft YaHei UI",13,FontStyle.Bold),ForeColor=Color.White,BackColor=QuickStyle.Orange,TextAlign=ContentAlignment.MiddleCenter,Location=new Point(6,6),Size=new Size(30,30)};Controls.Add(brand);
        Add("拼音","",40,56,()=>{if(KeyboardRequested!=null)KeyboardRequested();});
        Add("","mic",100,34,()=>{if(VoiceRequested!=null)VoiceRequested();}).AccessibleName="语音输入 F8";
        Add("","tools",138,34,()=>{if(ToolsRequested!=null)ToolsRequested();}).AccessibleName="工具箱";
        Add("","gear",176,34,()=>{if(SettingsRequested!=null)SettingsRequested();}).AccessibleName="输入法设置";
        Add("","hide",214,34,()=>{Hide();if(Dismissed!=null)Dismissed();}).AccessibleName="收起状态栏";
    }
    QuickButton Add(string text,string icon,int x,int width,Action action){var b=new QuickButton{Text=text,Symbol=icon,Location=new Point(x,4),Size=new Size(width,34)};b.Click+=(s,e)=>action();Controls.Add(b);tips.SetToolTip(b,icon=="mic"?"语音输入 · F8":icon=="tools"?"工具箱":icon=="gear"?"输入法设置":icon=="hide"?"收起状态栏":"拼音输入设置 · Win + 空格切换输入法");return b;}
    protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
    internal void SetRecording(bool recording){foreach(Control c in Controls){var b=c as QuickButton;if(b!=null&&b.Symbol=="mic"){b.Accent=recording;b.Invalidate();}}}
    internal void SetLanguage(int mode){foreach(Control c in Controls){var b=c as QuickButton;if(b!=null&&b.Symbol==""&&b.Text.Length>0){b.Text=mode==2?"EN":mode==1?"中":"智能";tips.SetToolTip(b,"单击切换中英文 · Ctrl + 单击恢复智能判断 · 单按 Shift 切换");}}}
}
internal sealed class QuickPanel:PassivePopup {
    internal event Action VoiceRequested,MeetingRequested,SettingsRequested,CopyRequested,ExitRequested,Dismissed,EmojiRequested,CommandsRequested,ServicesRequested,NewsRequested;
    readonly QuickButton sponsor=new QuickButton();
    internal QuickPanel(){
        Text="数萌 AI · 工具箱";ClientSize=new Size(292,586);
        var title=Skin.Label("数萌 AI 输入法",13,true);title.Location=new Point(18,16);Controls.Add(title);
        var hint=Skin.Label("让输入更轻松",9);hint.ForeColor=Skin.Muted;hint.Location=new Point(18,46);Controls.Add(hint);
        var close=new QuickButton{Symbol="close",Location=new Point(254,12),Size=new Size(26,26),AccessibleName="关闭工具箱"};close.Click+=(s,e)=>{Hide();if(Dismissed!=null)Dismissed();};Controls.Add(close);
        Row("语音输入","按 F8 开始 / 结束","mic",82,()=>{if(VoiceRequested!=null)VoiceRequested();});
        Row("实时语音翻译","边说边译 · 双语字幕 · 离线","tools",138,()=>{if(MeetingRequested!=null)MeetingRequested();});
        Row("输入法设置","麦克风与输入测试","gear",194,()=>{if(SettingsRequested!=null)SettingsRequested();});
        Row("表情与符号","Emoji · 特殊符号 · 颜文字","keyboard",250,()=>{if(EmojiRequested!=null)EmojiRequested();});
        Row("软件助手","微信找人 · 打开与切换软件","tools",306,()=>{if(CommandsRequested!=null)CommandsRequested();});
        Row("服务与赞助","词库 API · 企业服务","gear",362,()=>{if(ServicesRequested!=null)ServicesRequested();});
        Row("每日新闻与热词","中英文词库 · 每日更新 · 离线缓存","tools",418,()=>{if(NewsRequested!=null)NewsRequested();});
        sponsor.SetBounds(12,475,268,36);sponsor.Visible=false;Controls.Add(sponsor);
        sponsor.Click+=(s,e)=>{var ad=SponsorPlacement.Load();if(ad!=null)System.Diagnostics.Process.Start(ad.url);};
        var copy=new QuickButton{Text="复制最近语音",Location=new Point(12,531),Size=new Size(155,36)};copy.Click+=(s,e)=>{if(CopyRequested!=null)CopyRequested();};Controls.Add(copy);
        var exit=new QuickButton{Text="退出",Location=new Point(214,531),Size=new Size(62,36)};exit.Click+=(s,e)=>{if(ExitRequested!=null)ExitRequested();};Controls.Add(exit);
    }
    internal void RefreshSponsor(){var ad=SponsorPlacement.Load();sponsor.Visible=ad!=null;if(ad!=null)sponsor.Text="广告 · "+ad.label;}
    void Row(string title,string subtitle,string symbol,int y,Action action){
        var button=new QuickButton{Text=title,Subtitle=subtitle,Symbol=symbol,Location=new Point(12,y),Size=new Size(268,50),AccessibleName=title};
        button.Click+=(s,e)=>action();Controls.Add(button);
    }
}
internal sealed class QuickAccess:IDisposable {
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")]static extern IntPtr GetShellWindow();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,System.Text.StringBuilder text,int max);
    readonly QuickBar bar=new QuickBar();readonly QuickPanel panel=new QuickPanel();readonly Timer timer=new Timer{Interval=200};
    readonly uint processId=(uint)System.Diagnostics.Process.GetCurrentProcess().Id;
    DateTime opened;IntPtr target,openedTarget;bool disposed;
    internal event Action<IntPtr> VoiceRequested;
    internal event Action MeetingRequested,SettingsRequested,CopyRequested,ExitRequested;
    internal event Action<IntPtr> EmojiRequested;
    internal event Action CommandsRequested,ServicesRequested,NewsRequested;
    internal IntPtr Target{get{return target;}}
    internal QuickAccess(){
        bar.ToolsRequested+=()=>ShowTools(false);bar.VoiceRequested+=Voice;panel.VoiceRequested+=Voice;
        bar.KeyboardRequested+=()=>{int value=LanguageControl.Send(target,(Control.ModifierKeys&Keys.Control)!=0?4:1);if(value>=0)bar.SetLanguage(value);};bar.SettingsRequested+=Settings;panel.SettingsRequested+=Settings;
        panel.MeetingRequested+=()=>{Dismiss();if(MeetingRequested!=null)MeetingRequested();};
        panel.CopyRequested+=()=>{Dismiss();if(CopyRequested!=null)CopyRequested();};
        panel.ExitRequested+=()=>{Dismiss();if(ExitRequested!=null)ExitRequested();};
        panel.EmojiRequested+=()=>{var destination=target;Dismiss();if(EmojiRequested!=null)EmojiRequested(destination);};
        panel.CommandsRequested+=()=>{Dismiss();if(CommandsRequested!=null)CommandsRequested();};
        panel.ServicesRequested+=()=>{Dismiss();if(ServicesRequested!=null)ServicesRequested();};
        panel.NewsRequested+=()=>{Dismiss();if(NewsRequested!=null)NewsRequested();};
        bar.Dismissed+=Dismiss;panel.Dismissed+=Dismiss;
        timer.Tick+=(s,e)=>Track();timer.Start();Track();
    }
    static bool IsDesktop(IntPtr window){var name=new System.Text.StringBuilder(80);GetClassName(window,name,80);string cls=name.ToString();return window==GetShellWindow()||cls=="Shell_TrayWnd"||cls=="Shell_SecondaryTrayWnd"||cls=="Progman"||cls=="WorkerW";}
    void Track(){
        if(disposed)return;
        IntPtr foreground=GetForegroundWindow();uint pid=0;GetWindowThreadProcessId(foreground,out pid);
        if(foreground!=IntPtr.Zero&&pid!=processId&&!IsDesktop(foreground))target=foreground;
        if(bar.Visible)bar.SetLanguage(LanguageControl.Send(target,0));
        if(!bar.Visible&&!panel.Visible)return;
        bool pointerInside=(bar.Visible&&bar.Bounds.Contains(Cursor.Position))||(panel.Visible&&panel.Bounds.Contains(Cursor.Position));
        if(pointerInside)opened=DateTime.UtcNow;
        // Transient tools do not linger over unrelated applications.
        if(!pointerInside&&(DateTime.UtcNow-opened).TotalSeconds>7)Dismiss();
        else if(!pointerInside&&foreground!=openedTarget&&!IsDesktop(foreground))Dismiss();
        else if(!pointerInside&&(DateTime.UtcNow-opened).TotalMilliseconds>300&&(GetAsyncKeyState(1)<0||GetAsyncKeyState(2)<0))Dismiss();
    }
    internal void ShowBar(){if(disposed)return;if(bar.Visible){Dismiss();return;}Position();openedTarget=GetForegroundWindow();opened=DateTime.UtcNow;bar.Show();}
    internal void ShowTools(bool showBar){if(disposed)return;panel.RefreshSponsor();Position();openedTarget=GetForegroundWindow();opened=DateTime.UtcNow;if(showBar)bar.Show();panel.Show();}
    void Position(){var work=Screen.FromPoint(Cursor.Position).WorkingArea;bar.Location=new Point(Math.Max(work.Left,work.Right-bar.Width-12),Math.Max(work.Top,work.Bottom-bar.Height-10));panel.Location=new Point(Math.Max(work.Left,work.Right-panel.Width-12),Math.Max(work.Top,bar.Top-panel.Height-8));}
    void Settings(){Dismiss();if(SettingsRequested!=null)SettingsRequested();}
    void Voice(){IntPtr destination=target;Dismiss();if(VoiceRequested!=null)VoiceRequested(destination);}
    internal void Recording(bool value){bar.SetRecording(value);}
    internal void Dismiss(){bar.Hide();panel.Hide();}
    public void Dispose(){if(disposed)return;disposed=true;timer.Stop();timer.Dispose();bar.Dispose();panel.Dispose();}
}
}
