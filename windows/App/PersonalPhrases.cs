// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
namespace Winintel.Windows {
internal sealed class PersonalPhrases : Form {
    readonly TextBox phrase=new TextBox(),pinyin=new TextBox();
    readonly ListBox entries=new ListBox();
    readonly Label status=new Label();
    readonly Dictionary<char,string[]> readings=new Dictionary<char,string[]>();
    static readonly string Store=Path.Combine(Settings.Folder,"personal-phrases.tsv");
    internal PersonalPhrases(){
        Text="个人词库";ClientSize=new Size(550,420);Font=new Font("Microsoft YaHei UI",10);BackColor=Color.White;StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;
        var help=Skin.Label("保存姓名或专业词，完整拼音和简拼都能找到。");help.SetBounds(18,16,514,28);Controls.Add(help);
        var nameLabel=Skin.Label("词语");nameLabel.SetBounds(18,55,60,28);Controls.Add(nameLabel);
        phrase.SetBounds(85,55,435,28);phrase.MaxLength=48;Controls.Add(phrase);
        var codeLabel=Skin.Label("拼音");codeLabel.SetBounds(18,94,60,28);Controls.Add(codeLabel);
        pinyin.SetBounds(85,94,435,28);pinyin.MaxLength=256;Controls.Add(pinyin);
        var hint=Skin.Label("自动填入读音；多音字请修改，如：yang hui ming");hint.SetBounds(85,128,440,26);hint.ForeColor=Skin.Muted;Controls.Add(hint);
        var save=Skin.Button("保存词语",true);save.SetBounds(85,161,125,36);Controls.Add(save);
        var remove=Skin.Button("删除选中");remove.SetBounds(223,161,125,36);Controls.Add(remove);
        entries.SetBounds(18,211,502,142);Controls.Add(entries);
        status.SetBounds(18,367,510,40);status.ForeColor=Skin.Muted;Controls.Add(status);
        try{
            foreach(var row in File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"character-readings.tsv"),Encoding.UTF8)){
                var cells=row.Split('\t');if(cells.Length==2&&cells[0].Length==1)readings[cells[0][0]]=cells[1].Split(' ');
            }
        }catch(IOException){}
        phrase.TextChanged+=(s,e)=>{
            var codes=new List<string>();foreach(char ch in phrase.Text){string[] options;if(!readings.TryGetValue(ch,out options)){pinyin.Text="";return;}codes.Add(options[0]);}
            pinyin.Text=string.Join(" ",codes.ToArray());
        };
        save.Click+=(s,e)=>SavePhrase();
        remove.Click+=(s,e)=>{var selected=entries.SelectedItem as string;if(selected==null)return;try{Change(rows=>rows.Remove(selected));Reload();status.Text="已从个人词库删除；已有选词记忆仍保留。";}catch(IOException ex){status.Text=ex.Message;}};
        Reload();
    }
    void Reload(){entries.Items.Clear();if(File.Exists(Store))foreach(var row in File.ReadAllLines(Store,Encoding.UTF8))if(row.Contains("\t"))entries.Items.Add(row);}
    static void Change(Action<List<string>> edit){
        Directory.CreateDirectory(Settings.Folder);
        using(var mutex=new Mutex(false,"Local\\winintel-personal-phrases")){
            bool acquired=false;
            try{
                try{acquired=mutex.WaitOne(2000);}catch(AbandonedMutexException){acquired=true;}
                if(!acquired)throw new IOException("个人词库正在更新，请稍后重试。");
                var rows=File.Exists(Store)?File.ReadAllLines(Store,Encoding.UTF8).ToList():new List<string>();edit(rows);
                var temporary=Store+"."+Guid.NewGuid().ToString("N")+".tmp";
                try{File.WriteAllLines(temporary,rows,new UTF8Encoding(false));if(File.Exists(Store))File.Replace(temporary,Store,null);else File.Move(temporary,Store);}
                finally{if(File.Exists(temporary))File.Delete(temporary);}
            }finally{if(acquired)mutex.ReleaseMutex();}
        }
    }
    void SavePhrase(){
        var text=phrase.Text.Trim();var syllables=pinyin.Text.Trim().ToLowerInvariant().Replace('ü','v').Split(new[]{' ','\''},StringSplitOptions.RemoveEmptyEntries);
        if(text.Length<2||text.Length>48||syllables.Length!=text.Length||syllables.Any(s=>s.Any(ch=>ch<'a'||ch>'z'))||string.Concat(syllables).Length>256||text.Any(ch=>ch=='\t'||ch=='\r'||ch=='\n')){
            status.Text="请输入词语，以及每字一个拼音（用空格分隔）。";return;
        }
        for(int i=0;i<text.Length;i++){string[] options;if(!readings.TryGetValue(text[i],out options)||!options.Contains(syllables[i])){status.Text="请检查第 "+(i+1)+" 个字的拼音。";return;}}
        var code=string.Concat(syllables);var abbreviation=string.Concat(syllables.Select(s=>s[0].ToString()));
        try{Change(rows=>{rows.RemoveAll(r=>r==code+"\t"+text);rows.Add(code+"\t"+text);});Reload();status.Text="已保存：输入 "+abbreviation+" 或 "+code+" 即可选词。";}
        catch(IOException ex){status.Text=ex.Message;}
    }
}
}
