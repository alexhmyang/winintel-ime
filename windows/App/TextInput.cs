// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace Winintel.Windows {
internal static class TextInput {
    [StructLayout(LayoutKind.Sequential)]struct Key{public ushort vk,scan;public uint flags,time;public UIntPtr extra;}
    [StructLayout(LayoutKind.Sequential)]struct Mouse{public int x,y;public uint data,flags,time;public UIntPtr extra;}
    [StructLayout(LayoutKind.Explicit)]struct Union{[FieldOffset(0)]public Key key;[FieldOffset(0)]public Mouse mouse;}
    [StructLayout(LayoutKind.Sequential)]struct Input{public uint type;public Union data;}
    [DllImport("user32.dll")]internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern uint SendInput(uint count,Input[] input,int size);
    [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")]static extern bool IsWindow(IntPtr window);
    [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")]static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [StructLayout(LayoutKind.Sequential)]struct Point{public int x,y;}
    [DllImport("user32.dll")]static extern bool ScreenToClient(IntPtr h,ref Point point);
    [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point point);
    [DllImport("user32.dll")]static extern IntPtr GetAncestor(IntPtr h,uint flags);
    [DllImport("user32.dll")]static extern IntPtr SendMessageTimeout(IntPtr h,uint message,UIntPtr w,IntPtr l,uint flags,uint timeout,out UIntPtr result);
    internal static bool Ready(IntPtr target){return target!=IntPtr.Zero&&IsWindow(target)&&GetForegroundWindow()==target&&GetAsyncKeyState(17)>=0&&GetAsyncKeyState(18)>=0&&GetAsyncKeyState(16)>=0&&GetAsyncKeyState(91)>=0&&GetAsyncKeyState(92)>=0;}
    internal static bool Insert(IntPtr target,string text,bool flush=true,int maxLength=4096){
        if(!Ready(target)||string.IsNullOrEmpty(text)||text.Length>maxLength)return false;
        if(flush&&LanguageControl.SendMessage(target,55,0)==-2)return false;
        if(!Ready(target))return false;
        var keys=new List<Input>();foreach(char ch in text){keys.Add(new Input{type=1,data=new Union{key=new Key{scan=ch,flags=4}}});keys.Add(new Input{type=1,data=new Union{key=new Key{scan=ch,flags=6}}});}
        return SendInput((uint)keys.Count,keys.ToArray(),Marshal.SizeOf(typeof(Input)))==keys.Count;
    }
    internal static bool Click(IntPtr target,int x,int y){
        if(!Ready(target))return false;
        var previous=SetThreadDpiAwarenessContext(new IntPtr(-4));
        try{
            if(!SetCursorPos(x,y)||!Ready(target))return false;
            if(GetAncestor(WindowFromPoint(new Point{x=x,y=y}),2)!=target)return false;
            var events=new[]{new Input{type=0,data=new Union{mouse=new Mouse{flags=2}}},new Input{type=0,data=new Union{mouse=new Mouse{flags=4}}}};
            return SendInput(2,events,Marshal.SizeOf(typeof(Input)))==2;
        }finally{if(previous!=IntPtr.Zero)SetThreadDpiAwarenessContext(previous);}
    }
    internal static bool ClickWindow(IntPtr target,int x,int y){
        if(!Ready(target))return false;
        var previous=SetThreadDpiAwarenessContext(new IntPtr(-4));
        try{
            var point=new Point{x=x,y=y};if(!ScreenToClient(target,ref point)||point.x<0||point.y<0||point.x>32767||point.y>32767)return false;
            var position=new IntPtr((point.y<<16)|(point.x&65535));UIntPtr result;
            if(!Ready(target))return false;
            if(SendMessageTimeout(target,0x201,new UIntPtr(1),position,2,200,out result)==IntPtr.Zero)return false;
            return SendMessageTimeout(target,0x202,UIntPtr.Zero,position,2,200,out result)!=IntPtr.Zero;
        }finally{if(previous!=IntPtr.Zero)SetThreadDpiAwarenessContext(previous);}
    }
}
}
