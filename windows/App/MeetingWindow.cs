// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
namespace Winintel.Windows {
internal sealed class MeetingWindow:Form {
    readonly Meeting meeting;readonly Translation translator;readonly Action<Action> dispatch;
    readonly TextBox source=new TextBox{Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
    readonly TextBox translated=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
    readonly TextBox captions=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill};
    readonly ComboBox direction=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=120};
    readonly CheckBox live=new CheckBox{Text="自动翻译",Checked=true,AutoSize=true,Margin=new Padding(14,12,8,4)};
    readonly Label status=new Label{Dock=DockStyle.Bottom,Height=36,Padding=new Padding(24,8,8,0),ForeColor=Skin.Muted};
    int revision;string partial="";
    readonly Timer liveTimer=new Timer{Interval=650};
    string submitted="";int liveEpoch,sequence,displayed;Caption latestCaption;
    bool recording;
    internal string Direction {get{return direction.SelectedIndex==1?"zh-en":direction.SelectedIndex==2?"en-zh":"auto";}}
    internal bool Live {get{return live.Checked;}}
    internal event Action RecordRequested;
    internal MeetingWindow(Meeting data,Translation engine,Action<Action> ui){
        meeting=data;translator=engine;dispatch=ui;Text="数萌 AI · 实时语音翻译";
        ClientSize=new Size(900,610);MinimumSize=new Size(800,540);Font=new Font("Microsoft YaHei UI",10);StartPosition=FormStartPosition.CenterScreen;BackColor=Color.White;AutoScaleMode=AutoScaleMode.Dpi;
        var heading=new System.Windows.Forms.Panel{Dock=DockStyle.Top,Height=86,BackColor=Color.White};
        var title=Skin.Label("实时语音翻译",21,true);title.Location=new Point(26,17);heading.Controls.Add(title);
        var subtitle=Skin.Label("边说边译  ·  自动判断中英文  ·  本机离线处理");subtitle.Location=new Point(28,59);subtitle.ForeColor=Skin.Muted;heading.Controls.Add(subtitle);
        var tabs=new MaterialTabs{Dock=DockStyle.Fill};var textTab=new MaterialPage("文字翻译"){Padding=new Padding(20)};var captionTab=new MaterialPage("字幕与会议"){Padding=new Padding(20)};
        Skin.Editor(source);Skin.Editor(translated);Skin.Editor(captions);
        var split=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1};split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
        var sourceCard=new MaterialCard{Dock=DockStyle.Fill,Margin=new Padding(0,0,8,0)};
        var targetCard=new MaterialCard{Dock=DockStyle.Fill,Margin=new Padding(8,0,0,0)};
        var sourceTitle=Skin.Label("原文",11,true);sourceTitle.Dock=DockStyle.Top;sourceTitle.AutoSize=false;sourceTitle.Height=36;
        var targetTitle=Skin.Label("译文",11,true);targetTitle.Dock=DockStyle.Top;targetTitle.AutoSize=false;targetTitle.Height=36;targetTitle.ForeColor=Skin.Blue;
        sourceCard.Controls.Add(source);sourceCard.Controls.Add(sourceTitle);targetCard.Controls.Add(translated);targetCard.Controls.Add(targetTitle);split.Controls.Add(sourceCard,0,0);split.Controls.Add(targetCard,1,0);textTab.Controls.Add(split);
        var captionCard=new MaterialCard{Dock=DockStyle.Fill};captionCard.Controls.Add(captions);captionTab.Controls.Add(captionCard);tabs.TabPages.AddRange(new[]{textTab,captionTab});
        var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=56,Padding=new Padding(20,4,12,4),WrapContents=false,BackColor=Color.White};
        direction.Items.AddRange(new object[]{"自动判断","中文 → 英文","英文 → 中文"});direction.SelectedIndex=0;
        direction.Margin=new Padding(4,8,8,4);direction.Width=128;
        var translate=Skin.Button("翻译",true);translate.Width=82;var copy=Skin.Button("复制译文");copy.Width=94;
        var record=Skin.Button("录音 F8");record.Width=102;var srt=Skin.Button("导出 SRT");var notes=Skin.Button("导出会议记录");var clear=Skin.Button("新建会议");var retry=Skin.Button("补译字幕");
        bar.Controls.AddRange(new Control[]{direction,translate,copy,record,live});
        var captionActions=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(0,8,0,0),WrapContents=false};captionActions.Controls.AddRange(new Control[]{srt,notes,retry,clear});captionTab.Controls.Add(captionActions);
        Controls.Add(tabs);Controls.Add(bar);Controls.Add(heading);Controls.Add(status);
        Shown+=(s,e)=>Skin.RoundWindow(this);
        source.TextChanged+=(s,e)=>revision++;
        direction.SelectedIndexChanged+=(s,e)=>{revision++;ResetLive();translated.Clear();foreach(var c in meeting.Captions){c.Target="";c.Error="";if(Live)Translate(c);}if(partial.Length>0)liveTimer.Start();};
        live.CheckedChanged+=(s,e)=>{ResetLive();if(Live&&partial.Length>0)liveTimer.Start();};
        liveTimer.Tick+=(s,e)=>TranslatePreview();
        translate.Click+=(s,e)=>{
            if(string.IsNullOrWhiteSpace(source.Text))return;int version=revision;
            Status("正在本机翻译，首次使用需要加载模型…");
            if(!translator.Enqueue(source.Text,Direction,(text,error)=>dispatch(()=>{
                if(IsDisposed||Disposing)return;
                if(version!=revision){Status("原文已修改，请重新翻译");return;}
                translated.Text=error.Length==0?text:"";Status(error.Length==0?"翻译完成（本机 CPU）":error);
            })))Status("翻译队列已满，请稍后重试");
        };
        copy.Click+=(s,e)=>{if(translated.Text.Length>0)Clipboard.SetText(translated.Text);};
        record.Click+=(s,e)=>{if(RecordRequested!=null)RecordRequested();};
        srt.Click+=(s,e)=>Export(false);notes.Click+=(s,e)=>Export(true);
        clear.Click+=(s,e)=>{if(recording){Status("请先按 F8 停止录音，再新建会议");return;}if(MessageBox.Show(this,"清空当前会议字幕并开始新记录？请先导出需要保留的内容。","新建会议",MessageBoxButtons.OKCancel)==DialogResult.OK){meeting.Clear();partial="";Render();}};
        retry.Click+=(s,e)=>{foreach(var c in meeting.Captions.Where(c=>c.Target.Length==0).ToArray())Translate(c);};
        FormClosing+=(s,e)=>{if(e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
        Status("F8 开始 / 结束录音；实时译文随讲话更新，停顿后整句复核。");
    }
    void ResetLive(){liveTimer.Stop();translator.CancelPreview();liveEpoch++;submitted="";displayed=0;}
    internal void Add(Caption caption){ResetLive();latestCaption=caption;source.Text=caption.Source;translated.Clear();Render();if(Live)Translate(caption);}
    internal void Preview(string text){
        partial=text??"";
        if(partial.Length==0){ResetLive();}else if(Live){source.Text=partial;if(!liveTimer.Enabled)liveTimer.Start();}
        Render();
    }
    void TranslatePreview(){
        if(!Live||partial.Length<2||partial==submitted)return;
        submitted=partial;int epoch=liveEpoch,id=++sequence;string input=partial;string dir=Direction;
        Status("正在实时翻译 · 本机 CPU");
        if(!translator.EnqueuePreview(input,dir,(text,error)=>dispatch(()=>{
            if(IsDisposed||Disposing||epoch!=liveEpoch||id<=displayed)return;
            displayed=id;if(error.Length==0)translated.Text=text;
            Status(error.Length==0?"实时译文 · 停顿后将进行整句复核":error);
        })))submitted="";
    }
    internal void Recording(bool value){recording=value;if(!value)liveTimer.Stop();}
    void Translate(Caption caption){
        if(caption.Error=="正在翻译")return;
        int generation=meeting.Generation,epoch=liveEpoch;string dir=Direction;caption.Error="正在翻译";Status("正在翻译字幕…");
        if(!translator.Enqueue(caption.Source,dir,(text,error)=>dispatch(()=>{
            if(IsDisposed||Disposing||generation!=meeting.Generation||dir!=Direction)return;
            caption.Target=text;caption.Error=error;
            if(caption==latestCaption&&partial.Length==0)translated.Text=error.Length==0?text:"";
            Render();Status(error.Length==0?"整句译文已复核（本机离线）":error);
        }))){caption.Error="队列已满，可点击补译字幕";Render();}
    }
    protected override void Dispose(bool disposing){if(disposing){ResetLive();liveTimer.Dispose();}base.Dispose(disposing);}
    internal void Render(){
        if(IsDisposed||Disposing)return;
        captions.Text=string.Join("\r\n\r\n",meeting.Captions.OrderBy(c=>c.Start).Select(c=>"["+TimeSpan.FromSeconds(c.Start).ToString(@"hh\:mm\:ss")+"] "+c.Source+"\r\n"+(c.Target.Length>0?c.Target:c.Error)))+(partial.Length>0?"\r\n\r\n（实时识别）"+partial:"");
        captions.SelectionStart=captions.TextLength;captions.ScrollToCaret();
    }
    void Status(string value){if(!IsDisposed&&!Disposing)status.Text=value;}
    void Export(bool notes){
        if(meeting.Captions.Count==0){Status("请先录音生成字幕");return;}
        if(meeting.Captions.Any(c=>c.Error=="正在翻译")){Status("翻译尚在处理，请完成后导出");return;}
        using(var dialog=new SaveFileDialog{Filter=notes?"Markdown 会议记录|*.md":"字幕文件|*.srt",FileName="数萌-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+(notes?".md":".srt")}){
            if(dialog.ShowDialog(this)!=DialogResult.OK)return;
            try{File.WriteAllText(dialog.FileName,notes?meeting.Notes():meeting.Srt(),new UTF8Encoding(true));Status("已保存："+dialog.FileName);}catch(Exception e){Status("保存失败："+e.Message);}
        }
    }
}
}
