// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace Winintel.Windows {
internal sealed class Microphone {
    public uint Id;public string Name;
    public override string ToString(){return Name;}
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct Caps {public ushort mid,pid;public uint version;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string name;public uint formats;public ushort channels,reserved;}
    [DllImport("winmm.dll")]static extern uint waveInGetNumDevs();
    [DllImport("winmm.dll",CharSet=CharSet.Unicode)]static extern uint waveInGetDevCapsW(UIntPtr id,out Caps caps,uint size);
    internal static List<Microphone> List(){var list=new List<Microphone>{new Microphone{Id=0xffffffff,Name="Windows 默认麦克风"}};for(uint i=0;i<waveInGetNumDevs();i++){Caps caps;if(waveInGetDevCapsW(new UIntPtr(i),out caps,(uint)Marshal.SizeOf(typeof(Caps)))==0)list.Add(new Microphone{Id=i,Name=caps.name});}return list;}
}
}
