// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Winintel.Windows {
// Pass through every key. Store only whether Shift was used with another key.
internal sealed class ShiftCompatibility:IDisposable {
    delegate IntPtr Hook(int code,IntPtr message,IntPtr data);
    [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,Hook fn,IntPtr module,uint thread);
    [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr message,IntPtr data);
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string name);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr OpenFileMapping(uint access,bool inherit,string name);
    [DllImport("kernel32.dll")]static extern IntPtr MapViewOfFile(IntPtr map,uint access,uint high,uint low,UIntPtr size);
    [DllImport("kernel32.dll")]static extern bool UnmapViewOfFile(IntPtr address);
    [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
    readonly Hook callback;readonly Timer timer;IntPtr hook,map,address,target;
    bool down,used,pending;int mode,due;
    readonly Func<IntPtr> foreground;readonly Func<int> readMode;readonly Func<IntPtr,int,int> send;
    internal ShiftCompatibility():this(GetForegroundWindow,null,LanguageControl.Send,true){}
    internal ShiftCompatibility(Func<IntPtr> fg,Func<int> read,Func<IntPtr,int,int> command,bool install){
        foreground=fg;readMode=read??ReadMode;send=command;
        callback=Callback;timer=new Timer{Interval=15};timer.Tick+=(s,e)=>Tick(Environment.TickCount);timer.Start();
        if(install)hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);
    }
    int ReadMode(){
        if(address==IntPtr.Zero){map=OpenFileMapping(4,false,"Local\\winintel-ime-language054");if(map!=IntPtr.Zero){address=MapViewOfFile(map,4,0,0,(UIntPtr)4);if(address==IntPtr.Zero){CloseHandle(map);map=IntPtr.Zero;}}}
        return address==IntPtr.Zero?-1:Marshal.ReadInt32(address);
    }
    internal void Observe(uint key,bool up,bool modifiers,int now){
        bool shift=key==16||key==160||key==161;
        if(!shift){if(down)used=true;return;}
        if(!up){if(!down){pending=false;down=true;used=modifiers;target=foreground();mode=readMode();}else if(modifiers)used=true;return;}
        if(!down)return;down=false;
        if(!used&&!modifiers&&mode>=0&&foreground()==target){pending=true;due=unchecked(now+40);}
    }
    internal void Tick(int now){
        if(!pending||unchecked(now-due)<0)return;pending=false;
        if(foreground()==target&&readMode()==mode)send(target,1);
    }
    IntPtr Callback(int code,IntPtr message,IntPtr data){
        if(code>=0){int m=message.ToInt32();if(m==0x100||m==0x104||m==0x101||m==0x105){
            uint key=unchecked((uint)Marshal.ReadInt32(data));
            bool modifiers=GetAsyncKeyState(17)<0||GetAsyncKeyState(18)<0||GetAsyncKeyState(91)<0||GetAsyncKeyState(92)<0;
            Observe(key,m==0x101||m==0x105,modifiers,Environment.TickCount);
        }}return CallNextHookEx(hook,code,message,data);
    }
    public void Dispose(){timer.Stop();timer.Dispose();if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}if(address!=IntPtr.Zero)UnmapViewOfFile(address);if(map!=IntPtr.Zero)CloseHandle(map);}
}
}
