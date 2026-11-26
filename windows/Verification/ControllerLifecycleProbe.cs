using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Winintel.Windows;
internal static class ControllerLifecycleProbe {
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object target,string name){return target.GetType().GetField(name,Flags).GetValue(target);}
    [STAThread]static void Main(string[] args){
        Application.EnableVisualStyles();Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        int errors=0;Application.ThreadException+=(s,e)=>errors++;
        using(var owner=new Form()){
            owner.Shown+=async(s,e)=>{
                var controller=new Controller(false);
                try{
                    var result=typeof(Controller).GetMethod("Result",Flags);
                    result.Invoke(controller,new object[]{0,"final","测试句子。",1L});
                    result.Invoke(controller,new object[]{0,"final","测试句子。",1L});
                    bool duplicate=((VoiceDraft)Field(controller,"draft")).Text=="测试句子。";
                    result.Invoke(controller,new object[]{-1,"final","旧会话迟到文字",2L});
                    bool stale=((VoiceDraft)Field(controller,"draft")).Text=="测试句子。";
                    result.Invoke(controller,new object[]{0,"done","",2L});
                    bool previewOnly=((VoiceDraft)Field(controller,"draft")).Text=="测试句子。";
                    controller.ExitThread();
                    result.Invoke(controller,new object[]{0,"partial","退出后迟到结果",3L});
                    await Task.Yield();
                    bool ok=(bool)Field(controller,"exiting")&&((VoicePreview)Field(controller,"preview")).IsDisposed&&duplicate&&stale&&previewOnly&&errors==0;
                    File.WriteAllText(args[0],"| 场景 | 结果 |\n|---|---|\n| 相同分段 ID 只处理一次 | "+(duplicate?"通过":"失败")+" |\n| 旧会话迟到结果忽略 | "+(stale?"通过":"失败")+" |\n| 停止录音保留草稿 | "+(previewOnly?"通过":"失败")+" |\n| 退出后回调不更新已释放窗口 | "+(ok?"通过":"失败")+" |\n");
                    Environment.ExitCode=ok?0:1;
                }catch(Exception ex){File.WriteAllText(args[0],ex.ToString());Environment.ExitCode=1;controller.ExitThread();}
                finally{owner.Close();}
            };
            Application.Run(owner);
        }
    }
}
