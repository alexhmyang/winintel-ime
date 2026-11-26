using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Winintel.Windows;
internal static class PreviewLifecycleProbe {
    [STAThread]static void Main(string[] args){
        Application.EnableVisualStyles();
        using(var owner=new Form()){
            owner.Shown+=async(s,e)=>{
                bool closed=false,disposed=false,delayed=false,restarted=false;
                try{using(var preview=new VoicePreview()){preview.UpdateText("正在聆听");preview.Close();preview.UpdateText("识别完成");closed=!preview.IsDisposed&&!preview.Visible;preview.BeginSession();preview.UpdateText("下一次录音");restarted=preview.Visible;for(int i=0;i<100;i++){preview.Close();preview.UpdateText("迟到结果");preview.BeginSession();preview.UpdateText("新会话");}}}catch(ObjectDisposedException){}
                try{var preview=new VoicePreview();preview.UpdateText("正在聆听");preview.Dispose();preview.UpdateText("退出后的结果");disposed=!preview.Visible;}catch(ObjectDisposedException){}
                try{var preview=new VoicePreview();preview.UpdateText("正在识别");var release=new TaskCompletionSource<bool>();var work=Continue(preview,release.Task);preview.Dispose();release.SetResult(true);await work;delayed=true;}catch(ObjectDisposedException){}
                File.WriteAllText(args[0],"| 场景 | 结果 |\n|---|---|\n| 关闭预览后继续接收结果，保持隐藏 | "+(closed?"通过":"复现异常")+" |\n| 释放预览后迟到回调 | "+(disposed?"通过":"复现异常")+" |\n| 异步等待期间释放预览 | "+(delayed?"通过":"复现异常")+" |\n| 新会话恢复预览，重复关闭 100 次 | "+(restarted?"通过":"失败")+" |\n");
                Environment.ExitCode=closed&&disposed&&delayed&&restarted?0:1;owner.Close();
            };
            Application.Run(owner);
        }
    }
    static async Task Continue(VoicePreview preview,Task release){await release;preview.UpdateText("异步识别完成");}
}
