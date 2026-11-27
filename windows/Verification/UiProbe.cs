// SPDX-License-Identifier: GPL-3.0-or-later
// Runs native TSF in an isolated textbox, without changing the system IME profile.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
internal sealed class UiProbe:Form {
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr LoadLibrary(string path);
    [DllImport("kernel32.dll")]static extern IntPtr GetProcAddress(IntPtr h,string name);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string cls,string name);
    static IntPtr FindWindow(string cls,string name){
        IntPtr window=IntPtr.Zero;uint owner;
        while((window=FindWindowEx(IntPtr.Zero,window,cls,name))!=IntPtr.Zero){
            GetWindowThreadProcessId(window,out owner);if(owner==(uint)System.Diagnostics.Process.GetCurrentProcess().Id)return window;
        }return IntPtr.Zero;
    }
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out Rect r);
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
    [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr h,int index);
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
    [DllImport("kernel32.dll")]static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")]static extern bool AttachThreadInput(uint source,uint target,bool attach);
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
    [StructLayout(LayoutKind.Sequential)]struct Rect{public int Left,Top,Right,Bottom;public Rectangle Bounds{get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Key(uint value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Attach();
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Mode(int value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int FocusStore(IntPtr window);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void Detach();
    readonly TextBox box=new TextBox{Multiline=true,Font=new Font("Microsoft YaHei UI",14),Dock=DockStyle.Fill,BorderStyle=BorderStyle.None};
    readonly string root,output;Key key;Detach detach;FocusStore focus;int failures;readonly List<string> log=new List<string>();
    UiProbe(string r,string o){root=r;output=o;Text="winintel AI input test";ClientSize=new Size(720,200);BackColor=Color.White;Padding=new Padding(28);StartPosition=FormStartPosition.Manual;Location=new Point(80,60);TopMost=true;Controls.Add(box);Shown+=async(s,e)=>await Run();}
    void Check(bool ok,string label){log.Add(label+": "+(ok?"PASS":"FAIL"));if(!ok)failures++;}
    static void Capture(Rectangle rectangle,string path){using(var image=new Bitmap(rectangle.Width,rectangle.Height))using(var g=Graphics.FromImage(image)){g.CopyFromScreen(rectangle.Location,Point.Empty,rectangle.Size);image.Save(path,ImageFormat.Png);}}
    static void Capture(Form form,string path){CaptureWindow(form.Handle,path);}
    static void CaptureWindow(IntPtr window,string path){Rect rect;GetWindowRect(window,out rect);using(var image=new Bitmap(rect.Right-rect.Left,rect.Bottom-rect.Top))using(var g=Graphics.FromImage(image)){var dc=g.GetHdc();try{PrintWindow(window,dc,2);}finally{g.ReleaseHdc(dc);}image.Save(path,ImageFormat.Png);}}
    async Task Press(uint value){for(int n=0;n<30;n++){int hr=key(value);if(hr==0||hr==1)return;if(hr!=unchecked((int)0x8000000A))throw new Exception("TSF key HRESULT "+hr.ToString("X8"));await Task.Delay(25);}throw new Exception("TSF edit timed out for key "+value);}
    async Task Type(string code){await Press(27);box.Clear();foreach(char c in code)await Press((uint)char.ToUpperInvariant(c));}
    static object Create(Assembly assembly,string name,params object[] args){return Activator.CreateInstance(assembly.GetType("Winintel.Windows."+name,true),BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,args,null);}
    static object Call(object instance,string name,params object[] args){return instance.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(instance,args);}
    static List<Control> Descendants(Control parent){var result=new List<Control>();foreach(Control c in parent.Controls){result.Add(c);result.AddRange(Descendants(c));}return result;}
    static void FocusTest(Form form){uint pid;uint foreground=GetWindowThreadProcessId(GetForegroundWindow(),out pid),current=GetCurrentThreadId();bool attached=foreground!=0&&foreground!=current&&AttachThreadInput(current,foreground,true);try{form.Activate();SetForegroundWindow(form.Handle);}finally{if(attached)AttachThreadInput(current,foreground,false);}}
    async Task Run(){
        try{
            Environment.SetEnvironmentVariable("LOCALAPPDATA",Path.Combine(output,"learning"));
            Directory.CreateDirectory(Path.Combine(output,"learning"));
            var library=LoadLibrary(Path.Combine(root,"x64","winintel-tsf.dll"));if(library==IntPtr.Zero)throw new Exception("Cannot load TSF test module");
            key=(Key)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestKey"),typeof(Key));
            detach=(Detach)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestDetach"),typeof(Detach));
            var attach=(Attach)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestAttach"),typeof(Attach));
            focus=(FocusStore)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestFocus"),typeof(FocusStore));
            Check(attach()==0&&focus(box.Handle)==0,"isolated TSF attachment");
            FocusTest(this);box.Focus();focus(box.Handle);await Type("nihao");
            var candidate=FindWindow("winintel.Candidates.052",null);Rect rect;
            Check(candidate!=IntPtr.Zero&&IsWindowVisible(candidate),"candidate appears during composition");
            Check(GetForegroundWindow()==Handle,"candidate show preserves application focus");
            Check((GetWindowLong(candidate,-20)&0x08000000)!=0&&SendMessage(candidate,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()==3,"candidate forbids mouse activation");
            GetWindowRect(candidate,out rect);Check(rect.Bottom-rect.Top<=220&&rect.Right-rect.Left<=760,"large text candidate geometry");
            await Type("nihxo");await Press(37);await Press(37);await Press(46);await Press('A');await Press(32);
            Check(box.Text=="你好","middle correction refreshes Chinese candidates");
            var up=(Key)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestKeyUp"),typeof(Key));
            var mode=(Mode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestMode"),typeof(Mode));
            var control=(Key)Marshal.GetDelegateForFunctionPointer(GetProcAddress(library,"WinintelTestControl"),typeof(Key));
            await Type("nihao");await Press(37);await Press(8);control(1);await Press('Z');control(0);
            await Press(16);up(16);Check(box.Text=="nihao","Ctrl Z restores middle deletion and caret");mode(0);
            await Type("nihao");await Press(37);await Press(8);control(1);await Press('Z');await Press('Y');control(0);
            await Press(16);up(16);Check(box.Text=="niho","Ctrl Y redoes middle deletion");mode(0);
            await Type("nihao");await Press(37);await Press(8);control(1);await Press('Z');control(0);await Press(16);control(1);await Press('Z');control(0);up(16);
            Check(mode(-1)==0,"Ctrl Shift Z never toggles language");await Press(16);up(16);Check(box.Text=="niho","Ctrl Shift Z redoes middle deletion");mode(0);
            await Type("nihao");await Press(37);await Press(8);control(1);await Press('Z');control(0);await Press('B');control(1);await Press('Y');control(0);
            await Press(16);up(16);Check(box.Text=="nihabo","new insertion clears redo history");mode(0);
            await Type("nihao");await Task.Delay(50);SendMessage(candidate,0x202,IntPtr.Zero,new IntPtr((18<<16)|13));await Press('A');
            await Press(16);up(16);Check(box.Text=="anihao"&&GetForegroundWindow()==Handle,"mouse moves composition caret without activation");mode(0);
            await Press(27);control(1);Check(key('Z')==1&&key('Y')==1,"empty composition passes undo and redo to host");control(0);
            await Type("nihao");await Press(36);await Press(8);await Press('A');await Press(35);await Press(46);await Press('B');
            await Press(16);up(16);Check(box.Text=="anihaob","Home End and delete boundaries preserve buffer");mode(0);
            await Type(new string('a',257));await Press(36);for(int i=0;i<100;i++)await Press(39);await Press(46);await Press('B');
            await Task.Delay(80);CaptureWindow(candidate,Path.Combine(output,"long-composition.png"));
            Check(GetForegroundWindow()==Handle,"long composition editing keeps application focus");
            await Press(16);int longRelease=up(16);log.Add("long edit commit HRESULT="+longRelease.ToString("X8")+", length="+box.Text.Length+", value="+box.Text);Check(box.Text==new string('a',100)+"b"+new string('a',155),"256 letter middle correction preserves suffix");mode(0);
            string sentence="";for(int i=0;i<20;i++)sentence+="nihao";await Type(sentence);await Press(32);
            string translated="";for(int i=0;i<20;i++)translated+="你好";Check(box.Text==translated,"100 letter composition commits full sentence");
        }catch(Exception e){Check(false,e.ToString());}
        // Continue the main UI checks only after removing the candidate composition.
        try{
            if(key!=null){FocusTest(this);box.Focus();focus(box.Handle);await Press(27);box.Clear();}
            var assembly=Assembly.LoadFrom(Path.Combine(root,"..","winintel-ime.exe"));
            var settings=Create(assembly,"Settings");
            using(var panel=(Form)Create(assembly,"Panel",settings,(Action)(()=>{}))){
                panel.Location=new Point(70,50);panel.Show();FocusTest(panel);await Task.Delay(120);
                Check(!panel.TopMost,"settings never forced on top");
                var tabs=Descendants(panel).Find(c=>c.GetType().Name=="MaterialTabs");
                Check(tabs!=null&&(int)tabs.GetType().GetProperty("TabCount").GetValue(tabs,null)==3,"settings split into keyboard voice and office");
                Capture(panel,Path.Combine(output,"settings.png"));
                tabs.GetType().GetProperty("SelectedIndex").SetValue(tabs,1,null);await Task.Delay(60);Capture(panel,Path.Combine(output,"microphone.png"));
                tabs.GetType().GetProperty("SelectedIndex").SetValue(tabs,2,null);await Task.Delay(60);Capture(panel,Path.Combine(output,"office.png"));panel.Hide();
            }
            FocusTest(this);box.Focus();focus(box.Handle);await Task.Delay(150);
            using(var bar=(Form)Create(assembly,"QuickBar")){
                bar.Location=new Point(900,500);bar.Show();await Task.Delay(100);
                Check(GetForegroundWindow()==Handle&&!bar.ShowInTaskbar,"quick bar preserves input focus and stays out of taskbar");
                Check(SendMessage(bar.Handle,0x21,IntPtr.Zero,IntPtr.Zero).ToInt32()==3,"quick bar ignores mouse activation");
                Check(bar.ClientSize.Width<=270&&bar.ClientSize.Height<=48,"quick bar has compact dimensions");
                Capture(bar,Path.Combine(output,"quickbar.png"));bar.Hide();
            }
            using(var tools=(Form)Create(assembly,"QuickPanel")){
                tools.Show();await Task.Delay(80);Check(GetForegroundWindow()==Handle&&!tools.TopMost,"toolbox opens without activating or forcing topmost");
                Capture(tools,Path.Combine(output,"toolbox.png"));tools.Hide();
            }
            using(var access=(IDisposable)Create(assembly,"QuickAccess")){
                Call(access,"ShowTools",true);await Task.Delay(100);
                var accessType=access.GetType();var toolbar=(Form)accessType.GetField("bar",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(access);
                var popup=(Form)accessType.GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(access);
                Check(Screen.FromHandle(toolbar.Handle).WorkingArea.Contains(toolbar.Bounds)&&Screen.FromHandle(popup.Handle).WorkingArea.Contains(popup.Bounds),"quick tools respect monitor work area");
                using(var other=new Form{Text="Other application"}){other.Show();other.Activate();await Task.Delay(400);Check(!toolbar.Visible&&!popup.Visible,"quick tools hide when another window takes focus");other.Close();}
            }
            FocusTest(this);box.Focus();focus(box.Handle);await Task.Delay(100);
            using(var preview=(Form)Create(assembly,"VoicePreview")){
                log.Add("before voice: foreground="+GetForegroundWindow()+", target="+Handle);
                Call(preview,"BeginSession",Handle);Call(preview,"Draft","让输入更自然，让表达更轻松。",true);Call(preview,"UpdateText","正在聆听 · 按 F8 结束");
                log.Add("after voice: foreground="+GetForegroundWindow()+", preview="+preview.Handle+", visible="+preview.Visible);
                Check(preview.Visible&&GetForegroundWindow()==Handle,"voice preview appears without stealing focus");
                await Task.Delay(100);
                Check(preview.ClientSize.Height<=200&&!preview.ShowInTaskbar,"voice compact panel stays out of taskbar");
                int stopped=0,confirmed=0;
                preview.GetType().GetEvent("StopRequested",BindingFlags.Instance|BindingFlags.NonPublic).GetAddMethod(true).Invoke(preview,new object[]{(Action)(()=>stopped++)});
                preview.GetType().GetEvent("ConfirmRequested",BindingFlags.Instance|BindingFlags.NonPublic).GetAddMethod(true).Invoke(preview,new object[]{(Action)(()=>confirmed++)});
                var commit=(Button)Descendants(preview).Find(c=>c.Text=="结束录音");commit.PerformClick();
                Check(stopped==1&&confirmed==0,"recording action stops audio instead of inserting text");
                Call(preview,"Draft","让输入更自然，让表达更轻松。",false);commit.PerformClick();
                Check(stopped==1&&confirmed==1,"finished draft action requests confirmation");
                FocusTest(preview);
                var draftEditor=(TextBox)Descendants(preview).Find(c=>c is TextBox);draftEditor.Focus();draftEditor.Select(2,3);
                Call(preview,"Draft","让输入更自然，让表达更轻松。追加。",false);
                Check(draftEditor.SelectionStart==2&&draftEditor.SelectionLength==3,"draft refresh preserves active selection");
                draftEditor.SelectedText="新";
                Check(draftEditor.Text.StartsWith("让输新然"),"draft selection replacement stays in place");
                FocusTest(this);box.Focus();box.Text="前旧后";box.Select(1,1);
                var insert=assembly.GetType("Winintel.Windows.TextInput").GetMethod("Insert",BindingFlags.Static|BindingFlags.NonPublic);
                bool inserted=(bool)insert.Invoke(null,new object[]{Handle,"新",false,4096});await Task.Delay(150);
                Check(inserted&&box.Text=="前新后","desktop voice insertion replaces target selection");
                box.Text="前后";box.Select(1,0);inserted=(bool)insert.Invoke(null,new object[]{Handle,"中",false,4096});await Task.Delay(150);
                Check(inserted&&box.Text=="前中后","desktop voice insertion uses middle caret");
                Call(preview,"Draft","让输入更自然，让表达更轻松。",true);
                Capture(preview,Path.Combine(output,"voice.png"));
                using(var other=new Form{Text="Other application",Location=new Point(40,40),StartPosition=FormStartPosition.Manual}){other.Show();other.Activate();await Task.Delay(400);Check(!preview.Visible,"voice preview hides when switching applications");other.Close();}
                FocusTest(this);Call(preview,"RefreshVisibility");Check(preview.Visible&&GetForegroundWindow()==Handle,"voice preview returns only to its target application");
                preview.Close();Call(preview,"UpdateText","Late result");await Task.Delay(300);Check(!preview.Visible,"closing preview prevents asynchronous reopening");
                Call(preview,"EndSession");Check(!preview.Visible,"confirmed draft has no lingering panel");
            }
            var meeting=Create(assembly,"Meeting");var translation=Create(assembly,"Translation");
            using(var window=(Form)Create(assembly,"MeetingWindow",meeting,translation,(Action<Action>)(a=>a()))){
                window.Show();window.Activate();await Task.Delay(100);Check(!window.TopMost,"translation workspace never forced on top");Capture(window,Path.Combine(output,"translation.png"));window.Hide();
            }
            ((IDisposable)translation).Dispose();
            FocusTest(this);box.Focus();int rebound=focus(box.Handle);log.Add("rebind after auxiliary windows: "+rebound.ToString("X8"));await Task.Delay(150);
            await Type("yanghuiming");var nameWindow=FindWindow("winintel.Candidates.052",null);CaptureWindow(nameWindow,Path.Combine(output,"name-candidates.png"));
            await Press('1');Check(box.Text=="杨慧明","full pinyin composes an unlisted name in TSF");
            await Type("yhm");await Press('1');Check(box.Text=="杨慧明","learned name is first with initials in TSF");
            await Type("shanhaoming");await Press('1');Check(box.Text=="单浩明","surname-specific pronunciation inserts correctly in TSF");
            await Type("shi");var candidate=FindWindow("winintel.Candidates.052",null);Rect r;GetWindowRect(candidate,out r);
            CaptureWindow(candidate,Path.Combine(output,"candidates.png"));
            await Press(34);await Press(33);await Press('1');log.Add("keyboard committed text="+box.Text);Check(box.Text.Length>0&&!IsWindowVisible(candidate),"keyboard selection inserts text and hides candidates");
            await Type("nihao");await Press(32);Check(box.Text=="\u4f60\u597d","pinyin composition remains functional");
            await Type("shi");GetWindowRect(candidate,out r);
            // First candidate occupies the top-left cell; mouse selection must commit into this textbox.
            int point=(48<<16)|30;SendMessage(candidate,0x202,IntPtr.Zero,new IntPtr(point));
            Check(box.Text.Length>0&&GetForegroundWindow()==Handle&&!IsWindowVisible(candidate),"mouse selection commits without taking focus");
            Location=new Point(80,Screen.PrimaryScreen.WorkingArea.Bottom-90);box.Focus();await Type("shi");GetWindowRect(candidate,out r);
            var work=Screen.FromHandle(candidate).WorkingArea;
            Check(work.Contains(r.Bounds),"candidates stay inside monitor work area");
            Check(r.Bottom<=box.PointToScreen(new Point(3,3)).Y,"candidates flip above caret near screen bottom");
            await Press(27);Check(!IsWindowVisible(candidate),"escape immediately hides candidates");
            var up=(Key)Marshal.GetDelegateForFunctionPointer(GetProcAddress(libraryForTest(),"WinintelTestKeyUp"),typeof(Key));
            var repeat=(Key)Marshal.GetDelegateForFunctionPointer(GetProcAddress(libraryForTest(),"WinintelTestRepeat"),typeof(Key));
            var mode=(Mode)Marshal.GetDelegateForFunctionPointer(GetProcAddress(libraryForTest(),"WinintelTestMode"),typeof(Mode));
            mode(0);await Press(16);up(16);Check(mode(-1)==2,"single Shift switches immediately to English");
            Check(key('H')==0,"English mode collects letters for local completion");await Press(27);
            bool switches=true;for(int i=0;i<20;i++){int before=mode(-1);await Press(16);up(16);switches&=mode(-1)!=before;}
            Check(switches,"twenty consecutive Shift taps each switch exactly once");
            mode(0);await Press(16);repeat(16);repeat(16);up(16);Check(mode(-1)==2,"held Shift autorepeat toggles only once on release");
            mode(0);await Press(16);await Press('A');up(16);Check(mode(-1)==0,"Shift with a letter never toggles language");
            await Press(27);mode(0);await Press(16);await Press(17);up(16);Check(mode(-1)==0,"Shift then Control combination never toggles language");
            mode(0);await Type("nihao");await Press(16);up(16);Check(box.Text=="nihao"&&mode(-1)==2,"switch preserves uncommitted letters without loss");
            mode(0);await Type("hello");await Press(32);Check(box.Text=="hello","automatic mode recognizes clear English word");
            mode(0);await Type("nihao");await Press(32);Check(box.Text=="你好","automatic mode preserves Chinese pinyin");
            mode(0);await Type("http");await Press(0xBE);await Press('C');await Press('O');await Press('M');await Press(32);Check(box.Text=="http.com","web address stays in English through punctuation");
            mode(1);await Type("nihao");await Press(32);Check(box.Text=="你好"&&mode(-1)==1,"explicit Chinese choice remains fixed");
            mode(0);
            var language=assembly.GetType("Winintel.Windows.LanguageControl").GetMethod("Send",BindingFlags.Static|BindingFlags.NonPublic);
            Check((int)language.Invoke(null,new object[]{box.Handle,1})==2&&mode(-1)==2,"toolbar language control targets active input thread");
            Check((int)language.Invoke(null,new object[]{box.Handle,4})==0&&mode(-1)==0,"toolbar control restores automatic detection");
        }catch(Exception e){Check(false,e.ToString());}
        finally{if(detach!=null)detach();File.WriteAllLines(Path.Combine(output,"ui-results.txt"),log);Environment.ExitCode=failures==0?0:1;Close();}
    }
    IntPtr libraryForTest(){return LoadLibrary(Path.Combine(root,"x64","winintel-tsf.dll"));}
    [STAThread]static void Main(string[] args){Directory.CreateDirectory(args[1]);Directory.CreateDirectory(Path.Combine(args[1],"learning"));Application.EnableVisualStyles();Application.Run(new UiProbe(args[0],args[1]));}
}
