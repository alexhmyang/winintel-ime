// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
namespace Winintel.Windows {
internal sealed class NewsOptions {
    public int RefreshMinutes=5;
    public string Endpoint="";
    public bool Fallback=true;
}
internal sealed class NewsResponse {public int schema;public RemoteNews[] items;}
internal sealed class RemoteNews {public string title,url,source,published_at;}
internal sealed class RealtimeNews:IDisposable {
    readonly DailyContent content;
    readonly string path=Path.Combine(DailyContent.Folder,"realtime-news.json");
    readonly JavaScriptSerializer json=new JavaScriptSerializer{MaxJsonLength=2097152};
    readonly HttpClient http=new HttpClient{Timeout=TimeSpan.FromSeconds(15)};
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=30000};
    bool busy,disposed;DateTime attempted;RealtimeNewsPanel panel;
    internal NewsOptions Options;internal event Action Changed;
    internal DailyData Data{get{return content.Data;}}
    internal RealtimeNews(DailyContent existing){
        content=existing;
        // Disable the legacy scheduler; this module owns the refresh cadence.
        var legacy=(System.Windows.Forms.Timer)typeof(DailyContent).GetField("timer",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(content);legacy.Stop();
        try{Options=json.Deserialize<NewsOptions>(File.ReadAllText(path,Encoding.UTF8));}catch{Options=new NewsOptions();}
        if(Options==null)Options=new NewsOptions();Options.RefreshMinutes=Math.Max(1,Math.Min(60,Options.RefreshMinutes));
        timer.Tick+=(s,e)=>Start(false);timer.Start();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("winintel-ime-news/1.0");
    }
    internal void Save(){DailyContent.Atomic(path,json.Serialize(Options));content.Save();}
    internal void Show(){if(panel==null||panel.IsDisposed)panel=new RealtimeNewsPanel(this);panel.Show();panel.Activate();}
    internal async void Start(bool force){if(!force&&(!Data.Enabled||(DateTime.UtcNow-attempted).TotalMinutes<Options.RefreshMinutes))return;try{await Refresh();}catch(Exception e){if(!disposed){Data.Status="新闻刷新失败，保留缓存："+e.GetBaseException().Message;Notify();}}}
    internal static List<NewsItem> Parse(string text,string endpoint){
        var pack=new JavaScriptSerializer{MaxJsonLength=2097152}.Deserialize<NewsResponse>(text);
        if(pack==null||pack.schema!=1||pack.items==null||pack.items.Length>200)throw new InvalidDataException("新闻接口格式错误");
        var result=new List<NewsItem>();
        foreach(var item in pack.items){
            DateTimeOffset date;
            if(item==null||string.IsNullOrWhiteSpace(item.title)||item.title.Length>300||!DailyContent.WebUrl(item.url)||
               !DateTimeOffset.TryParse(item.published_at,CultureInfo.InvariantCulture,DateTimeStyles.AllowWhiteSpaces,out date)||date.UtcDateTime>DateTime.UtcNow.AddMinutes(10))throw new InvalidDataException("新闻条目标题、链接或日期不合法");
            // Source is an absolute URL so its publisher remains visible.
            result.Add(new NewsItem{Title=item.title,Url=item.url,Source=DailyContent.WebUrl(item.source)?item.source:endpoint,Published=date.UtcDateTime});
        }
        return result.GroupBy(item=>item.Url).Select(g=>g.First()).OrderByDescending(item=>item.Published).ToList();
    }
    async Task<string> Download(string endpoint){
        if(!DailyContent.WebUrl(endpoint))throw new IOException("新闻接口需使用 HTTPS，本机开发允许 HTTP");
        using(var cancel=new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        using(var response=await http.GetAsync(endpoint,HttpCompletionOption.ResponseHeadersRead,cancel.Token)){
            response.EnsureSuccessStatusCode();if(!DailyContent.WebUrl(response.RequestMessage.RequestUri.AbsoluteUri))throw new IOException("接口跳转地址不合法");
            using(var input=await response.Content.ReadAsStreamAsync())using(var output=new MemoryStream()){
                byte[] buffer=new byte[8192];int n;
                while((n=await input.ReadAsync(buffer,0,buffer.Length,cancel.Token))>0){if(output.Length+n>2097152)throw new IOException("新闻接口返回超过 2 MB");output.Write(buffer,0,n);}
                return new UTF8Encoding(false,true).GetString(output.ToArray());
            }
        }
    }
    internal async Task Refresh(){
        if(disposed||busy)return;busy=true;attempted=DateTime.UtcNow;string fallback="";
        try{
            // A legacy startup download may still be finishing when attached.
            var active=typeof(DailyContent).GetField("updating",BindingFlags.NonPublic|BindingFlags.Instance);
            while((bool)active.GetValue(content)){if(disposed)return;await Task.Delay(100);}
            if(!string.IsNullOrWhiteSpace(Options.Endpoint)){
                try{
                    var items=Parse(await Download(Options.Endpoint),Options.Endpoint);if(disposed)return;
                    Data.Items=items;Data.Attempted=attempted;Data.Downloaded=DateTime.UtcNow;
                    Data.Status="自有新闻接口 · "+items.Count+" 条 · 每 "+Options.RefreshMinutes+" 分钟刷新";
                    if(items.Count>0&&(DateTime.UtcNow-items[0].Published).TotalHours>48)Data.Status+=" · 来源超过 48 小时无新条目";
                    if(!string.IsNullOrWhiteSpace(Data.HotwordUrl))try{content.ApplyPack(await Download(Data.HotwordUrl),true);}catch(Exception e){Data.Status+=" · 热词更新失败："+e.GetBaseException().Message;}
                    content.Save();Notify();return;
                }catch(Exception e){
                    if(disposed)return;fallback="自有新闻接口不可用："+e.GetBaseException().Message;
                    if(!Options.Fallback){Data.Status=fallback+"；保留缓存";content.Save();Notify();return;}
                }
            }
            await content.Update();if(disposed)return;
            Data.Status=(fallback.Length>0?fallback+"；已回退免费 RSS。":"免费公开 RSS · ")+Data.Status+" · 每 "+Options.RefreshMinutes+" 分钟刷新";
            content.Save();Notify();
        }finally{busy=false;}
    }
    void Notify(){if(!disposed&&Changed!=null)Changed();}
    public void Dispose(){disposed=true;timer.Dispose();http.Dispose();if(panel!=null)panel.Dispose();}
}
internal sealed class RealtimeNewsPanel:Form {
    readonly RealtimeNews news;
    readonly TextBox chinese=new TextBox(),english=new TextBox(),endpoint=new TextBox(),hot=new TextBox();
    readonly CheckBox enabled=new CheckBox{Text="自动刷新",AutoSize=true},fallback=new CheckBox{Text="自有接口失败时使用免费 RSS",AutoSize=true};
    readonly NumericUpDown interval=new NumericUpDown{Minimum=1,Maximum=60,Width=52};
    readonly Button refresh=new Button{Text="立即刷新",AutoSize=true};
    readonly ListView list=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,HideSelection=false};
    readonly Label status=new Label{Dock=DockStyle.Bottom,Height=68,Padding=new Padding(8)};
    internal RealtimeNewsPanel(RealtimeNews model){
        news=model;Text="数萌 AI · 实时新闻与中英文热词";Size=new Size(980,690);MinimumSize=new Size(850,560);BackColor=Color.White;Font=new Font("Microsoft YaHei UI",10);StartPosition=FormStartPosition.CenterScreen;
        var top=new TableLayoutPanel{Dock=DockStyle.Top,Height=224,ColumnCount=2,Padding=new Padding(10)};
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140));top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Add(top,"免费中文 RSS",chinese,0);Add(top,"免费英文 RSS",english,1);Add(top,"自有新闻接口",endpoint,2);Add(top,"自有热词接口",hot,3);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};actions.Controls.Add(enabled);actions.Controls.Add(interval);actions.Controls.Add(new Label{Text="分钟",AutoSize=true,Padding=new Padding(0,4,0,0)});actions.Controls.Add(fallback);actions.Controls.Add(refresh);
        var personal=new Button{Text="个人词库",AutoSize=true};actions.Controls.Add(personal);personal.Click+=(s,e)=>{using(var form=new PersonalPhrases())form.ShowDialog(this);};top.Controls.Add(actions,1,4);
        var hint=new Label{Text="默认免费直连；自有接口留空即可。新闻按来源发布节奏更新，断网仍可查看缓存。",Dock=DockStyle.Fill,ForeColor=Color.DimGray};top.Controls.Add(hint,1,5);
        list.Columns.Add("发布日期",145);list.Columns.Add("新闻标题（双击打开原文）",580);list.Columns.Add("来源",205);
        Controls.Add(list);Controls.Add(status);Controls.Add(top);
        chinese.Text=news.Data.ChineseFeed;english.Text=news.Data.EnglishFeed;hot.Text=news.Data.HotwordUrl;endpoint.Text=news.Options.Endpoint;enabled.Checked=news.Data.Enabled;fallback.Checked=news.Options.Fallback;interval.Value=news.Options.RefreshMinutes;
        enabled.CheckedChanged+=(s,e)=>Save();fallback.CheckedChanged+=(s,e)=>Save();interval.ValueChanged+=(s,e)=>Save();
        refresh.Click+=async(s,e)=>{Save();refresh.Enabled=false;try{await news.Refresh();}finally{if(!IsDisposed)refresh.Enabled=true;}};
        foreach(var box in new[]{chinese,english,hot,endpoint})box.Leave+=(s,e)=>Save();
        list.DoubleClick+=(s,e)=>{if(list.SelectedItems.Count>0){var item=(NewsItem)list.SelectedItems[0].Tag;if(DailyContent.WebUrl(item.Url))System.Diagnostics.Process.Start(item.Url);}};
        news.Changed+=Render;Render();FormClosing+=(s,e)=>{if(e.CloseReason==CloseReason.UserClosing){Save();e.Cancel=true;Hide();}};
    }
    static void Add(TableLayoutPanel panel,string title,TextBox box,int row){panel.RowStyles.Add(new RowStyle(SizeType.Absolute,34));panel.Controls.Add(new Label{Text=title,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,row);box.Dock=DockStyle.Fill;panel.Controls.Add(box,1,row);}
    void Save(){news.Data.Enabled=enabled.Checked;news.Data.ChineseFeed=chinese.Text.Trim();news.Data.EnglishFeed=english.Text.Trim();news.Data.HotwordUrl=hot.Text.Trim();news.Options.Endpoint=endpoint.Text.Trim();news.Options.Fallback=fallback.Checked;news.Options.RefreshMinutes=(int)interval.Value;news.Save();}
    void Render(){
        if(IsDisposed)return;list.BeginUpdate();list.Items.Clear();foreach(var item in news.Data.Items){var row=new ListViewItem(item.Published.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));row.SubItems.Add(item.Title);row.SubItems.Add(new Uri(item.Source).Host);row.Tag=item;list.Items.Add(row);}list.EndUpdate();
        status.Text="最近成功获取："+(news.Data.Downloaded==default(DateTime)?"无":news.Data.Downloaded.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))+" · 本地热词："+news.Data.HotVersion+"\r\n"+news.Data.Status;
    }
    protected override void Dispose(bool disposing){if(disposing)news.Changed-=Render;base.Dispose(disposing);}
}
#if WININTEL_REALTIME
internal static class RealtimeProgram {
    [STAThread]static void Main(string[] args){
        bool created;using(var mutex=new Mutex(true,"Local\\winintel-ime-windows03",out created)){
            if(!created){try{using(var signal=EventWaitHandle.OpenExisting("Local\\winintel-ime-show03"))signal.Set();}catch{}return;}
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            var controller=new Controller(Array.IndexOf(args,"--show")>=0,Array.IndexOf(args,"--quick")>=0,false);
            var fields=BindingFlags.NonPublic|BindingFlags.Instance;
            var daily=(DailyContent)typeof(Controller).GetField("daily",fields).GetValue(controller);
            using(var news=new RealtimeNews(daily)){
                var quick=(QuickAccess)typeof(Controller).GetField("quick",fields).GetValue(controller);
                var handlers=(Action)typeof(QuickAccess).GetField("NewsRequested",fields).GetValue(quick);
                if(handlers!=null)foreach(Action action in handlers.GetInvocationList())quick.NewsRequested-=action;
                quick.NewsRequested+=news.Show;
                if(Array.IndexOf(args,"--news")>=0)news.Show();
                Application.Idle+=(s,e)=>news.Start(false);
                Application.Run(controller);
            }
        }
    }
}
#endif
}
