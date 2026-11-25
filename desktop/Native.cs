// GPL-3.0-or-later
using System;
using System.Runtime.InteropServices;
namespace Winintel.Desktop {
internal static class Native {
    [StructLayout(LayoutKind.Sequential)] struct Keyboard { public ushort vk, scan; public uint flags, time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] struct Mouse { public int x,y; public uint data,flags,time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Explicit)] struct Union { [FieldOffset(0)] public Keyboard keyboard; [FieldOffset(0)] public Mouse mouse; }
    [StructLayout(LayoutKind.Sequential)] struct Input { public uint type; public Union value; }
    [StructLayout(LayoutKind.Sequential)] struct GuiInfo { public uint size, flags; public IntPtr active,focus,capture,menu,move,caret; public int left,top,right,bottom; }
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] static extern bool GetGUIThreadInfo(uint thread, ref GuiInfo info);
    [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window,int id);
    [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count, Input[] inputs,int size);
    [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr icon);
    [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] static extern bool AttachThreadInput(uint first,uint second,bool attach);
    [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr window);
    internal static void ActivateForTest(IntPtr window) {
        uint process;uint other=GetWindowThreadProcessId(GetForegroundWindow(),out process),current=GetCurrentThreadId();
        bool attached=other!=current && AttachThreadInput(current,other,true);
        try {BringWindowToTop(window);SetForegroundWindow(window);}
        finally {if(attached) AttachThreadInput(current,other,false);}
    }
    internal static IntPtr Focus(IntPtr window) {
        uint process; uint thread = GetWindowThreadProcessId(window,out process);
        GuiInfo info = new GuiInfo { size=(uint)Marshal.SizeOf(typeof(GuiInfo)) };
        return GetGUIThreadInfo(thread,ref info) ? info.focus : IntPtr.Zero;
    }
    internal static bool TypeText(string text,IntPtr window,IntPtr focus) {
        if (GetForegroundWindow()!=window || focus==IntPtr.Zero || Focus(window)!=focus) return false;
        // Bound each batch: Windows limits SendInput arrays and some editors drop huge bursts.
        for(int offset=0; offset<text.Length; offset+=64) {
            if(GetForegroundWindow()!=window || Focus(window)!=focus) return false;
            int length=Math.Min(64,text.Length-offset); Input[] inputs=new Input[length*2];
            for(int i=0;i<length;i++) {
                inputs[i*2]=new Input {type=1,value=new Union {keyboard=new Keyboard {scan=text[offset+i],flags=4}}};
                inputs[i*2+1]=new Input {type=1,value=new Union {keyboard=new Keyboard {scan=text[offset+i],flags=6}}};
            }
            if(SendInput((uint)inputs.Length,inputs,Marshal.SizeOf(typeof(Input)))!=inputs.Length) return false;
        }
        return true;
    }
}
}
