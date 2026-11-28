// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Runtime.InteropServices;
namespace Winintel.Windows {
internal static class LanguageControl {
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [StructLayout(LayoutKind.Sequential)]struct GuiThreadInfo {
        internal uint size,flags;internal IntPtr active,focus,capture,menu,movesize,caret;
        internal int left,top,right,bottom;
    }
    [DllImport("user32.dll")]static extern bool GetGUIThreadInfo(uint thread,ref GuiThreadInfo info);
    [DllImport("user32.dll")]static extern IntPtr SendMessageTimeout(IntPtr window,uint message,UIntPtr w,IntPtr l,uint flags,uint timeout,out UIntPtr result);
    internal static int Send(IntPtr target,int command){
        return SendMessage(target,54,command);
    }
    internal static int SendMessage(IntPtr target,uint message,int command){
        uint targetPid;uint targetThread=GetWindowThreadProcessId(target,out targetPid);
        if(targetThread==0)return -1;
        var info=new GuiThreadInfo{size=(uint)Marshal.SizeOf(typeof(GuiThreadInfo))};
        // The editor can belong to a different thread/process than its outer window.
        if(GetGUIThreadInfo(targetThread,ref info)&&info.focus!=IntPtr.Zero&&info.focus!=target){
            int focused=SendToWindow(info.focus,message,command);
            if(focused!=-1)return focused;
        }
        return SendToWindow(target,message,command);
    }
    static int SendToWindow(IntPtr target,uint message,int command){
        uint pid;uint thread=GetWindowThreadProcessId(target,out pid);IntPtr window=IntPtr.Zero,fallback=IntPtr.Zero;int matches=0;
        if(pid==0)return -1;
        while((window=FindWindowEx(IntPtr.Zero,window,"winintel.Language.054",null))!=IntPtr.Zero){
            uint owner;uint inputThread=GetWindowThreadProcessId(window,out owner);if(owner!=pid)continue;
            fallback=window;matches++;if(inputThread!=thread)continue;
            UIntPtr result;if(SendMessageTimeout(window,0x8000+message,(UIntPtr)(uint)command,IntPtr.Zero,2,100,out result)!=IntPtr.Zero)return unchecked((int)result.ToUInt64());
        }
        if(matches==1){UIntPtr result;if(SendMessageTimeout(fallback,0x8000+message,(UIntPtr)(uint)command,IntPtr.Zero,2,100,out result)!=IntPtr.Zero)return unchecked((int)result.ToUInt64());}
        return -1;
    }
}
}
