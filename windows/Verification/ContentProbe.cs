// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Text;
using System.Web.Script.Serialization;
using Winintel.Windows;
internal static class ContentProbe {
    static int failures;static readonly List<string> rows=new List<string>{"| Test | Result |","|---|---|"};
    static void Check(bool ok,string text){rows.Add("| "+text+" | "+(ok?"PASS":"FAIL")+" |");if(!ok)failures++;}
    [STAThread]static void Main(string[] args){try{Run(args);}catch(Exception e){Console.WriteLine(e.GetType().FullName+": "+e.Message);if(e.InnerException!=null)Console.WriteLine(e.InnerException.Message);Environment.ExitCode=1;}}
    static void Run(string[] args){
        string rss="<rss><channel><item><title>AI &amp; News</title><link>https://example.org/news</link><pubDate>Thu, 26 Nov 2026 13:00:00 GMT</pubDate></item></channel></rss>";
        var items=DailyContent.ParseFeed(rss,"https://example.org/rss");
        Check(items.Count==1&&items[0].Title=="AI & News"&&items[0].Published.Year==2026,"RSS title and publication date");
        string atom="<feed xmlns='http://www.w3.org/2005/Atom'><entry><title>World</title><link rel='alternate' href='https://example.org/world'/><updated>2026-11-26T13:00:00Z</updated></entry></feed>";
        Check(DailyContent.ParseFeed(atom,"https://example.org/atom").Count==1,"Atom namespace and alternate link");
        try{DailyContent.ParseFeed("<!DOCTYPE rss [<!ENTITY x SYSTEM 'file:///C:/Windows/win.ini'>]><rss>&x;</rss>","https://example.org");Check(false,"External entities blocked");}catch{Check(true,"External entities blocked");}
        Check(!DailyContent.WebUrl("file:///C:/Windows")&&!DailyContent.WebUrl("javascript:alert(1)")&&!DailyContent.WebUrl("https://user:pass@example.org"),"Only safe news links accepted");
        Check(DailyContent.WebUrl("http://127.0.0.1:8787/v1/hotwords")&&!DailyContent.WebUrl("http://example.org/rss"),"Local backend supported; public HTTPS required");
        var pack=new JavaScriptSerializer().Deserialize<HotPack>(File.ReadAllText(Path.Combine(args[0],"hotwords.json")));
        Check(pack.schema==1&&pack.chinese.Length>=17&&pack.english.Length>=30,"Bilingual curated dictionary pack");
        // User authorized installation includes downloading public feeds; persist its cache for first launch.
        using(var content=new DailyContent()){
            content.Data.ChineseFeed="https://www.ithome.com/rss/";content.Data.EnglishFeed="https://feeds.npr.org/1004/rss.xml";content.Data.HotwordUrl="";
            string prior=File.ReadAllText(Path.Combine(DailyContent.Folder,"hot-chinese.tsv"));
            try{content.ApplyPack("{\"schema\":1,\"version\":\"2026-11-26\",\"chinese\":[{\"code\":\"bad\\n\",\"text\":\"非法\"}],\"english\":[]}",true);Check(false,"Malformed pack rejected");}catch{Check(File.ReadAllText(Path.Combine(DailyContent.Folder,"hot-chinese.tsv"))==prior,"Malformed pack preserves dictionary");}
            content.Update().GetAwaiter().GetResult();
            Check(content.Data.Items.Any(i=>i.Source.Contains("ithome")&&i.Published.ToLocalTime().Date==DateTime.Today),"Chinese news downloaded with today's publication date");
            Check(content.Data.Items.Any(i=>i.Source.Contains("npr")&&i.Published.ToLocalTime().Date==DateTime.Today),"English news downloaded with today's publication date");
            Check(File.Exists(Path.Combine(DailyContent.Folder,"daily-content.json"))&&content.Data.Downloaded!=default(DateTime),"Offline cache persisted with download date");
            rows.Add("| Latest news | "+(content.Data.Items.Count>0?content.Data.Items.Max(i=>i.Published).ToLocalTime().ToString("yyyy-MM-dd HH:mm"):content.Data.Status)+" |");
            using(var form=new DailyPanel(content)){form.Show();Application.DoEvents();using(var image=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(image,new Rectangle(0,0,image.Width,image.Height));image.Save(Path.Combine(Path.GetDirectoryName(args[1]),"daily-news.png"),ImageFormat.Png);}Check(form.Controls.Count>=3,"News panel rendered with cached headlines");form.Close();}
        }
        File.WriteAllLines(args[1],rows,Encoding.UTF8);Console.WriteLine(string.Join(Environment.NewLine,rows));Environment.ExitCode=failures==0?0:1;
    }
}
