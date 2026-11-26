// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
internal sealed class EnglishProbe:Form {
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll")]static extern IntPtr GetProcAddress(IntPtr h,string name);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Key(uint value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Init();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Mode(int value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Focus(IntPtr handle);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void Detach();
    readonly TextBox box=new TextBox{Multiline=true,Dock=DockStyle.Fill};
    readonly string root,output;Key key,up,control;Mode mode;Detach detach;int failures;readonly List<string> rows=new List<string>{"| Test | Result |","|---|---|"};
    EnglishProbe(string r,string o){root=r;output=o;Controls.Add(box);Text="English completion verification";Shown+=async(s,e)=>await Run();}
    void Check(bool value,string name){rows.Add("| "+name+" | "+(value?"PASS":"FAIL")+" |");if(!value)failures++;}
    T Export<T>(IntPtr library,string name){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,name),typeof(T));}
    async Task Press(uint k){for(int i=0;i<20;i++){int hr=key(k);if(hr>=0)return;if(hr!=unchecked((int)0x8000000A))throw new Exception(hr.ToString("X8"));await Task.Delay(20);}throw new Exception("Edit timed out");}
    async Task Type(string word){await Press(27);box.Clear();foreach(char c in word)await Press(char.ToUpperInvariant(c));}
    async Task Run(){
        try{
            var library=LoadLibrary(Path.Combine(root,"x64","winintel-tsf.dll"));if(library==IntPtr.Zero)throw new Exception("Load failed");
            key=Export<Key>(library,"WinintelTestKey");up=Export<Key>(library,"WinintelTestKeyUp");control=Export<Key>(library,"WinintelTestControl");mode=Export<Mode>(library,"WinintelTestMode");detach=Export<Detach>(library,"WinintelTestDetach");
            Check(Export<Init>(library,"WinintelTestAttach")()==0&&Export<Focus>(library,"WinintelTestFocus")(box.Handle)==0,"Native TSF attached");
            mode(2);await Type("hel");await Press(9);Check(box.Text=="hello","Tab completes hel to hello");
            await Type("deepse");await Press(9);Check(box.Text=="DeepSeek","Technical word canonical spelling ("+box.Text+")");
            await Type("hel");await Press(32);Check(box.Text=="hel ","Space keeps original plus space");
            await Type("unknownxyz");await Press(9);Check(box.Text=="unknownxyz","Unknown word preserved");
            await Type("hello");using(var other=new TextBox()){Controls.Add(other);var focus=Export<Focus>(library,"WinintelTestFocus");focus(other.Handle);Check(box.Text=="hello"&&other.Text=="","Focus change commits original to source control");focus(box.Handle);Controls.Remove(other);}
            await Type("test");Check(key('2')==1&&box.Text=="test","Digits flush word and pass to host");
            await Type("test");Check(key(0xBE)==1&&box.Text=="test","Punctuation flushes and passes");
            await Type("test");Check(key(13)==1&&box.Text=="test","Enter flushes and passes");
            await Type("hello");control(1);Check(key('A')==1&&box.Text=="hello","Ctrl shortcut flushes original and passes");control(0);
            await Type("healo");await Press(36);await Press(39);await Press(39);await Press(46);await Press('L');await Press(9);Check(box.Text=="hello","Middle editing updates English candidates");
            await Type("");await Press(16);await Press('H');up(16);await Press('E');await Press('L');await Press(9);Check(box.Text=="Hello"&&mode(-1)==2,"Shift capitalizes without language toggle");
            await Type("");await Press(16);await Press('H');await Press('E');await Press('L');up(16);await Press(9);Check(box.Text=="HELLO","All uppercase prefix preserved");
            await Type("hel");await Press(16);up(16);Check(box.Text=="hel"&&mode(-1)==1,"Single Shift preserves buffer and switches");
            mode(1);await Type("nihao");await Press(32);Check(box.Text=="你好","Chinese composition unchanged");
            await Type("zhinengti");await Press(32);Check(box.Text=="智能体","Chinese hotword integrated");
            await Type("jushenzhineng");await Press(32);Check(box.Text=="具身智能","Modern Chinese phrase integrated");
        }catch(Exception e){Check(false,e.ToString().Replace("\r"," ").Replace("\n"," "));}
        finally{if(detach!=null)detach();File.WriteAllLines(output,rows);Environment.ExitCode=failures==0?0:1;Close();}
    }
    [STAThread]static void Main(string[] args){Application.EnableVisualStyles();Application.Run(new EnglishProbe(args[0],args[1]));}
}
