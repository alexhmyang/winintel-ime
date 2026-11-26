// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Runtime.InteropServices;
namespace Winintel.Windows {
internal static class LanguageControl {
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string title);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
    [DllImport("user32.dll")]static extern IntPtr SendMessageTimeout(IntPtr window,uint message,UIntPtr w,IntPtr l,uint flags,uint timeout,out UIntPtr result);
    internal static int Send(IntPtr target,int command){
        uint pid;uint thread=GetWindowThreadProcessId(target,out pid);IntPtr window=IntPtr.Zero;
        while((window=FindWindowEx(IntPtr.Zero,window,"winintel.Language.054",null))!=IntPtr.Zero){
            uint owner;uint inputThread=GetWindowThreadProcessId(window,out owner);if(owner!=pid||inputThread!=thread)continue;
            UIntPtr result;if(SendMessageTimeout(window,0x8000+54,(UIntPtr)(uint)command,IntPtr.Zero,2,100,out result)!=IntPtr.Zero)return unchecked((int)result.ToUInt64());
        }
        return -1;
    }
}
}
