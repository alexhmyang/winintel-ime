// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
namespace Winintel.Windows {
internal static class AppCommands {
    [StructLayout(LayoutKind.Sequential)]struct Rect{public int Left,Top,Right,Bottom;}
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int n);
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out Rect r);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]static extern bool AttachThreadInput(uint a,uint b,bool attach);
    delegate bool EnumProc(IntPtr h,IntPtr l);
    [DllImport("user32.dll")]static extern bool EnumWindows(EnumProc callback,IntPtr l);
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,System.Text.StringBuilder text,int count);
    static IntPtr WechatWindow(){
        var pids=Process.GetProcessesByName("Weixin").Concat(Process.GetProcessesByName("WeChat")).Select(p=>{using(p)return p.Id;}).ToArray();
        IntPtr best=IntPtr.Zero;long area=0;
        EnumWindows((h,l)=>{
            uint pid;GetWindowThreadProcessId(h,out pid);
            if(!pids.Contains((int)pid)||!IsWindowVisible(h))return true;
            var title=new System.Text.StringBuilder(256);GetWindowText(h,title,256);
            if(title.ToString()!="微信"&&title.ToString()!="WeChat"&&title.ToString()!="Weixin")return true;
            Rect r;if(!GetWindowRect(h,out r)||r.Left<-1000||r.Top<-1000)return true;
            long size=(long)(r.Right-r.Left)*(r.Bottom-r.Top);
            if(size>area){best=h;area=size;}return true;
        },IntPtr.Zero);return best;
    }
    internal static string Normalize(string text){return (text??"").Trim().TrimEnd('。','！','!').Replace("，"," ").Replace(","," ");}
    internal static bool SearchLabel(string text){
        text=(text??"").Trim().TrimStart('0','O','○','〇','囗','口',' ');
        return text=="搜索"||text=="搜索(S)";
    }
    internal static async Task<string> Execute(string command){
        command=Normalize(command);
        if(command.StartsWith("软件指令"))command=Normalize(command.Substring(4));
        if(command.StartsWith("微信找")){var name=command.Substring(3).Trim();if(name.Length==0||name.Length>40||name.Any(char.IsControl))return "请输入 1–40 字的联系人名称。";return await FindWechat(name);}
        string app=null;
        if(command.StartsWith("打开"))app=command.Substring(2).Trim();
        else if(command.StartsWith("切换到"))app=command.Substring(3).Trim();
        if(app==null)return "支持：微信找 张三、打开微信、打开记事本、切换到 Excel。";
        string[] processes=app=="微信"?new[]{"Weixin","WeChat"}:app=="记事本"?new[]{"notepad"}:app=="Excel"?new[]{"EXCEL"}:app=="Word"?new[]{"WINWORD"}:app=="浏览器"?new[]{"msedge","chrome"}:null;
        if(processes==null)return "当前支持微信、记事本、Excel、Word、浏览器。";
        var window=app=="微信"?WechatWindow():Find(processes);
        if(window==IntPtr.Zero&&app=="记事本"){Process.Start("notepad.exe");return "已打开记事本。";}
        if(window==IntPtr.Zero&&command.StartsWith("打开")){
            foreach(var name in processes){
                string path=null;
                foreach(var hive in new[]{Registry.CurrentUser,Registry.LocalMachine}){
                    using(var key=hive.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\"+name+".exe")){if(key!=null)path=key.GetValue("") as string;}
                    if(!string.IsNullOrEmpty(path)&&File.Exists(path.Trim('"'))){Process.Start(path.Trim('"'));return "已打开 "+app+"。";}
                }
            }
        }
        if(window==IntPtr.Zero)return "请先启动 "+app+"，再执行切换。";
        return await Activate(window)?"已切换到 "+app+"。":"未能切换，请点击软件窗口重试。";
    }
    static IntPtr Find(string[] names){foreach(var name in names)foreach(var p in Process.GetProcessesByName(name)){using(p){if(p.MainWindowHandle!=IntPtr.Zero)return p.MainWindowHandle;}}return IntPtr.Zero;}
    static async Task<bool> Activate(IntPtr h){
        uint pid;uint current=GetCurrentThreadId(),foreground=GetWindowThreadProcessId(TextInput.GetForegroundWindow(),out pid);
        bool attached=foreground!=0&&foreground!=current&&AttachThreadInput(current,foreground,true);
        try{ShowWindow(h,9);SetForegroundWindow(h);}
        finally{if(attached)AttachThreadInput(current,foreground,false);}
        await Task.Delay(150);
        if(TextInput.GetForegroundWindow()!=h){try{AutomationElement.FromHandle(h).SetFocus();}catch(InvalidOperationException){}catch(ElementNotAvailableException){}await Task.Delay(100);}
        return TextInput.GetForegroundWindow()==h;
    }
    internal static async Task<string> FindWechat(string name){
        var h=WechatWindow();if(h==IntPtr.Zero)return "请先启动并登录微信。";
        if(!await Activate(h))return "请点击微信主窗口后重试。";
        await Task.Delay(180);
        if(!TextInput.Ready(h))return "窗口已切换，请重试。";
        // Prefer exposed accessibility controls; Qt versions may only expose title-bar buttons.
        bool entered=false;
        try{
            var root=AutomationElement.FromHandle(h);
            var edits=root.FindAll(TreeScope.Descendants,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Edit));
            foreach(AutomationElement edit in edits){
                string label=edit.Current.Name.Trim();
                if(label!="搜索"&&label!="搜索联系人"&&label!="搜索联系人和群聊")continue;
                object pattern;if(edit.TryGetCurrentPattern(ValuePattern.Pattern,out pattern)&&!((ValuePattern)pattern).Current.IsReadOnly){if(!TextInput.Ready(h))return "窗口已切换，请重试。";edit.SetFocus();((ValuePattern)pattern).SetValue(name);entered=true;break;}
            }
        }catch(InvalidOperationException){}catch(ElementNotAvailableException){}
        var bounds=LocalOcr.Bounds(h);double scale=LocalOcr.Scale(h);
        var header=new Rectangle(bounds.Left,bounds.Top,Math.Min((int)(360*scale),bounds.Width),Math.Min((int)(160*scale),bounds.Height));
        if(!entered){
            var hits=await LocalOcr.ReadWindow(h,header);var search=hits.Where(x=>SearchLabel(x.Text)).ToArray();
            // WeChat 4.x can display Favorites inside the main window.
            // Only use its version-specific chat navigation when that view is identified.
            if(search.Length==0&&hits.Any(x=>x.Text=="写笔记"||x.Text=="全部收藏")&&TextInput.Ready(h)){
                string version=null;uint pid;GetWindowThreadProcessId(h,out pid);
                using(var process=Process.GetProcessById((int)pid)){try{version=process.MainModule.FileVersionInfo.FileVersion;}catch(System.ComponentModel.Win32Exception){}}
                if(version!=null&&version.StartsWith("4.")){
                    if(!TextInput.Click(h,bounds.Left+(int)(36*scale),bounds.Top+(int)(118*scale)))return "未能切换微信聊天列表，请重试。";
                    await Task.Delay(250);
                    hits=await LocalOcr.ReadWindow(h,header);search=hits.Where(x=>SearchLabel(x.Text)).ToArray();
                }
            }
            if(search.Length==0&&TextInput.Ready(h)){
                ShowWindow(h,3);await Task.Delay(200);
                if(!TextInput.Ready(h))return "窗口已切换，请重试。";
                bounds=LocalOcr.Bounds(h);
                header=new Rectangle(bounds.Left,bounds.Top,Math.Min((int)(360*scale),bounds.Width),Math.Min((int)(160*scale),bounds.Height));
                hits=await LocalOcr.ReadWindow(h,header);search=hits.Where(x=>SearchLabel(x.Text)).ToArray();
            }
            if(search.Length==0&&TextInput.Ready(h)){
                var back=hits.Where(x=>x.Text.StartsWith("〈")||x.Text.StartsWith("‹")).ToArray();
                if(back.Length==1){
                    var backBox=back[0].Bounds;
                    if(!TextInput.Click(h,backBox.Left+Math.Min(backBox.Height/4,backBox.Width/4),backBox.Top+backBox.Height/2))return "未能返回微信列表，请重试。";
                    await Task.Delay(250);
                    search=(await LocalOcr.ReadWindow(h,header)).Where(x=>SearchLabel(x.Text)).ToArray();
                }
            }
            if(search.Length!=1||!TextInput.Ready(h))return "未识别到唯一的微信搜索框，请先回到微信主界面。";
            var b=search[0].Bounds;
            if(!TextInput.Click(h,b.Left+b.Width/2,b.Top+b.Height/2))return "未能定位搜索框，请重试。";
            await Task.Delay(100);
            if(!TextInput.Insert(h,name,false))return "未能输入搜索名称，请重试。";
        }
        await Task.Delay(700);
        if(!TextInput.Ready(h))return "已搜索；窗口切换后停止操作。";
        int top=Math.Min((int)(95*scale),bounds.Height-1);
        var sidebar=new Rectangle(bounds.Left,bounds.Top+top,Math.Min((int)(360*scale),bounds.Width),Math.Min((int)(620*scale),Math.Max(1,bounds.Height-top)));
        var matches=ContactMatches(await LocalOcr.ReadWindow(h,sidebar),name);
        if(matches.Length!=1)return "已搜索“"+name+"”；"+(matches.Length>1?"存在同名结果，请点击目标联系人。":"请在微信搜索结果中选择联系人。");
        var box=matches[0].Bounds;
        return TextInput.Click(h,box.Left+box.Width/2,box.Top+box.Height/2)?"已打开“"+name+"”的匹配结果。":"已搜索，请手动选择联系人。";
    }
    internal static OcrHit[] ContactMatches(System.Collections.Generic.List<OcrHit> hits,string name){
        bool contacts=false;var result=new System.Collections.Generic.List<OcrHit>();
        foreach(var hit in hits.OrderBy(x=>x.Bounds.Top)){
            if(hit.Text=="联系人"||hit.Text=="群聊"||hit.Text=="最近联系人"){contacts=true;continue;}
            if(hit.Text=="聊天记录"||hit.Text=="公众号"||hit.Text=="文章"||hit.Text.StartsWith("搜索网络")){contacts=false;continue;}
            if(contacts&&hit.Text==name)result.Add(hit);
        }return result.ToArray();
    }
}
internal sealed class CommandPanel:Form {
    readonly TextBox command=new TextBox();
    readonly Label status=new Label();
    internal CommandPanel(){
        Text="数萌 AI · 软件助手";ClientSize=new Size(570,245);BackColor=Color.White;Font=new Font("Microsoft YaHei UI",10);StartPosition=FormStartPosition.CenterScreen;
        var title=Skin.Label("软件助手",17,true);title.SetBounds(20,18,520,34);Controls.Add(title);
        var hint=Skin.Label("微信找 张三 · 打开微信 · 打开记事本 · 切换到 Excel",10);hint.SetBounds(20,65,530,30);Controls.Add(hint);
        command.SetBounds(20,104,395,30);Controls.Add(command);
        var run=Skin.Button("执行",true);run.SetBounds(432,100,115,36);Controls.Add(run);
        status.SetBounds(20,155,527,74);status.Text="只打开、切换与查找。搜索使用本机识别。";Controls.Add(status);
        run.Click+=async(s,e)=>{run.Enabled=false;try{var result=await AppCommands.Execute(command.Text);if(!IsDisposed)status.Text=result;}catch(Exception ex){if(!IsDisposed)status.Text="操作停止："+ex.Message;}finally{if(!IsDisposed)run.Enabled=true;}};
    }
    internal void SetCommand(string text){command.Text=text;}
}
}
