// SPDX-License-Identifier: GPL-3.0-or-later
// Sends actual keyboard events into this probe's own fresh TSF textbox.
using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
class RealCompositionProbe:Form {
    [ComImport,Guid("71c6e74c-0f28-11d8-a82a-00065b84435c"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ProfileMgr {[PreserveSig]int ActivateProfile(uint type,ushort language,ref Guid cls,ref Guid profile,IntPtr hkl,uint flags);}
    [StructLayout(LayoutKind.Sequential)]struct K {public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
    [StructLayout(LayoutKind.Sequential)]struct M {public int x,y;public uint data,flags,time;public UIntPtr extra;}
    [StructLayout(LayoutKind.Explicit)]struct U {[FieldOffset(0)]public K k;[FieldOffset(0)]public M m;}
    [StructLayout(LayoutKind.Sequential)]struct I {public uint type;public U u;}
    [DllImport("user32.dll")]static extern uint SendInput(uint count,I[] inputs,int size);
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]static extern bool AttachThreadInput(uint source,uint target,bool attach);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string name);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
    TextBox box=new TextBox{Multiline=true,Dock=DockStyle.Fill};
    string output,expectedRoot;string log="| Test | Result | Actual |\n|---|---|---|\n";int errors;
    readonly HashSet<ushort> held=new HashSet<ushort>();
    RealCompositionProbe(string path,string root){output=path;expectedRoot=root;Text="winintel AI long composition verification";Width=650;Height=220;TopMost=true;Controls.Add(box);Shown+=async(s,e)=>await Run();}
    void Check(bool ok,string name,string actual){log+="| "+name+" | "+(ok?"PASS":"FAIL")+" | "+actual.Replace("|","/").Replace("\r","").Replace("\n"," ")+" |\n";if(!ok)errors++;}
    async Task Key(ushort vk,bool up){if(!up&&GetForegroundWindow()!=Handle){uint owner;GetWindowThreadProcessId(GetForegroundWindow(),out owner);throw new Exception("Probe lost focus to process "+owner+"; input cancelled");}var input=new I{type=1,u=new U{k=new K{vk=vk,flags=up?2u:0u}}};if(SendInput(1,new[]{input},Marshal.SizeOf(typeof(I)))!=1)throw new Exception("SendInput failed");if(up)held.Remove(vk);else held.Add(vk);await Task.Delay(35);}
    async Task Tap(ushort vk){await Key(vk,false);await Key(vk,true);}
    async Task Ctrl(ushort vk,bool shift=false){await Key(17,false);if(shift)await Key(16,false);await Tap(vk);if(shift)await Key(16,true);await Key(17,true);}
    async Task Type(string value){foreach(char c in value)await Tap((ushort)char.ToUpperInvariant(c));}
    IntPtr Window(string cls){IntPtr h=IntPtr.Zero;uint pid;while((h=FindWindowEx(IntPtr.Zero,h,cls,null))!=IntPtr.Zero){GetWindowThreadProcessId(h,out pid);if(pid==(uint)Process.GetCurrentProcess().Id)return h;}return IntPtr.Zero;}
    void Chinese(){var bridge=Window("winintel.Language.054");if(bridge==IntPtr.Zero)throw new Exception("TSF bridge missing");SendMessage(bridge,0x8000+54,(IntPtr)3,IntPtr.Zero);}
    async Task Reset(){await Tap(27);Chinese();box.Clear();await Task.Delay(100);}
    async Task Raw(string expected,string label){await Tap(16);Check(box.Text==expected,label,box.Text);await Reset();}
    async Task Run(){try{
        Activate();SetForegroundWindow(Handle);box.Focus();await Task.Delay(150);
        var cls=new Guid("1B6F3BD2-51A0-4935-B2F9-31CAF0345E71");var profile=new Guid("DF4864AE-309A-4FA1-9A71-37FAC4328923");
        var manager=(ProfileMgr)Activator.CreateInstance(System.Type.GetTypeFromCLSID(new Guid("33C53A50-F456-4884-B049-85FD643ECFED")));
        int hr=manager.ActivateProfile(1,0x804,ref cls,ref profile,IntPtr.Zero,0);Marshal.ReleaseComObject(manager);Check(hr==0,"System TSF activation","0x"+hr.ToString("X8"));await Task.Delay(150);
        uint owner;uint foreground=GetWindowThreadProcessId(GetForegroundWindow(),out owner),current=GetCurrentThreadId();bool linked=foreground!=current&&AttachThreadInput(current,foreground,true);
        try{Activate();SetForegroundWindow(Handle);box.Focus();}finally{if(linked)AttachThreadInput(current,foreground,false);}
        await Task.Delay(100);await Reset();
        bool currentDll=false;
        foreach(ProcessModule module in Process.GetCurrentProcess().Modules)if(module.ModuleName=="winintel-tsf.dll"){
            currentDll=Path.GetFullPath(module.FileName)==Path.GetFullPath(Path.Combine(expectedRoot,"x64","winintel-tsf.dll"));
            Check(currentDll,"Fresh application loads installed 0.5.5 DLL",module.FileName);
        }
        if(!currentDll)throw new Exception("Installed native component was not loaded");
        await Type("nihxo");await Tap(37);await Tap(37);await Tap(46);await Type("a");await Raw("nihao","Real keyboard repairs exact pinyin buffer");
        await Type("nihxo");await Tap(37);await Tap(37);await Tap(46);await Type("a");await Tap(32);
        Check(box.Text=="你好","Repair middle and recompute Chinese candidate",box.Text);await Reset();
        await Type("nihao");await Tap(36);await Tap(8);await Type("a");await Tap(35);await Tap(46);await Type("b");await Raw("anihaob","Home End and deletion boundaries");
        await Type("nihao");await Tap(37);await Tap(8);await Ctrl(90);await Raw("nihao","Ctrl Z restores middle deletion");
        await Type("nihao");await Tap(37);await Tap(8);await Ctrl(90);await Ctrl(89);await Raw("niho","Ctrl Y redoes deletion");
        await Type("nihao");await Tap(37);await Tap(8);await Ctrl(90);await Ctrl(90,true);await Raw("niho","Ctrl Shift Z redoes without language toggle");
        await Type("nihao");await Tap(37);await Tap(8);await Ctrl(90);await Type("b");await Ctrl(89);await Raw("nihabo","New edit invalidates redo");
        await Type("nihao");var candidate=Window("winintel.Candidates.052");
        SendMessage(candidate,0x202,IntPtr.Zero,(IntPtr)((18<<16)|13));await Type("a");await Raw("anihao","Mouse positions composition caret without focus theft");
        await Type(new string('a',257));await Tap(36);for(int i=0;i<100;i++)await Tap(39);await Tap(46);await Type("b");
        await Raw(new string('a',100)+"b"+new string('a',155),"256 letters and middle correction preserve suffix");
        string sentence="";for(int i=0;i<20;i++)sentence+="nihao";await Type(sentence);await Tap(32);
        string chinese="";for(int i=0;i<20;i++)chinese+="你好";Check(box.Text==chinese,"100-letter sentence commits completely",box.Text);await Reset();
        await Tap(16);await Type("hello");await Tap(32);Check(box.Text=="hello ","Shift still switches to English",box.Text);
        await Tap(16);await Type("nihao");await Tap(32);Check(box.Text=="hello 你好","Shift switches back to Chinese",box.Text);await Reset();
        await Type("hello");await Tap(0xBE);await Tap(8);SendMessage(Window("winintel.Language.054"),0x8000+54,(IntPtr)4,IntPtr.Zero);
        foreach(ProcessModule module in Process.GetCurrentProcess().Modules)if(module.ModuleName=="winintel-tsf.dll")
            Check(Path.GetFullPath(module.FileName)==Path.GetFullPath(Path.Combine(expectedRoot,"x64","winintel-tsf.dll")),"Fresh application loads installed 0.5.5 DLL",module.FileName);
    }catch(Exception e){Check(false,"Exception",e.Message);}finally{foreach(ushort vk in held){var release=new I{type=1,u=new U{k=new K{vk=vk,flags=2}}};SendInput(1,new[]{release},Marshal.SizeOf(typeof(I)));}held.Clear();var bridge=Window("winintel.Language.054");if(bridge!=IntPtr.Zero)SendMessage(bridge,0x8000+54,(IntPtr)4,IntPtr.Zero);File.WriteAllText(output,log);Environment.ExitCode=errors==0?0:1;Close();}}
    [STAThread]static void Main(string[] args){Application.EnableVisualStyles();Application.Run(new RealCompositionProbe(args[0],args[1]));}
}
