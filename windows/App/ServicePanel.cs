// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
namespace Winintel.Windows {
internal sealed class ServicePanel:Form {
    readonly ServiceOptions options=ServiceOptions.Load();
    readonly TextBox endpoint=new TextBox(),lexicon=new TextBox(),token=new TextBox{UseSystemPasswordChar=true};
    readonly CheckBox enabled=new CheckBox(),sponsor=new CheckBox();
    readonly Label status=new Label();
    internal ServicePanel(){
        Text="服务与赞助";ClientSize=new Size(610,515);Font=new Font("Microsoft YaHei UI",10);BackColor=Color.White;StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;
        var title=Skin.Label("免费开源 · 企业服务",17,true);title.SetBounds(20,16,570,32);Controls.Add(title);
        var model=Skin.Label("社区版永久免费。企业可购买词库托管、部署与技术支持。");model.SetBounds(20,59,570,26);Controls.Add(model);
        var contact=Skin.Button("复制服务邮箱");contact.SetBounds(20,94,155,34);contact.Click+=(s,e)=>{Clipboard.SetText("service@winintel.com");status.Text="已复制 service@winintel.com，可咨询托管与企业定制。";};Controls.Add(contact);
        enabled.Text="启用词库 API（仅手动同步）";enabled.SetBounds(20,149,550,28);enabled.Checked=options.LexiconEnabled;Controls.Add(enabled);
        Field("服务地址",endpoint,189);endpoint.Text=options.Endpoint;endpoint.MaxLength=1024;
        Field("词库 ID",lexicon,230);lexicon.Text=options.LexiconId;lexicon.MaxLength=64;
        Field("访问密钥",token,271);token.Text=options.Token;token.MaxLength=256;
        var hint=Skin.Label("只下载词库，不上传输入内容。未配置服务时继续使用离线词库。",9);hint.SetBounds(20,313,570,28);hint.ForeColor=Skin.Muted;Controls.Add(hint);
        sponsor.Text="允许工具箱显示标明“广告”的赞助信息";sponsor.SetBounds(20,351,565,28);sponsor.Checked=options.SponsorEnabled;Controls.Add(sponsor);
        var save=Skin.Button("保存设置");save.SetBounds(20,397,130,36);Controls.Add(save);save.Click+=(s,e)=>{try{Save();status.Text="已保存。赞助内容只在工具箱显示。";}catch(Exception ex){status.Text=ex.Message;}};
        var sync=Skin.Button("同步词库",true);sync.SetBounds(163,397,130,36);Controls.Add(sync);
        var clear=Skin.Button("移除 API 词库");clear.SetBounds(306,397,145,36);Controls.Add(clear);
        clear.Click+=(s,e)=>{try{LocalFiles.Replace(Path.Combine(Settings.Folder,"api-phrases.tsv"),"");status.Text="已移除下载的 API 词库，个人词库与选词习惯保留。";}catch(IOException ex){status.Text=ex.Message;}};
        sync.Click+=async(s,e)=>{
            sync.Enabled=save.Enabled=clear.Enabled=false;status.Text="正在下载词库…";
            try{Save();int count=await LexiconClient.Sync(options);if(!IsDisposed)status.Text="已同步 "+count+" 个词条，可离线使用，候选立即更新。";}
            catch(Exception ex){if(!IsDisposed)status.Text="同步失败，已有词库保留："+ex.Message;}
            finally{if(!IsDisposed)sync.Enabled=save.Enabled=clear.Enabled=true;}
        };
        status.SetBounds(20,450,570,54);status.ForeColor=Skin.Muted;Controls.Add(status);
    }
    void Field(string label,TextBox editor,int y){var text=Skin.Label(label);text.SetBounds(20,y,90,28);Controls.Add(text);editor.SetBounds(113,y,472,28);Controls.Add(editor);}
    void Save(){options.LexiconEnabled=enabled.Checked;options.SponsorEnabled=sponsor.Checked;options.Endpoint=endpoint.Text.Trim();options.LexiconId=lexicon.Text.Trim();options.Token=token.Text;options.Save();}
}
}
