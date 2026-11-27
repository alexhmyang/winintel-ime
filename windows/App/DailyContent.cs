// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;
namespace Winintel.Windows {
internal sealed class NewsItem {public string Title="",Url="",Source="";public DateTime Published;}
internal sealed class HotPhrase {public string code="",text="";}
internal sealed class HotPack {public int schema;public string version="",source="";public HotPhrase[] chinese=new HotPhrase[0];public string[] english=new string[0];}
internal sealed class DailyData {
    public bool Enabled=true;
    public string ChineseFeed="https://www.ithome.com/rss/",EnglishFeed="https://feeds.npr.org/1004/rss.xml",HotwordUrl="";
    public DateTime Attempted,Downloaded;
    public string Status="尚未下载新闻",HotVersion="";
    public List<NewsItem> Items=new List<NewsItem>();
}
internal sealed class DailyContent:IDisposable {
    internal static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"winintel-ime-native");
    readonly string path=Path.Combine(Folder,"daily-content.json");
    readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=2097152};
    readonly HttpClient http=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=60000};
    internal DailyData Data;bool updating,disposed;
    internal event Action Changed;
    internal DailyContent(){
        System.Net.ServicePointManager.SecurityProtocol|=System.Net.SecurityProtocolType.Tls12;
        Directory.CreateDirectory(Folder);
        try{Data=json.Deserialize<DailyData>(File.ReadAllText(path,Encoding.UTF8));}catch{Data=new DailyData();}
        if(Data==null)Data=new DailyData();if(Data.Items==null)Data.Items=new List<NewsItem>();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("winintel-ime/0.5.8");
        try{ApplyPack(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"hotwords.json"),Encoding.UTF8),false);}catch{}
        timer.Tick+=(s,e)=>Start();
        timer.Start();
    }
    internal async void Start(){if(Data.Enabled&&(DateTime.UtcNow-Data.Attempted).TotalHours>=24)try{await Update();}catch(Exception e){Data.Status="更新失败："+e.Message;Notify();}}
    internal void Save(){Atomic(path,json.Serialize(Data));}
    internal static void Atomic(string path,string text){
        string temp=path+"."+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temp,text,new UTF8Encoding(false));
        try{if(File.Exists(path))File.Replace(temp,path,null);else File.Move(temp,path);}finally{if(File.Exists(temp))File.Delete(temp);}
    }
    internal static bool WebUrl(string value){Uri uri;return Uri.TryCreate(value,UriKind.Absolute,out uri)&&(uri.Scheme=="https"||(uri.Scheme=="http"&&uri.IsLoopback))&&string.IsNullOrEmpty(uri.UserInfo);}
    async Task<string> Download(string url){
        if(!WebUrl(url))throw new IOException("地址需要 HTTPS（本机后台允许 HTTP）");
        using(var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        using(var response=await http.GetAsync(url,HttpCompletionOption.ResponseHeadersRead,deadline.Token)){
            response.EnsureSuccessStatusCode();
            if(response.Content.Headers.ContentLength>2097152)throw new IOException("内容超过 2 MB");
            using(var input=await response.Content.ReadAsStreamAsync())using(var output=new MemoryStream()){
                byte[] buffer=new byte[8192];int read;while((read=await input.ReadAsync(buffer,0,buffer.Length,deadline.Token))>0){if(output.Length+read>2097152)throw new IOException("内容超过 2 MB");output.Write(buffer,0,read);}
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }
    }
    internal static List<NewsItem> ParseFeed(string text,string source){
        var doc=new XmlDocument{XmlResolver=null};
        using(var reader=XmlReader.Create(new StringReader(text),new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=2097152}))doc.Load(reader);
        var result=new List<NewsItem>();
        foreach(XmlNode node in doc.SelectNodes("//*[local-name()='item' or local-name()='entry']")){
            Func<string,string> field=name=>{var child=node.SelectSingleNode("*[local-name()='"+name+"']");return child==null?"":child.InnerText.Trim();};
            string title=System.Net.WebUtility.HtmlDecode(field("title")),url=field("link");
            var link=node.SelectSingleNode("*[local-name()='link'][@href and (not(@rel) or @rel='alternate')]");
            if(link!=null)url=link.Attributes["href"].Value;
            DateTimeOffset date;string stamp=field("pubDate");if(stamp.Length==0)stamp=field("published");if(stamp.Length==0)stamp=field("updated");
            if(title.Length==0||title.Length>300||!WebUrl(url)||!DateTimeOffset.TryParse(stamp,CultureInfo.InvariantCulture,DateTimeStyles.AllowWhiteSpaces,out date))continue;
            result.Add(new NewsItem{Title=title,Url=url,Source=source,Published=date.UtcDateTime});if(result.Count>=100)break;
        }
        if(result.Count==0)throw new IOException("没有包含有效日期的新闻条目");return result;
    }
    internal void ApplyPack(string text,bool remote){
        var pack=json.Deserialize<HotPack>(text);
        DateTime version;
        if(pack==null||pack.schema!=1||!DateTime.TryParseExact(pack.version,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out version)||version.Date>DateTime.UtcNow.Date.AddDays(1)||pack.chinese==null||pack.english==null||pack.chinese.Length>10000||pack.english.Length>10000)throw new IOException("热词包格式错误");
        if(!remote&&string.CompareOrdinal(Data.HotVersion,pack.version)>=0&&File.Exists(Path.Combine(Folder,"hot-chinese.tsv"))&&File.Exists(Path.Combine(Folder,"hot-english.tsv")))return;
        var chinese=new StringBuilder();var english=new StringBuilder();
        foreach(var phrase in pack.chinese){
            if(phrase==null||phrase.code==null||phrase.text==null)throw new IOException("热词字段错误");
            string code=phrase.code.Replace(" ","").ToLowerInvariant();
            if(!Regex.IsMatch(code,@"\A[a-z]{1,256}\z")||phrase.text.Length==0||phrase.text.Length>48||phrase.text.IndexOfAny(new[]{'\r','\n','\t'})>=0)throw new IOException("中文热词不合法");
            chinese.Append(code).Append('\t').Append(phrase.text).Append('\n');
        }
        foreach(string word in pack.english){
            if(string.IsNullOrWhiteSpace(word)||word.Length>96||!Regex.IsMatch(word,@"\A[A-Za-z][A-Za-z0-9 .+#-]*\z"))throw new IOException("英文热词不合法");
            string code=Regex.Replace(word.ToLowerInvariant(),"[^a-z]","");if(code.Length>64)throw new IOException("英文热词过长");
            english.Append(code).Append('\t').Append(word).Append("\t200\n");
        }
        // Validate the entire pack before replacing either local cache.
        Atomic(Path.Combine(Folder,"hot-chinese.tsv"),chinese.ToString());Atomic(Path.Combine(Folder,"hot-english.tsv"),english.ToString());Data.HotVersion=pack.version;
    }
    internal async Task Update(){
        if(updating||disposed)return;updating=true;Data.Attempted=DateTime.UtcNow;Data.Status="正在更新…";Save();Notify();
        var notes=new List<string>();bool success=false;
        try{
            foreach(string url in new[]{Data.ChineseFeed,Data.EnglishFeed}){
                if(string.IsNullOrWhiteSpace(url))continue;
                try{
                    var items=ParseFeed(await Download(url),url);if(disposed)return;
                    Data.Items.RemoveAll(item=>item.Source==url);Data.Items.AddRange(items);success=true;
                    DateTime latest=items.Max(item=>item.Published);notes.Add(new Uri(url).Host+"：最新 "+latest.ToLocalTime().ToString("MM-dd HH:mm")+((DateTime.UtcNow-latest).TotalHours>48?"（来源内容已过期）":""));
                }catch(Exception e){notes.Add((WebUrl(url)?new Uri(url).Host:"新闻源")+"：更新失败，保留缓存（"+e.GetBaseException().Message+"）");}
            }
            if(!string.IsNullOrWhiteSpace(Data.HotwordUrl))try{ApplyPack(await Download(Data.HotwordUrl),true);notes.Add("热词 "+Data.HotVersion);}catch(Exception e){notes.Add("热词更新失败，保留本地词库："+e.Message);}
            if(disposed)return;
            Data.Items=Data.Items.GroupBy(item=>item.Url).Select(group=>group.First()).OrderByDescending(item=>item.Published).Take(200).ToList();
            if(success)Data.Downloaded=DateTime.UtcNow;
            Data.Status=string.Join("；",notes);Save();Notify();
        }finally{updating=false;}
    }
    void Notify(){if(!disposed&&Changed!=null)Changed();}
    public void Dispose(){disposed=true;timer.Dispose();http.Dispose();}
}
internal sealed class DailyPanel:Form {
    readonly DailyContent content;readonly ListView list=new ListView{View=View.Details,FullRowSelect=true,HideSelection=false,Dock=DockStyle.Fill};
    readonly Label status=new Label{Dock=DockStyle.Bottom,Height=64,Padding=new Padding(8)};
    readonly TextBox chinese=new TextBox(),english=new TextBox(),hot=new TextBox();
    readonly CheckBox enabled=new CheckBox{Text="每天自动更新（仅下载公开内容）",AutoSize=true};
    readonly Button update=new Button{Text="立即更新",Width=95};
    internal DailyPanel(DailyContent model){
        content=model;Text="数萌 AI · 每日新闻与中英文热词";Size=new Size(940,640);MinimumSize=new Size(760,520);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.White;
        var top=new TableLayoutPanel{Dock=DockStyle.Top,Height=172,ColumnCount=2,Padding=new Padding(10)};top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140));top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Add(top,"中文新闻 RSS",chinese,0);Add(top,"英文新闻 RSS",english,1);Add(top,"自有后台热词地址",hot,2);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};actions.Controls.Add(enabled);actions.Controls.Add(update);
        var words=new Button{Text="个人词库",Width=95};actions.Controls.Add(words);words.Click+=(s,e)=>{using(var form=new PersonalPhrases())form.ShowDialog(this);};
        top.Controls.Add(actions,1,3);Controls.Add(list);Controls.Add(status);Controls.Add(top);
        list.Columns.Add("发布日期",145);list.Columns.Add("新闻标题（双击打开原文）",530);list.Columns.Add("来源",180);
        chinese.Text=content.Data.ChineseFeed;english.Text=content.Data.EnglishFeed;hot.Text=content.Data.HotwordUrl;enabled.Checked=content.Data.Enabled;
        enabled.CheckedChanged+=(s,e)=>{content.Data.Enabled=enabled.Checked;content.Save();};
        update.Click+=async(s,e)=>{content.Data.ChineseFeed=chinese.Text.Trim();content.Data.EnglishFeed=english.Text.Trim();content.Data.HotwordUrl=hot.Text.Trim();content.Save();update.Enabled=false;try{await content.Update();}finally{if(!IsDisposed)update.Enabled=true;}};
        list.DoubleClick+=(s,e)=>{if(list.SelectedItems.Count>0){var item=(NewsItem)list.SelectedItems[0].Tag;if(DailyContent.WebUrl(item.Url))Process.Start(item.Url);}};
        content.Changed+=Render;Render();
    }
    static void Add(TableLayoutPanel panel,string title,TextBox box,int row){panel.RowStyles.Add(new RowStyle(SizeType.Absolute,34));panel.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);box.Dock=DockStyle.Fill;panel.Controls.Add(box,1,row);}
    void Render(){
        if(IsDisposed)return;list.BeginUpdate();list.Items.Clear();
        foreach(var item in content.Data.Items){var row=new ListViewItem(item.Published.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));row.SubItems.Add(item.Title);row.SubItems.Add(new Uri(item.Source).Host);row.Tag=item;list.Items.Add(row);}list.EndUpdate();
        status.Text="本地热词版本："+content.Data.HotVersion+" · 最近成功下载："+(content.Data.Downloaded==default(DateTime)?"无":content.Data.Downloaded.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))+"\r\n"+content.Data.Status;
    }
    protected override void Dispose(bool disposing){if(disposing)content.Changed-=Render;base.Dispose(disposing);}
}
}
