using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Winintel.Windows;
internal static class MeetingProbe {
    static void Check(bool condition,string name){if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);}
    [STAThread]static void Main(string[] args){
        Application.EnableVisualStyles();
        var meeting=new Meeting();
        Check(meeting.Srt()=="","empty export");
        var a=meeting.Add(1.125,2.5,"决定下周一发布。");
        a.Target="Release next Monday.";
        meeting.Add(0,1,"First\r\nline");
        meeting.Add(2,2,"Overlap");
        string srt=meeting.Srt();
        Check(srt.StartsWith("1\r\n00:00:00,000 --> 00:00:01,000"),"chronological order");
        Check(srt.Contains("00:00:01,125 --> 00:00:02,500"),"millisecond timestamp");
        Check(srt.Contains("00:00:02,500 --> 00:00:02,600"),"overlap and zero duration");
        Check(srt.Contains("First line")&&srt.Contains("Release next Monday."),"source and bilingual export");
        Check(meeting.Notes().Contains("决定下周一发布")&&!meeting.Notes().Contains("张伟"),"meeting extracts without invented people");
        meeting.Add(3601.2,3602,"Long recording");
        Check(meeting.Srt().Contains("01:00:01,200"),"hour timestamp");
        int generation=meeting.Generation;meeting.Clear();
        Check(meeting.Generation!=generation&&meeting.Captions.Count==0,"clear invalidates old results");
        using(var engine=new Translation()){
            using(var window=new MeetingWindow(meeting,engine,action=>action())){
                window.Show();Application.DoEvents();window.Close();
                Check(!window.IsDisposed&&!window.Visible,"close hides window");
                window.Show();Application.DoEvents();window.Render();
                Check(window.Visible,"window reopens");
                window.Preview("实时预览");Application.DoEvents();
                Check(window.Controls.OfType<TabControl>().First().TabPages[1].Controls.OfType<TextBox>().First().Text.Contains("实时预览"),"partial captions visible");
                window.Controls.OfType<TabControl>().First().SelectedIndex=1;
                var sample=meeting.Add(0,4.2,"我们决定下周一发布新版本。");
                sample.Target="We decided to release the new version next Monday.";
                window.Render();Application.DoEvents();
                using(var image=new System.Drawing.Bitmap(window.Width,window.Height)){
                    window.DrawToBitmap(image,new System.Drawing.Rectangle(0,0,window.Width,window.Height));
                    image.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"meeting-window-probe.png"));
                }
                window.Dispose();window.Render();
                window.Preview("迟到结果");
                Check(window.IsDisposed,"late render after dispose");
            }
        }
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"meeting-probe-result.txt"),"12 checks passed");
    }
}
