// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
// Project: winintel AI Input Method - https://github.com/alexhmyang/winintel-ime
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Winintel.Desktop;
namespace Winintel.Windows {
internal sealed class Controller:ApplicationContext {
    [StructLayout(LayoutKind.Sequential)]struct Key {public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
    [StructLayout(LayoutKind.Explicit)]struct Union{[FieldOffset(0)]public Key key;[FieldOffset(0)]public Mouse mouse;}
    [StructLayout(LayoutKind.Sequential)]struct Mouse{public int x,y;public uint data,flags,time;public UIntPtr extra;}
    [StructLayout(LayoutKind.Sequential)]struct Input {public uint type;public Union data;}
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")]static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")]static extern uint SendInput(uint count,Input[] input,int size);
    [DllImport("user32.dll")]static extern bool RegisterHotKey(IntPtr h,int id,uint mods,uint key);
    [DllImport("user32.dll")]static extern bool UnregisterHotKey(IntPtr h,int id);
    sealed class Hotkey:NativeWindow,IDisposable {
        readonly Action action;internal Hotkey(Action a){action=a;CreateHandle(new CreateParams{Caption="winintel.VoiceControl"});if(!RegisterHotKey(Handle,8,0x4000,0x77))throw new IOException("F8 已被其他程序占用");}
        protected override void WndProc(ref Message m){if(m.Msg==0x312&&m.WParam.ToInt32()==8)action();base.WndProc(ref m);}
        public void Dispose(){UnregisterHotKey(Handle,8);DestroyHandle();}
    }
    readonly ShiftCompatibility shiftCompatibility=new ShiftCompatibility();
    readonly Settings settings=Settings.Load();readonly Panel panel;readonly VoicePreview preview=new VoicePreview();readonly NotifyIcon tray;readonly QuickAccess quick=new QuickAccess();
    readonly ChildJob job=new ChildJob();readonly EventWaitHandle showEvent=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\winintel-ime-show03");readonly RegisteredWaitHandle showWait;Process host;readonly Hotkey hotkey;VoiceSession voice;IntPtr target;bool starting,stopping;
    readonly VoiceDraft draft=new VoiceDraft();readonly HashSet<long> completed=new HashSet<long>();string partial="",lastText="",sessionError="";long partialSegment;
    readonly CancellationTokenSource shutdown=new CancellationTokenSource();bool exiting;int sessionGeneration;
    readonly Translation translator=new Translation();readonly Meeting meeting=new Meeting();readonly MeetingWindow meetingWindow;
    readonly EmojiPalette emoji=new EmojiPalette();CommandPanel commands;
    readonly DailyContent daily=new DailyContent();DailyPanel dailyPanel;
    readonly Dictionary<long,double[]> segmentTimes=new Dictionary<long,double[]>();double meetingOffset;
    internal Controller(bool show,bool quickTools=false,bool news=false){
        panel=new Panel(settings,()=>{});panel.CreateControl();var force=panel.Handle;
        meetingWindow=new MeetingWindow(meeting,translator,Dispatch);
        meetingWindow.RecordRequested+=Toggle;
        panel.MeetingRequested+=ShowMeeting;
        panel.ServicesRequested+=ShowServices;panel.CommandsRequested+=()=>ShowCommands("");
        preview.Edited+=value=>{draft.Edit(value);lastText=draft.Text;};
        preview.ClearRequested+=()=>{draft.Clear();Render();preview.UpdateText("草稿已清空 · 可撤销");};
        preview.UndoRequested+=()=>{draft.Undo();Render();preview.UpdateText("已撤销");};
        preview.ConfirmRequested+=Confirm;
        preview.StopRequested+=Toggle;
        quick.VoiceRequested+=window=>{if(window==IntPtr.Zero||!IsWindow(window)){ShowPanel();panel.Status("请先点击需要输入的窗口，再按 F8 或点击状态栏语音。");return;}target=window;Toggle(true);};
        quick.SettingsRequested+=ShowPanel;quick.MeetingRequested+=ShowMeeting;
        quick.ServicesRequested+=ShowServices;quick.CommandsRequested+=()=>ShowCommands("");
        quick.NewsRequested+=ShowNews;
        quick.EmojiRequested+=window=>emoji.Open(window);
        emoji.Selected+=(window,text)=>{if(TextInput.Insert(window,text))emoji.Hide();else panel.Status("未能输入表情，请重新点击目标输入框。");};
        quick.CopyRequested+=()=>{if(lastText.Length>0)Clipboard.SetText(lastText);};quick.ExitRequested+=()=>ExitThread();
        showWait=ThreadPool.RegisterWaitForSingleObject(showEvent,(s,t)=>Dispatch(ShowPanel),null,Timeout.Infinite,false);
        var icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);panel.Icon=icon;preview.Icon=icon;tray=new NotifyIcon{Icon=icon,Text="数萌 AI 输入法 · F8 语音",Visible=true};
        tray.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left)quick.ShowBar();else if(e.Button==MouseButtons.Right)ShowPanel();};
        try{hotkey=new Hotkey(()=>Toggle());}catch(Exception e){panel.Status(e.Message);}
        string root=AppDomain.CurrentDomain.BaseDirectory;
        try{host=Process.Start(new ProcessStartInfo(Path.Combine(root,"runtime","node.exe"),"\""+Path.Combine(root,"standalone","voice-host.mjs")+"\""){UseShellExecute=false,CreateNoWindow=true,WorkingDirectory=root});job.Add(host);}catch(Exception e){panel.Status("语音服务启动失败："+e.Message);}
        if(show)ShowPanel();
        if(quickTools)quick.ShowTools(true);
        if(news)ShowNews();
        daily.Start();
    }
    void ShowPanel(){if(exiting||panel.IsDisposed||panel.Disposing)return;quick.Dismiss();panel.Show();panel.Activate();}
    void ShowMeeting(){if(exiting||meetingWindow.IsDisposed)return;quick.Dismiss();meetingWindow.Show();meetingWindow.Activate();}
    void ShowServices(){if(exiting)return;quick.Dismiss();using(var service=new ServicePanel())service.ShowDialog(panel);}
    void ShowNews(){if(exiting)return;quick.Dismiss();if(dailyPanel==null||dailyPanel.IsDisposed)dailyPanel=new DailyPanel(daily);dailyPanel.Show();dailyPanel.Activate();}
    void ShowCommands(string text){if(exiting)return;quick.Dismiss();if(commands==null||commands.IsDisposed)commands=new CommandPanel();commands.SetCommand(text);commands.Show();commands.Activate();}
    void Dispatch(Action a){if(exiting||panel.IsDisposed||panel.Disposing)return;try{panel.BeginInvoke((Action)(()=>{if(!exiting&&!panel.IsDisposed&&!panel.Disposing)a();}));}catch(InvalidOperationException){}}
    void Toggle(){Toggle(false);}
    async void Toggle(bool preserveTarget){
        if(exiting||starting||stopping)return;
        if(voice!=null){stopping=true;preview.UpdateText("正在停止录音并进行离线整句复核…");voice.Stop();return;}
        int generation=++sessionGeneration;IntPtr foreground=GetForegroundWindow();if(!preserveTarget&&foreground!=preview.Handle&&foreground!=IntPtr.Zero)target=foreground;
        starting=true;completed.Clear();segmentTimes.Clear();meetingOffset=meeting.Duration;partial="";partialSegment=0;sessionError="";meetingWindow.Preview("");preview.BeginSession(target);Render();preview.UpdateText("正在准备语音输入…");
        var session=new VoiceSession((kind,text,segment)=>Dispatch(()=>Result(generation,kind,text,segment)),
            (segment,start,end)=>Dispatch(()=>{if(generation==sessionGeneration)segmentTimes[segment]=new[]{start,end};}));voice=session;
        try{
            Dictionary<string,object> connection=null;
            for(int i=0;i<50;i++){shutdown.Token.ThrowIfCancellationRequested();try{connection=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(Settings.Folder,"voice-session.json")));if(connection.ContainsKey("ready")&&(bool)connection["ready"]&&host!=null&&Convert.ToInt32(connection["pid"])==host.Id)break;}catch{}connection=null;await Task.Delay(100,shutdown.Token);}
            if(connection==null)throw new IOException("离线模型未就绪，请稍后按 F8 重试");
            await session.Start(Convert.ToInt32(connection["port"]),(string)connection["token"],settings.Microphone);
            if(exiting||generation!=sessionGeneration){session.Dispose();return;}
            preview.UpdateText("正在聆听（本机离线） · 再按 F8 结束");
        }catch(Exception e){session.Dispose();if(!exiting&&generation==sessionGeneration){voice=null;Render();preview.UpdateText(e.Message);panel.Status(e.Message);}}
        finally{if(!exiting&&generation==sessionGeneration){starting=false;Render();}}
    }
    void Render(){lastText=draft.Text;quick.Recording(voice!=null||starting||stopping);meetingWindow.Recording(voice!=null||starting||stopping);preview.Draft(draft.Text+(partial.Length>0?"\r\n"+partial:""),voice!=null||starting||stopping);}
    void Confirm(){
        if(exiting||voice!=null||starting||stopping)return;
        string result=draft.Text;if(result.Length==0)return;
        if(AppCommands.Normalize(result).StartsWith("软件指令")){ShowCommands(result);preview.EndSession();return;}
        if(!IsWindow(target)){preview.UpdateText("目标窗口已关闭，请复制草稿到需要输入的位置");return;}
        SetForegroundWindow(target);
        if(GetForegroundWindow()!=target){preview.UpdateText("未能切换回输入窗口，请使用复制按钮");return;}
        if(TextInput.Insert(target,result,true,int.MaxValue)){lastText=result;draft.Clear();preview.Draft("",false);preview.EndSession();}
        else preview.UpdateText("未完整输入，草稿已保留，请检查目标窗口");
    }
    void Result(int generation,string kind,string text,long segment){
        if(exiting||generation!=sessionGeneration)return;
        if(kind=="partial"){if(!completed.Contains(segment)){partial=text;partialSegment=segment;meetingWindow.Preview(text);Render();}return;}
        if(kind=="final"){
            if(!completed.Add(segment))return;
            string status=draft.Accept(text);
            double[] times;
            if(!string.IsNullOrWhiteSpace(text)&&status.Length==0&&segmentTimes.TryGetValue(segment,out times))
                meetingWindow.Add(meeting.Add(meetingOffset+times[0],meetingOffset+times[1],text));
            if(partialSegment==segment){partial="";meetingWindow.Preview("");}Render();preview.UpdateText(status.Length>0?status:"正在聆听 · 说“输入法指令，清空”可清空草稿");return;
        }
        if(kind=="error"||kind=="warning"){sessionError=text;panel.Status(text);preview.UpdateText(text);return;}
        if(kind!="done")return;
        voice=null;starting=false;stopping=false;partial="";meetingWindow.Preview("");Render();
        preview.UpdateText(sessionError.Length>0?sessionError:(draft.Text.Length>0?"录音已停止 · 可编辑、清空、撤销，确认后上屏":"草稿为空 · 按 F8 继续录音"));
    }
    protected override void ExitThreadCore(){if(exiting)return;exiting=true;shiftCompatibility.Dispose();++sessionGeneration;shutdown.Cancel();if(voice!=null)voice.Dispose();if(hotkey!=null)hotkey.Dispose();showWait.Unregister(null);showEvent.Dispose();translator.Dispose();meetingWindow.Dispose();daily.Dispose();if(dailyPanel!=null)dailyPanel.Dispose();job.Dispose();quick.Dispose();emoji.Dispose();if(commands!=null)commands.Dispose();tray.Visible=false;tray.Dispose();preview.Dispose();panel.Dispose();base.ExitThreadCore();}
}
internal static class Program{
    [STAThread]static void Main(string[] args){
        bool created;using(var mutex=new Mutex(true,"Local\\winintel-ime-windows03",out created)){
            if(!created){try{using(var signal=EventWaitHandle.OpenExisting("Local\\winintel-ime-show03"))signal.Set();}catch{}return;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Controller(Array.IndexOf(args,"--show")>=0,Array.IndexOf(args,"--quick")>=0,Array.IndexOf(args,"--news")>=0));
        }
    }
}
}
