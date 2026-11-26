// SPDX-License-Identifier: GPL-3.0-or-later
// Checks supported format without starting capture or recording any sound.
using System;
using System.IO;
using System.Runtime.InteropServices;
using Winintel.Windows;
internal static class MicrophoneProbe {
    [StructLayout(LayoutKind.Sequential,Pack=2)]struct Format{public ushort tag,channels;public uint rate,bytes;public ushort align,bits,extra;}
    [DllImport("winmm.dll")]static extern uint waveInOpen(out IntPtr handle,uint id,ref Format format,IntPtr callback,UIntPtr instance,uint flags);
    static void Main(string[] args){
        string table="| 设备 | 16 kHz 单声道格式查询 |\n|---|---|\n";
        foreach(var mic in Microphone.List()){
            Format format=new Format{tag=1,channels=1,rate=16000,bytes=32000,align=2,bits=16};IntPtr device;
            uint result=waveInOpen(out device,mic.Id,ref format,IntPtr.Zero,UIntPtr.Zero,1);
            table+="| "+mic.Name+" | "+(result==0?"支持":"设备返回 "+result)+" |\n";
        }
        File.WriteAllText(args[0],table);
    }
}
