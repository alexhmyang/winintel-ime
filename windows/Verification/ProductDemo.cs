// SPDX-License-Identifier: GPL-3.0-or-later
// Desktop recording uses real product forms, native TSF test store and offline ASR.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
class ProductDemo:Form {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 string work,root;Assembly app;object translator,meeting;Form office,preview,quick;
 Process audio,capture;bool ready,finished;string error="";int finals,partials;
 TextBox document,translation;Label heading,description,chapter,detail,modeLabel;
 Stopwatch clock=new Stopwatch();List<object> scenes=new List<object>();List<string> checks=new List<string>();
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr LoadLibrary(string path);
 [DllImport("kernel32.dll")]static extern IntPtr GetProcAddress(IntPtr h,string name);
 [DllImport("user32.dll")]static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Key(uint key);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Attach();
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Focus(IntPtr window);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate int Mode(int mode);
 [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate void Detach();
 Key key,up;Mode mode;Focus focus;Detach detach;
 object New(string name,params object[] args){return Activator.CreateInstance(app.GetType("Winintel.Windows."+name),F,null,args,null);}
 object Field(object obj,string name){return obj.GetType().GetField(name,F).GetValue(obj);}
 object Call(object obj,string name,params object[] args){return obj.GetType().GetMethod(name,F).Invoke(obj,args);}
 T Export<T>(IntPtr module,string name){return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(module,name),typeof(T));}
 Label LabelAt(string text,int x,int y,int w,int h,int size,Color color,bool bold=false){
  var c=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Microsoft YaHei UI",size, bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel),ForeColor=color,BackColor=Color.Transparent};
  Controls.Add(c);return c;
 }
 ProductDemo(string folder){
  work=folder;root=AppDomain.CurrentDomain.BaseDirectory;
  AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;Bounds=new Rectangle(0,0,1536,864);BackColor=Color.FromArgb(12,20,35);DoubleBuffered=true;TopMost=true;
  LabelAt("winintel AI",54,30,440,52,38,Color.White,true);
  LabelAt("数萌 AI 输入法  /  Windows 0.5.4",936,43,550,34,21,Color.FromArgb(161,185,210));
  LabelAt("产品实录",57,114,400,30,20,Color.FromArgb(69,210,188),true);
  heading=LabelAt("",54,178,405,150,48,Color.White,true);
  description=LabelAt("",58,353,392,190,27,Color.FromArgb(181,201,222));
  chapter=LabelAt("",58,650,395,35,22,Color.FromArgb(69,210,188),true);
  detail=LabelAt("",58,704,400,70,19,Color.FromArgb(161,185,210));
  LabelAt("真实产品窗口与本地引擎  ·  中文旁白 / 字幕",470,782,1000,40,23,Color.White);
  var card=new Panel{Location=new Point(470,140),Size=new Size(990,595),BackColor=Color.FromArgb(23,36,54)};Controls.Add(card);
  var title=new Label{Text="工作笔记",Location=new Point(30,25),Size=new Size(500,45),Font=new Font("Microsoft YaHei UI",28,FontStyle.Bold,GraphicsUnit.Pixel),ForeColor=Color.White};
  modeLabel=new Label{Text="智能中英",Location=new Point(746,25),Size=new Size(210,45),TextAlign=ContentAlignment.MiddleRight,Font=new Font("Microsoft YaHei UI",25,FontStyle.Bold,GraphicsUnit.Pixel),ForeColor=Color.FromArgb(69,210,188)};
  document=new TextBox{Multiline=true,ReadOnly=true,Location=new Point(30,86),Size=new Size(930,475),Font=new Font("Microsoft YaHei UI",31,FontStyle.Regular,GraphicsUnit.Pixel),BackColor=Color.FromArgb(245,249,253),BorderStyle=BorderStyle.None};
  card.Controls.AddRange(new Control[]{title,modeLabel,document});
  Shown+=async(s,e)=>await Run();
 }
 void Stage(string name,string title,string desc,string note,string narration){
  scenes.Add(new{name,time=clock.Elapsed.TotalSeconds,narration});heading.Text=title;description.Text=desc;chapter.Text=name;detail.Text=note;
  File.WriteAllText(Path.Combine(work,"scenes.json"),new JavaScriptSerializer().Serialize(scenes),Encoding.UTF8);
 }
 async Task Wait(Func<bool> condition,int seconds=60){for(int i=0;i<seconds*10&&!condition();i++)await Task.Delay(100);if(!condition())throw new Exception("Timed out waiting for actual product result");}
 async Task Press(uint value){
  for(int i=0;i<40;i++){int hr=key(value);if(hr==0||hr==1)return;if(hr!=unchecked((int)0x8000000A))throw new Exception("TSF HRESULT "+hr.ToString("X8"));await Task.Delay(40);}
  throw new Exception("TSF edit timeout");
 }
 void Bind(){Activate();SetForegroundWindow(Handle);document.Focus();if(focus(document.Handle)!=0)throw new Exception("TSF text store focus failed");}
 List<Control> All(Control parent){var list=new List<Control>{parent};foreach(Control c in parent.Controls)list.AddRange(All(c));return list;}
 void PixelFonts(Form window){var controls=All(window);var fonts=controls.Select(c=>new Font(c.Font.FontFamily,c.Font.Size*1.333f,c.Font.Style,GraphicsUnit.Pixel)).ToArray();for(int i=0;i<controls.Count;i++)controls[i].Font=fonts[i];}
 async Task TypePinyin(string text){foreach(char c in text.ToUpperInvariant()){await Press(c);await Task.Delay(150);}await Task.Delay(650);await Press(32);}
 void AudioMessage(string line){
  var m=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(line);string type=(string)m["type"];
  if(type=="status"){ready=m.ContainsKey("ready")&&(bool)m["ready"];if(!ready&&m.ContainsKey("error"))error=Convert.ToString(m["error"]);return;}
  if(type=="error"){error=Convert.ToString(m["error"]);return;}
  if(type=="partial"){
   string text=Convert.ToString(m["text"]);partials++;Call(office,"Preview",text);Call(preview,"Draft",text,true);Call(preview,"UpdateText","本机测试音频 · 实时识别");return;
  }
  if(type=="final"){
   string text=Convert.ToString(m["text"]);
   if(text.Length>0){
    finals++;var caption=Call(meeting,"Add",Convert.ToDouble(m["start"]),Convert.ToDouble(m["end"]),text);
    Call(office,"Add",caption);Call(office,"Preview","");Call(preview,"Draft",text,true);
    checks.Add("| 实际语音识别 "+finals+" | "+text+" |");
   }
   if(m.ContainsKey("finished")&&(bool)m["finished"]){finished=true;Call(office,"Preview","");Call(office,"Recording",false);Call(preview,"Draft",text,false);}
  }
 }
 async Task Run(){
  try{
   Directory.CreateDirectory(Path.Combine(work,"learning"));Environment.SetEnvironmentVariable("LOCALAPPDATA",Path.Combine(work,"learning"));
   app=Assembly.LoadFrom(Path.Combine(root,"winintel-ime.exe"));translator=New("Translation");meeting=New("Meeting");
   office=(Form)New("MeetingWindow",meeting,translator,(Action<Action>)(a=>{if(!IsDisposed)BeginInvoke(a);}));
   office.StartPosition=FormStartPosition.Manual;office.Location=new Point(490,111);office.ClientSize=new Size(920,610);office.AutoScaleMode=AutoScaleMode.None;office.Owner=this;office.TopMost=true;
   translation=(TextBox)Field(office,"translated");
   preview=(Form)New("VoicePreview");quick=(Form)New("QuickBar");quick.Owner=this;quick.TopMost=true;
   PixelFonts(office);PixelFonts(quick);
   var psi=new ProcessStartInfo(Path.Combine(root,"runtime","node.exe"),"\"F:\\Temp\\winintel-ime-video\\audio-demo.mjs\" \""+root.TrimEnd('\\')+"\" \""+work+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8};
   audio=Process.Start(psi);audio.OutputDataReceived+=(s,e)=>{if(e.Data!=null&&!IsDisposed)BeginInvoke((Action)(()=>AudioMessage(e.Data)));};audio.BeginOutputReadLine();audio.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(Path.Combine(work,"asr-error.txt"),e.Data+"\n");};audio.BeginErrorReadLine();
   await Wait(()=>ready||error.Length>0);if(!ready)throw new Exception(error);
   bool warm=false;string warmError="";Call(translator,"Enqueue","你好。","zh-en",(Action<string,string>)((text,err)=>{warmError=err;warm=true;}));await Wait(()=>warm,120);if(warmError.Length>0)throw new Exception(warmError);
   IntPtr dll=LoadLibrary(Path.Combine(work,"testpayload","x64","winintel-tsf.dll"));if(dll==IntPtr.Zero)throw new Exception("Missing isolated TSF test module");
   key=Export<Key>(dll,"WinintelTestKey");up=Export<Key>(dll,"WinintelTestKeyUp");mode=Export<Mode>(dll,"WinintelTestMode");focus=Export<Focus>(dll,"WinintelTestFocus");detach=Export<Detach>(dll,"WinintelTestDetach");
   if(Export<Attach>(dll,"WinintelTestAttach")()!=0)throw new Exception("Native attachment failed");Bind();mode(0);
   var cp=new ProcessStartInfo(@"F:\Temp\winintel-ime-video\package\ffmpeg.exe","-y -f gdigrab -framerate 25 -offset_x 0 -offset_y 0 -video_size 1536x864 -i desktop -an -c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p \""+work+"\\screen-raw.mp4\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true};
   capture=Process.Start(cp);capture.ErrorDataReceived+=(s,e)=>{if(e.Data!=null)File.AppendAllText(Path.Combine(work,"capture-log.txt"),e.Data+"\n");};capture.BeginErrorReadLine();clock.Start();
   Stage("01 / 输入更顺手","一键切换\n边说边译","智能中英输入\n实时离线语音翻译","Windows 0.5.4 · 本机实录","这是数萌 AI 输入法新版。单按 Shift 快速切换中英文，语音识别和实时翻译都在本机完成。");
   document.Text="数萌 AI 输入法\r\n\r\n智能中英  ·  快速切换\r\n实时语音翻译  ·  双语字幕\r\n\r\n本机离线运行  /  CPU 推理";await Task.Delay(8500);
   Stage("02 / 智能中英","看懂意图\n减少切换","拼音保持中文\n明确英文优先原文","实录：nihao / hello / github","输入拼音，中文候选照常出现。遇到 hello、github 等明确的英文单词，优先保留英文。判断完全在本机运行。");
   Bind();mode(0);document.Clear();await TypePinyin("nihao");if(document.Text!="你好")throw new Exception("Pinyin mismatch");document.AppendText("\r\n");
   await TypePinyin("hello");document.AppendText("\r\n");await TypePinyin("github");if(document.Text!="你好\r\nhello\r\ngithub")throw new Exception("English intent result mismatch: "+document.Text);checks.Add("| 智能中英 | "+document.Text.Replace("\r\n"," / ")+" |");await Task.Delay(2600);
   Stage("03 / Shift 快切","按一次\n就切换","单按松开即切换\n手动选择始终优先","长按不反复切换\nShift 组合键不误切","单独按下并松开 Shift，立即切换。英文模式直接输入字母，手动选择不会被自动判断改回。长按和组合键也不会触发反复切换。");
   Bind();mode(0);document.Clear();await TypePinyin("shurufa");document.AppendText("\r\n");
   await Press(16);up(16);modeLabel.Text="EN · Shift 一次";if(mode(-1)!=2)throw new Exception("Shift switch failed");await Task.Delay(1000);
   foreach(char c in "Hello, winintel AI!"){if(char.IsLetter(c)&&key(char.ToUpperInvariant(c))!=1)throw new Exception("English key was intercepted");document.AppendText(c.ToString());await Task.Delay(95);}
   await Task.Delay(2000);await Press(16);up(16);modeLabel.Text="中 · Shift 一次";await TypePinyin("nihao");checks.Add("| 单次 Shift 与英文直通 | "+document.Text.Replace("\r\n"," / ")+" |");
   for(int i=0;i<4;i++){await Press(16);up(16);modeLabel.Text=mode(-1)==2?"EN · 单次切换":"中 · 单次切换";await Task.Delay(600);}await Task.Delay(2500);detach();modeLabel.Text="本机离线";
   Stage("04 / 实时语音翻译","边说边译\n中英互通","讲话中更新临时译文\n停顿后整句复核","实际离线识别与翻译\n演示音频：本机合成中英文","打开实时语音翻译，讲话中的识别结果会持续送入本地翻译模型，停顿后再复核整句。这里使用本机合成的中英文测试音频，画面显示实际处理结果。");
   office.Show();office.Activate();Call(office,"Recording",true);
   audio.StandardInput.WriteLine(new JavaScriptSerializer().Serialize(new{type="start",files=new[]{@"F:\Temp\winintel-ime-model-upgrade\scenario-audio\4-zh.wav",@"F:\Temp\winintel-ime-model-upgrade\scenario-audio\7-en.wav"}}));audio.StandardInput.Flush();
   await Wait(()=>finished||error.Length>0);if(error.Length>0)throw new Exception(error);await Wait(()=>translation.Text.Length>0);
   if(finals<2||partials<1)throw new Exception("Actual streaming ASR did not produce expected segments");
   checks.Add("| 流式处理 | "+partials+" 次临时识别，"+finals+" 段定稿 / "+translation.Text+" |");await Task.Delay(7500);
   Stage("05 / 双语记录","会议内容\n自动留下","原文 + 译文 + 时间戳\n导出字幕和会议记录","SRT 双语字幕\nMarkdown 会议记录","识别结果自动进入双语字幕页，保留每段原文、译文和时间。可以导出字幕和会议记录，方便整理与后期剪辑。");
   var tabs=office.Controls.Cast<Control>().First(c=>c.GetType().Name=="MaterialTabs");tabs.GetType().GetProperty("SelectedIndex").SetValue(tabs,1,null);Call(office,"Render");await Task.Delay(10500);
   string srt=(string)Call(meeting,"Srt"),notes=(string)Call(meeting,"Notes");if(!srt.Contains("-->"))throw new Exception("Subtitle export invalid");
   File.WriteAllText(Path.Combine(work,"演示双语字幕.srt"),srt,new UTF8Encoding(true));File.WriteAllText(Path.Combine(work,"演示会议记录.md"),notes,new UTF8Encoding(true));checks.Add("| 双语字幕导出 | 原文、译文与时间戳完整 |");office.Hide();
   Stage("06 / 隐私与自主","输入留在本机\n安心表达","本地候选排序\n离线识别与翻译","开源 · 无付费 API 依赖\n音频与输入内容不上传","拼音候选、语音识别和中英翻译，均在本机处理。没有付费接口依赖，音频与输入内容无需上传。快捷工具栏也不会抢走正在输入的窗口焦点。");
   document.Text="数萌 AI 输入法\r\n\r\n✓ 本机候选排序与中英判断\r\n✓ 本机离线语音识别\r\n✓ 本机离线中英翻译\r\n✓ 单次 Shift 即时切换\r\n✓ 双语字幕与会议记录";
   quick.StartPosition=FormStartPosition.Manual;quick.Location=new Point(1136,670);quick.Show();await Task.Delay(11000);quick.Hide();
   Stage("07 / 开始体验","让输入流畅\n让沟通直接","winintel AI\n数萌 AI 输入法","winintel.com\n开源：github.com/alexhmyang/winintel-ime","数萌 AI 输入法。快速切换中英文，实时语音翻译，离线完成日常表达。现在就开始体验。");
   document.Text="winintel AI\r\n\r\n数萌 AI 输入法 0.5.4\r\n\r\nShift：中英文切换\r\nF8：开始 / 结束录音\r\n托盘工具箱：实时语音翻译\r\n\r\nwinintel.com";await Task.Delay(8500);
   clock.Stop();File.WriteAllText(Path.Combine(work,"duration.txt"),clock.Elapsed.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));capture.StandardInput.WriteLine("q");capture.StandardInput.Flush();await Wait(()=>capture.HasExited,15);if(capture.ExitCode!=0)throw new Exception("Recording failed");
   File.WriteAllText(Path.Combine(work,"演示验证报告.md"),"画面为新版桌面窗口与本地引擎实际录屏。候选和切换通过隔离 TSF 测试接口送键；英文模式验证字母直通后写入测试文本框。语音为本机合成音频，按真实时速送入实际离线识别工作线程，译文由本地模型生成。\n\n| 场景 | 实际结果 |\n|---|---|\n"+string.Join("\n",checks),new UTF8Encoding(true));
   File.WriteAllText(Path.Combine(work,"complete.txt"),"OK");
  }catch(Exception ex){File.WriteAllText(Path.Combine(work,"error.txt"),ex.ToString());if(capture!=null&&!capture.HasExited){capture.StandardInput.WriteLine("q");capture.StandardInput.Flush();}}
  finally{if(audio!=null&&!audio.HasExited){audio.StandardInput.WriteLine("{\"type\":\"close\"}");audio.StandardInput.Flush();}if(detach!=null)detach();if(office!=null)office.Dispose();if(preview!=null)preview.Dispose();if(quick!=null)quick.Dispose();if(translator is IDisposable)((IDisposable)translator).Dispose();Close();}
 }
 [STAThread]static void Main(string[] args){SetProcessDPIAware();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new ProductDemo(args[0]));}
}
