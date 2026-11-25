// GPL-3.0-or-later. Stop bundled child runtimes even if the tray process crashes.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace Winintel.Desktop {
internal sealed class ChildJob : IDisposable {
    [StructLayout(LayoutKind.Sequential)] struct Basic {
        public long processTime,jobTime;public uint flags;public UIntPtr minimum,maximum;public uint active;public UIntPtr affinity;public uint priority,scheduling;
    }
    [StructLayout(LayoutKind.Sequential)] struct Io {public ulong readOps,writeOps,otherOps,readBytes,writeBytes,otherBytes;}
    [StructLayout(LayoutKind.Sequential)] struct Extended {public Basic basic;public Io io;public UIntPtr processMemory,jobMemory,peakProcess,peakJob;}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attributes,string name);
    [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job,int kind,ref Extended info,uint length);
    [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    IntPtr handle;
    internal ChildJob() {
        handle=CreateJobObject(IntPtr.Zero,null);
        Extended value=new Extended {basic=new Basic {flags=0x2000}};
        if(handle==IntPtr.Zero || !SetInformationJobObject(handle,9,ref value,(uint)Marshal.SizeOf(typeof(Extended)))) throw new InvalidOperationException("Unable to manage bundled runtime");
    }
    internal void Add(Process process) {
        if(!AssignProcessToJobObject(handle,process.Handle)) {
            try {process.Kill();} catch {}
            throw new InvalidOperationException("Unable to attach bundled runtime");
        }
    }
    public void Dispose() {if(handle!=IntPtr.Zero) {CloseHandle(handle);handle=IntPtr.Zero;}}
}
}
