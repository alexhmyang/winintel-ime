// SPDX-License-Identifier: GPL-3.0-or-later
// Calls this build's TSF service directly; writes only to its own test textbox.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
internal sealed class PagingProbe:Form {
    [ComImport,Guid("71c6e74c-0f28-11d8-a82a-00065b84435c"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ProfileMgr{[PreserveSig]int ActivateProfile(uint type,ushort language,ref Guid cls,ref Guid profile,IntPtr hkl,uint flags);}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll")]static extern IntPtr GetProcAddress(IntPtr h,string name);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Key(uint value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Attach();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void Detach();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int FocusStore(IntPtr window);
    readonly TextBox box=new TextBox{Multiline=true,Dock=DockStyle.Fill};
    readonly string root,fixture;Key send;Detach detach;int failures;string log="";
    PagingProbe(string r,string f){root=r;fixture=f;Text="数萌候选分页验证";Width=640;Height=180;Controls.Add(box);Shown+=async(s,e)=>await Verify();}
    void Check(bool ok,string label){log+=label+": "+(ok?"PASS":"FAIL")+"\r\n";if(!ok)failures++;}
    async Task Press(uint key){for(int i=0;i<30;i++){int hr=send(key);if(hr==0||(key==27&&hr==1))return;if(hr!=unchecked((int)0x8000000A))throw new Exception("TSF key "+key+" HRESULT: "+hr.ToString("X8"));await Task.Delay(30);}throw new Exception("TSF edit session timed out");}
    async Task TypeCode(){await Press(27);box.Clear();await Press('S');await Press('H');await Press('I');}
    async Task Verify(){
        try{
            Environment.SetEnvironmentVariable("LOCALAPPDATA",fixture);
            var cls=new Guid("1B6F3BD2-51A0-4935-B2F9-31CAF0345E71");var profile=new Guid("DF4864AE-309A-4FA1-9A71-37FAC4328923");
            var manager=(ProfileMgr)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("33C53A50-F456-4884-B049-85FD643ECFED")));
            int activation=manager.ActivateProfile(1,0x804,ref cls,ref profile,IntPtr.Zero,0);
            log+="Profile activation HRESULT: 0x"+activation.ToString("X8")+"\r\n";
            var expected=File.ReadAllLines(Path.Combine(fixture,"paging-expected.tsv"));
            string first=expected[0].Split('\t')[1],secondPage=expected[1].Split('\t')[1],thirdPage=expected[2].Split('\t')[1];
            IntPtr library=LoadLibrary(Path.Combine(root,"x64","winintel-tsf.dll"));
            if(library==IntPtr.Zero)throw new Exception("Cannot load TSF DLL");
            send=(Key)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestKey"),typeof(Key));
            var attach=(Attach)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestAttach"),typeof(Attach));
            detach=(Detach)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestDetach"),typeof(Detach));
            int hr=attach();Check(hr==0,"direct service attachment HRESULT 0x"+hr.ToString("X8"));
            Activate();box.Focus();await Task.Delay(200);
            var focus=(FocusStore)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestFocus"),typeof(FocusStore));
            hr=focus(box.Handle);Check(hr==0,"isolated TSF text store HRESULT 0x"+hr.ToString("X8"));
            await TypeCode();await Press(34);await Press(33);await Press(32);
            Check(box.Text==first,"PgDn then PgUp returns first page");
            await TypeCode();await Press(34);await Press('1');
            Check(box.Text==secondPage,"second page digit 1 inserts tenth candidate");
            await TypeCode();await Press(32);
            Check(box.Text==secondPage,"selected second-page candidate now first");
            // Independent input returns to first page even after page navigation.
            await TypeCode();await Press(34);await Press(27);box.Clear();
            await Press('N');await Press('I');await Press('H');await Press('A');await Press('O');await Press(32);
            Check(box.Text=="你好","new code resets page");
            await TypeCode();await Press(34);await Press(34);await Press(33);await Press(33);await Press(32);
            Check(box.Text==secondPage,"repeated page navigation retains learned first");
            await TypeCode();await Press(187);await Press(187);await Press('1');
            Check(box.Text==thirdPage,"equals shortcut reaches third page and selects its first candidate");
            await TypeCode();await Press(189);await Press(32);
            Check(box.Text==thirdPage,"minus on first page stays first and preserves new preference");
        }catch(Exception e){Check(false,e.ToString());}
        finally{if(detach!=null)detach();File.WriteAllText(Path.Combine(fixture,"paging-result.txt"),log);Environment.ExitCode=failures==0?0:1;Close();}
    }
    [STAThread]static void Main(string[] args){
        Environment.SetEnvironmentVariable("LOCALAPPDATA",args[1]);
        Application.EnableVisualStyles();Application.Run(new PagingProbe(args[0],args[1]));
    }
}
