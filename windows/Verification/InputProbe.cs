// SPDX-License-Identifier: GPL-3.0-or-later
// Sends keys directly to our own TSF service and writes only to this test form.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
internal class InputProbe:Form {
    [ComImport,Guid("71c6e74c-0f28-11d8-a82a-00065b84435c"),InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ProfileMgr{[PreserveSig]int ActivateProfile(uint type,ushort language,ref Guid cls,ref Guid profile,IntPtr hkl,uint flags);}
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll")]static extern IntPtr GetProcAddress(IntPtr h,string name);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Key(uint value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Attach();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void Detach();
    readonly TextBox box=new TextBox{Multiline=true,Dock=DockStyle.Fill};readonly string root,report;Key send;int failures;int lastError;
    internal InputProbe(string r,string output){root=r;report=output;Text="数萌原生输入验证";Width=600;Height=180;Controls.Add(box);Shown+=async(s,e)=>await Verify();}
    async Task Verify(){
        var cls=new Guid("1B6F3BD2-51A0-4935-B2F9-31CAF0345E71");var profile=new Guid("DF4864AE-309A-4FA1-9A71-37FAC4328923");
        var manager=(ProfileMgr)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("33C53A50-F456-4884-B049-85FD643ECFED")));
        int activation=manager.ActivateProfile(1,0x804,ref cls,ref profile,IntPtr.Zero,0);
        IntPtr library=LoadLibrary(Path.Combine(root,"x64","winintel-tsf.dll"));send=(Key)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestKey"),typeof(Key));
        var attach=(Attach)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestAttach"),typeof(Attach));
        int attached=attach();
        Activate();box.Focus();await Task.Delay(250);
        string table="| 场景 | 实际文本 | 结果 |\n|---|---|---|\n";string[] codes={"nihao","zg","nihoa","shurufa"};string[] expected={"你好","中国","你好","输入法"};
        for(int i=0;i<codes.Length;i++){
            box.Clear();send(27);bool ok=true;
            foreach(char key in codes[i].ToUpperInvariant())if(!await Press(key)){ok=false;break;}
            if(ok)ok=await Press(32);await Task.Delay(100);ok=ok&&box.Text==expected[i];if(!ok)failures++;
            table+="| "+codes[i]+" | "+box.Text+" | "+(ok?"通过":"失败")+" 0x"+lastError.ToString("X8")+" |\n";
        }
        File.WriteAllText(report,table+"\nProfile activation HRESULT: 0x"+activation.ToString("X8")+"\nDirect TSF attachment HRESULT: 0x"+attached.ToString("X8"));
        var detach=(Detach)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestDetach"),typeof(Detach));detach();Environment.ExitCode=failures>0?1:0;Close();
    }
    async Task<bool> Press(uint key){for(int i=0;i<30;i++){int hr=send(key);lastError=hr;if(hr==0)return true;if(hr!=unchecked((int)0x8000000A))return false;await Task.Delay(50);}return false;}
    [STAThread]static void Main(string[] args){Application.EnableVisualStyles();Application.Run(new InputProbe(args[0],args[1]));}
}
