using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using Winintel.Windows;
internal static class VoiceUiProbe {
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static object Field(object obj,string name){return obj.GetType().GetField(name,Flags).GetValue(obj);}
    [STAThread]static void Main(string[] args){
        Application.EnableVisualStyles();
        using(var owner=new Form{Text="数萌语音草稿验证",Size=new Size(520,220)}){
            var box=new TextBox{Multiline=true,Dock=DockStyle.Fill};owner.Controls.Add(box);
            owner.Shown+=async(s,e)=>{
                Controller controller=null;
                try{
                    controller=new Controller(false);
                    var result=typeof(Controller).GetMethod("Result",Flags);
                    result.Invoke(controller,new object[]{0,"final","数萌AI服务器",1L});
                    result.Invoke(controller,new object[]{0,"done","",2L});
                    var preview=(VoicePreview)Field(controller,"preview");
                    var editor=(TextBox)Field(preview,"editor");
                    bool previewOnly=box.Text==""&&editor.Text=="数萌AI服务器"&&!editor.ReadOnly;
                    editor.Text="手动修正后的文字";
                    bool edited=((VoiceDraft)Field(controller,"draft")).Text==editor.Text;
                    ((Button)Field(preview,"confirm")).Enabled=true;
                    using(var image=new Bitmap(preview.Width,preview.Height)){preview.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height));image.Save(args[1]);}
                    typeof(Controller).GetField("target",Flags).SetValue(controller,owner.Handle);
                    owner.Activate();box.Focus();await System.Threading.Tasks.Task.Delay(100);
                    typeof(Controller).GetMethod("Confirm",Flags).Invoke(controller,null);
                    await System.Threading.Tasks.Task.Delay(100);
                    bool confirmed=box.Text=="手动修正后的文字"&&((VoiceDraft)Field(controller,"draft")).Text=="";
                    string status=((Label)Field(preview,"label")).Text;
                    bool protectedDraft=box.Text==""&&((VoiceDraft)Field(controller,"draft")).Text=="手动修正后的文字"&&status=="未能切换回输入窗口，请使用复制按钮";
                    bool ok=previewOnly&&edited&&(confirmed||protectedDraft);
                    File.WriteAllText(args[0],"| 场景 | 结果 |\n|---|---|\n| 停止路径仅保留草稿，不自动输入 | "+(previewOnly?"通过":"失败")+" |\n| 键盘编辑同步更新草稿 | "+(edited?"通过":"失败")+" |\n| 确认上屏的焦点检查 | "+(confirmed?"已输入测试窗口":protectedDraft?"系统拒绝切换焦点，已阻止输入并保留草稿":"失败")+" |\n");
                    Environment.ExitCode=ok?0:1;
                }catch(Exception ex){File.WriteAllText(args[0],ex.ToString());Environment.ExitCode=1;}
                finally{if(controller!=null)controller.ExitThread();owner.Close();}
            };
            Application.Run(owner);
        }
    }
}
