// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
class TranslationProbe:Form {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 Assembly app;object engine,meeting;Form office;List<string> rows=new List<string>();int failures;string output;
 object New(string name,params object[] args){return Activator.CreateInstance(app.GetType("Winintel.Windows."+name),F,null,args,null);}
 object Call(object obj,string name,params object[] args){return obj.GetType().GetMethod(name,F).Invoke(obj,args);}
 object Field(object obj,string name){return obj.GetType().GetField(name,F).GetValue(obj);}
 void Check(bool ok,string name,string detail){rows.Add("| "+name+" | "+(ok?"通过":"失败")+" | "+detail.Replace("|","/").Replace("\r"," ").Replace("\n"," ")+" |");if(!ok)failures++;}
 async Task Wait(Func<bool> condition){for(int i=0;i<1200&&!condition();i++)await Task.Delay(50);if(!condition())throw new Exception("translation timeout");}
 async Task Run(){
  try{
   app=Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"winintel-ime.exe"));
   engine=New("Translation");meeting=New("Meeting");
   office=(Form)New("MeetingWindow",meeting,engine,(Action<Action>)(a=>{if(!IsDisposed)BeginInvoke(a);}));
   var source=(TextBox)Field(office,"source");var translated=(TextBox)Field(office,"translated");
   var direction=(ComboBox)Field(office,"direction");
   var clock=Stopwatch.StartNew();bool warm=false;string warmText="",warmError="";
   Call(engine,"Enqueue","你好。","zh-en",(Action<string,string>)((text,error)=>{warmText=text;warmError=error;warm=true;}));
   await Wait(()=>warm);Check(warmError.Length==0&&warmText.Length>0,"离线模型冷启动",clock.Elapsed.TotalSeconds.ToString("0.00")+" 秒 / "+warmText);
   Call(office,"Recording",true);
   clock.Restart();Call(office,"Preview","我们决定下周一发布新版本。");
   await Wait(()=>translated.Text.Length>0);Check(source.Text.Contains("下周一"),"讲话中的临时译文",clock.Elapsed.TotalSeconds.ToString("0.00")+" 秒 / "+translated.Text);
   var caption=Call(meeting,"Add",0.0,4.0,"我们决定下周一发布新版本。");
   Call(office,"Add",caption);Call(office,"Preview","");
   await Wait(()=>((string)Field(caption,"Target")).Length>0);
   Check(translated.Text==(string)Field(caption,"Target"),"整句译文覆盖临时译文",translated.Text);
   Call(office,"Preview","旧的临时文字不应覆盖新句子");Call(office,"TranslatePreview");
   var next=Call(meeting,"Add",4.0,8.0,"Please confirm the project plan.");
   Call(office,"Add",next);Call(office,"Preview","");
   await Wait(()=>((string)Field(next,"Target")).Length>0);
   await Task.Delay(800);Check(translated.Text==(string)Field(next,"Target"),"迟到临时结果不会覆盖定稿",translated.Text);
   direction.SelectedIndex=2;
   await Wait(()=>((string)Field(next,"Target")).Length>0);
   Check(((string)Field(next,"Target")).Length>0,"英文语音自动转中文",((string)Field(next,"Target")));
   source.Text="手动修改原文";Call(office,"Preview","正在测试关闭自动翻译");
   ((CheckBox)Field(office,"live")).Checked=false;string previous=translated.Text;
   await Task.Delay(1000);Check(translated.Text==previous,"关闭自动翻译取消待处理预览","译文保持不变");
   Call(office,"Recording",false);
   office.Dispose();office=null;((IDisposable)engine).Dispose();engine=null;
  }catch(Exception e){Check(false,"运行异常",e.ToString());}
  finally{
   if(office!=null)office.Dispose();if(engine!=null)((IDisposable)engine).Dispose();
   File.WriteAllText(output,"| 检查 | 结果 | 实测 |\n|---|---|---|\n"+string.Join("\n",rows),new System.Text.UTF8Encoding(true));
   Environment.ExitCode=failures==0?0:1;Close();
  }
 }
 TranslationProbe(string path){output=path;Shown+=async(s,e)=>await Run();}
 [STAThread]static void Main(string[] args){Application.EnableVisualStyles();Application.Run(new TranslationProbe(args[0]));}
}
