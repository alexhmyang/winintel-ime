// Copyright 2026 winintel. GPL-3.0-or-later.
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
namespace Winintel.Desktop {
internal sealed class VoiceOverlay : Form {
    readonly Label label=new Label {Dock=DockStyle.Fill,ForeColor=Color.White,Padding=new Padding(18),Font=new Font("Microsoft YaHei UI",11)};
    internal VoiceOverlay() {
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
        BackColor=Color.FromArgb(22,38,56);Size=new Size(390,110);Controls.Add(label);
    }
    protected override bool ShowWithoutActivation {get {return true;}}
    protected override CreateParams CreateParams {get {var value=base.CreateParams;value.ExStyle|=0x08000000|0x80;return value;}}
    internal void UpdateText(string text) {
        var area=Screen.PrimaryScreen.WorkingArea;Location=new Point(area.Right-Width-18,area.Bottom-Height-18);
        label.Text=text;Show();
    }
}
internal sealed class TrayApp : Form {
    readonly Runtime runtime=new Runtime();
    readonly NotifyIcon tray=new NotifyIcon();
    readonly VoiceOverlay overlay=new VoiceOverlay();
    readonly Label status=new Label();
    readonly TextBox transcript=new TextBox();
    readonly CheckBox ai=new CheckBox(),autostart=new CheckBox();
    readonly TextBox model=new TextBox();
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer {Interval=1500};
    readonly Button configure=new Button();
    Icon appIcon;
    VoiceSession voice;
    IntPtr target,focus;
    bool recording,starting,ready,closing,polling;
    bool allowExit;
    string retained="";
    readonly string[] args;
    readonly EventWaitHandle quitEvent=new EventWaitHandle(false,EventResetMode.AutoReset,@"Local\winintel-ime-quit");
    internal TrayApp(string[] arguments) {
        args=arguments;
        Text="数萌AI输入法 · winintel AI";
        Size=new Size(660,620);MinimumSize=Size;StartPosition=FormStartPosition.CenterScreen;
        Font=new Font("Microsoft YaHei UI",10);BackColor=Color.FromArgb(245,247,250);
        using(Bitmap bitmap=new Bitmap(32,32)) {
            using(Graphics g=Graphics.FromImage(bitmap)) {
                g.Clear(Color.FromArgb(28,113,216));
                using(Font f=new Font("Microsoft YaHei UI",19,FontStyle.Bold)) g.DrawString("萌",f,Brushes.White,-1,-2);
            }
            IntPtr handle=bitmap.GetHicon();appIcon=(Icon)Icon.FromHandle(handle).Clone();Native.DestroyIcon(handle);
        }
        Icon=appIcon;tray.Icon=appIcon;tray.Text="数萌AI输入法 · 启动中";tray.Visible=true;
        var menu=new ContextMenuStrip();
        menu.Items.Add("设置与识别记录",null,(s,e)=>OpenSettings());
        menu.Items.Add("语音输入：在文本框按 F8",null,(s,e)=>Balloon("先点击要输入的文本框，再按 F8 开始或结束语音。"));
        menu.Items.Add("配置系统输入法",null,async(s,e)=>await ConfigureIme());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出数萌AI",null,(s,e)=>Exit());
        tray.ContextMenuStrip=menu;tray.DoubleClick+=(s,e)=>OpenSettings();
        Panel header=new Panel {Dock=DockStyle.Top,Height=100,BackColor=Color.FromArgb(22,38,56)};
        header.Controls.Add(new Label {Text="数萌AI输入法",Location=new Point(25,17),Size=new Size(550,34),ForeColor=Color.White,Font=new Font(Font.FontFamily,21,FontStyle.Bold)});
        header.Controls.Add(new Label {Text="winintel AI  /  本地输入 · 实时语音",Location=new Point(27,62),Size=new Size(560,24),ForeColor=Color.LightSteelBlue});
        Controls.Add(header);
        status.SetBounds(26,115,590,50);status.Text="正在加载本地词库和语音模型…";Controls.Add(status);
        var hint=new Label {Text="Win + 空格切换输入法；Shift 切换中英文。\n语音：先点击目标文本框，按 F8 开始，再按 F8 结束。",Location=new Point(26,173),Size=new Size(590,52)};
        Controls.Add(hint);
        configure.Text="配置 / 修复系统输入法";configure.SetBounds(26,240,270,37);
        configure.Click+=async(s,e)=>await ConfigureIme();Controls.Add(configure);
        Button test=new Button {Text="打开输入测试",Location=new Point(314,240),Size=new Size(295,37)};
        test.Click+=(s,e)=> {if(ready) Process.Start("http://127.0.0.1:"+runtime.Port+"/");};Controls.Add(test);
        autostart.Text="登录 Windows 时启动";autostart.SetBounds(26,292,330,28);
        using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) autostart.Checked=key!=null && key.GetValue("winintel AI Input")!=null;
        autostart.CheckedChanged+=(s,e)=> {
            using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run")) {
                if(autostart.Checked) key.SetValue("winintel AI Input","\""+Application.ExecutablePath+"\"");
                else key.DeleteValue("winintel AI Input",false);
            }
        };Controls.Add(autostart);
        ai.Text="启用本地大模型排序（需安装 Ollama）";ai.Checked=runtime.AiEnabled;ai.SetBounds(26,326,460,28);Controls.Add(ai);
        model.Text=runtime.Model;model.SetBounds(26,361,270,29);Controls.Add(model);
        Button save=new Button {Text="保存并重启模型服务",Location=new Point(314,357),Size=new Size(295,35)};
        save.Click+=async(s,e)=> {
            if(voice!=null || starting) {Balloon("请先结束语音识别。");return;}
            if(!System.Text.RegularExpressions.Regex.IsMatch(model.Text,@"^[a-zA-Z0-9_.:/-]{1,120}$")) {Balloon("请输入有效的 Ollama 模型名称。");return;}
            runtime.AiEnabled=ai.Checked;runtime.Model=model.Text;runtime.Save();runtime.Dispose();ready=false;
            await StartRuntime();
        };Controls.Add(save);
        Controls.Add(new Label {Text="语音识别记录（切换窗口后保留在这里，可手动复制）",Location=new Point(26,411),Size=new Size(590,26)});
        transcript.Multiline=true;transcript.ReadOnly=true;transcript.ScrollBars=ScrollBars.Vertical;transcript.SetBounds(26,440,583,83);Controls.Add(transcript);
        LinkLabel logs=new LinkLabel {Text="查看日志",Location=new Point(26,539),AutoSize=true};
        logs.LinkClicked+=(s,e)=>Process.Start("explorer.exe",runtime.Data);Controls.Add(logs);
        Controls.Add(new Label {Text="0.1.0 测试版 · 语音在本机处理",Location=new Point(255,539),Size=new Size(353,26),ForeColor=Color.DimGray,TextAlign=ContentAlignment.MiddleRight});
        FormClosing+=(s,e)=> {if(!allowExit) {e.Cancel=true;Hide();}};
        timer.Tick+=async(s,e)=> {if(quitEvent.WaitOne(0)) {Exit();return;} await Poll();};timer.Start();
        Shown+=async(s,e)=> {
            if(Array.IndexOf(args,"/self-test")>=0) {await SelfTest();return;}
            if(!Native.RegisterHotKey(Handle,1,0x4000,0x77)) Balloon("F8 已被其他软件占用。关闭冲突软件后重新启动数萌AI。");
            if(Array.IndexOf(args,"/settings")<0) Hide();
            await StartRuntime();
        };
    }
    void OpenSettings() {Show();WindowState=FormWindowState.Normal;Activate();}
    void Balloon(string text) {tray.ShowBalloonTip(5000,"数萌AI输入法",text,ToolTipIcon.Info);}
    async Task StartRuntime() {
        if(starting) return;
        starting=true;status.Text="正在加载本地词库和语音模型…";
        try {await runtime.Start();ready=true;await Poll();}
        catch(Exception e) {runtime.Log(e.ToString());status.Text=e.Message;Balloon(e.Message);runtime.Dispose();}
        finally {starting=false;}
    }
    async Task Poll() {
        if(!ready || polling || closing) return;polling=true;
        try {
            var result=await runtime.Status();var speech=(System.Collections.Generic.Dictionary<string,object>)result["speech"];
            bool loaded=(bool)speech["ready"];
            status.Text="本地词库 "+Convert.ToInt32(result["dictionaryEntries"]).ToString("N0")+" 条   ·   语音"+(loaded?"已就绪":"正在加载")+
                "\n"+(runtime.AiEnabled?"本地大模型："+Convert.ToString(result["ai"]):"中文 / 英文联想与纠错已启用");
            tray.Text="数萌AI输入法 · "+(recording?"正在录音":loaded?"F8 语音输入":"模型加载中");
        } catch(Exception e) {status.Text="本地服务连接中断，请保存设置重启服务。";runtime.Log(e.Message);}
        finally {polling=false;}
    }
    async Task ConfigureIme() {
        configure.Enabled=false;configure.Text="正在配置，请稍候…";
        try {await runtime.Configure();Balloon("数萌输入方案已配置。按 Win + 空格选择小狼毫，Ctrl + ` 选择数萌AI输入法。");}
        catch(Exception e) {runtime.Log(e.ToString());MessageBox.Show(this,e.Message,Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);}
        finally {configure.Enabled=true;configure.Text="配置 / 修复系统输入法";}
    }
    protected override void WndProc(ref Message message) {
        if(message.Msg==0x0312 && message.WParam.ToInt32()==1) {ToggleVoice();return;}
        base.WndProc(ref message);
    }
    async void ToggleVoice() {
        if(!ready || starting) {Balloon("本地模型正在启动，请稍候。");return;}
        if(voice!=null) {
            if(recording) {recording=false;voice.Stop();overlay.UpdateText("正在完成识别…");}
            return;
        }
        starting=true;target=Native.GetForegroundWindow();focus=Native.Focus(target);
        if(target==Handle || target==overlay.Handle || focus==IntPtr.Zero) {starting=false;Balloon("请先点击目标软件的文本框。");return;}
        VoiceSession session=null;
        session=new VoiceSession((kind,text)=> {
            if(closing || !IsHandleCreated) return;
            BeginInvoke((Action)delegate {
                if(voice!=session) return;
                if(kind=="partial") overlay.UpdateText("正在聆听 · F8 结束\n"+text);
                if(kind=="final" && text.Length>0) {
                    retained=(retained+text+Environment.NewLine);if(retained.Length>12000) retained=retained.Substring(retained.Length-12000);
                    transcript.Text=retained;
                    if(!Native.TypeText(text,target,focus)) Balloon("目标文本框已改变或不允许输入，识别结果已保留在设置里。");
                    overlay.UpdateText("已识别 · F8 结束\n"+text);
                }
                if(kind=="error") {Balloon(text);overlay.Hide();session.Dispose();voice=null;recording=false;}
                if(kind=="done") {overlay.Hide();voice=null;recording=false;}
            });
        });
        voice=session;
        try {await session.Start(runtime.Port,runtime.Token);recording=true;overlay.UpdateText("正在聆听 · F8 结束\n请说话，音频只在本机处理");}
        catch(Exception e) {session.Dispose();voice=null;Balloon(e.Message);}
        finally {starting=false;}
    }
    async Task SelfTest() {
        string report=Path.Combine(runtime.Root,"desktop-self-test.txt");
        try {
            await StartRuntime();if(!ready) throw new IOException("runtime failed");
            for(int i=0;i<80;i++) {
                var result=await runtime.Status();var speech=(System.Collections.Generic.Dictionary<string,object>)result["speech"];
                if((bool)speech["ready"]) break;
                if(i==79) throw new IOException("speech did not initialize");
                await Task.Delay(250);
            }
            string recognized="";
            using(var ws=new ClientWebSocket()) using(var deadline=new CancellationTokenSource(30000)) {
                ws.Options.AddSubProtocol("winintel."+runtime.Token);
                await ws.ConnectAsync(new Uri("ws://127.0.0.1:"+runtime.Port+"/ws/voice"),deadline.Token);
                byte[] start=System.Text.Encoding.UTF8.GetBytes("{\"type\":\"start\"}");
                await ws.SendAsync(new ArraySegment<byte>(start),WebSocketMessageType.Text,true,deadline.Token);
                var started=await VoiceSession.Receive(ws,deadline.Token);
                if((string)started["type"]!="started") throw new IOException("ASR handshake failed");
                string sample=Path.Combine(runtime.Root,"models","voice","test_wavs","0.wav");
                byte[] wav=File.ReadAllBytes(sample);int dataOffset=12,dataLength=0;
                while(dataOffset+8<=wav.Length) {
                    int length=BitConverter.ToInt32(wav,dataOffset+4);
                    if(System.Text.Encoding.ASCII.GetString(wav,dataOffset,4)=="data") {dataLength=length;dataOffset+=8;break;}
                    dataOffset+=8+length+(length%2);
                }
                if(dataLength==0) throw new IOException("sample WAV has no data");
                for(int offset=0;offset<dataLength;offset+=3200) {
                    int count=Math.Min(3200,dataLength-offset);float[] pcm=new float[count/2];
                    for(int j=0;j<pcm.Length;j++) pcm[j]=BitConverter.ToInt16(wav,dataOffset+offset+j*2)/32768f;
                    byte[] bytes=new byte[pcm.Length*4];Buffer.BlockCopy(pcm,0,bytes,0,bytes.Length);
                    await ws.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Binary,true,deadline.Token);
                    await Task.Delay(40);
                }
                byte[] finish=System.Text.Encoding.UTF8.GetBytes("{\"type\":\"finish\"}");
                await ws.SendAsync(new ArraySegment<byte>(finish),WebSocketMessageType.Text,true,deadline.Token);
                for(;;) {
                    var message=await VoiceSession.Receive(ws,deadline.Token);
                    if((string)message["type"]=="error") throw new IOException((string)message["error"]);
                    if((string)message["type"]=="final") recognized+=(string)message["text"];
                    if(message.ContainsKey("finished") && (bool)message["finished"]) break;
                }
            }
            if(recognized.Length==0) throw new IOException("no speech result");
            transcript.ReadOnly=false;transcript.Text="";OpenSettings();Native.ActivateForTest(Handle);transcript.Focus();
            await Task.Delay(200);
            IntPtr window=Native.GetForegroundWindow(),field=Native.Focus(window);
            if(window!=Handle) throw new IOException("Self-test window could not acquire foreground: "+window+"; own="+Handle);
            if(!Native.TypeText("数萌AI Windows 输入测试",window,field)) throw new IOException("SendInput failed; focus="+field+"; own field="+transcript.Handle);
            await Task.Delay(200);
            if(transcript.Text!="数萌AI Windows 输入测试") throw new IOException("Unicode text did not reach focused field: "+transcript.Text);
            File.WriteAllText(report,"PASS: bundled runtime; full dictionary; ASR model; desktop WebSocket audio; native Unicode SendInput.\r\nRecognized: "+recognized);
            Program.ExitCode=0;
        } catch(Exception e) {File.WriteAllText(report,"FAIL: "+e);Program.ExitCode=1;}
        finally {Exit();}
    }
    void Exit() {
        closing=true;timer.Stop();if(voice!=null) voice.Dispose();runtime.Dispose();
        Native.UnregisterHotKey(Handle,1);tray.Visible=false;tray.Dispose();overlay.Dispose();appIcon.Dispose();
        allowExit=true;Close();
        quitEvent.Dispose();
    }
}
internal static class Program {
    internal static int ExitCode;
    [STAThread] static int Main(string[] args) {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        if(Array.IndexOf(args,"/quit")>=0) {
            try {using(var quit=EventWaitHandle.OpenExisting(@"Local\winintel-ime-quit")) quit.Set();} catch {}
            using(var wait=new Mutex(false,@"Local\winintel-ime-desktop")) {
                try {if(wait.WaitOne(15000)) wait.ReleaseMutex();} catch(AbandonedMutexException) {wait.ReleaseMutex();}
            }
            return 0;
        }
        if(Array.IndexOf(args,"/configure")>=0) {
            using(var runtime=new Runtime()) {
                try {runtime.Configure().GetAwaiter().GetResult();return 0;}
                catch(Exception e) {runtime.Log(e.ToString());MessageBox.Show(e.Message,"数萌AI输入法");return 1;}
            }
        }
        using(Mutex mutex=new Mutex(false,@"Local\winintel-ime-desktop")) {
            if(!mutex.WaitOne(0,false)) {MessageBox.Show("数萌AI输入法已在右下角运行，请双击托盘图标打开设置。");return 0;}
            try {Application.Run(new TrayApp(args));return ExitCode;} finally {mutex.ReleaseMutex();}
        }
    }
}
}
