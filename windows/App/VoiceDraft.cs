// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Winintel.Windows {
internal sealed class VoiceDraft {
    readonly List<string> history=new List<string>();
    internal string Text {get;private set;}
    internal VoiceDraft(){Text="";}
    internal void Edit(string value){value=value??"";if(value==Text)return;history.Add(Text);if(history.Count>100)history.RemoveAt(0);Text=value;}
    internal void Clear(){Edit("");}
    internal void Undo(){if(history.Count==0)return;Text=history[history.Count-1];history.RemoveAt(history.Count-1);}
    internal void DeleteSentence(){
        string value=Text.TrimEnd();if(value.Length==0)return;
        int end=value.Length-1;while(end>=0&&"。！？.!?\r\n".IndexOf(value[end])>=0)end--;
        int previous=end;while(previous>=0&&"。！？.!?\r\n".IndexOf(value[previous])<0)previous--;
        if(previous<0){
            // ASR can omit punctuation. Preserve preceding completed utterances.
            for(int i=history.Count-1;i>=0;i--){
                string prior=history[i];
                if(prior.Length>0&&prior.Length<value.Length&&value.StartsWith(prior,StringComparison.Ordinal)){Edit(prior);return;}
            }
        }
        Edit(previous<0?"":value.Substring(0,previous+1));
    }
    static string JoinTechnicalTerms(string value){
        // A pause can divide one acronym or domain between recognition segments.
        const RegexOptions options=RegexOptions.IgnoreCase|RegexOptions.CultureInvariant;
        const string left=@"(?<![A-Za-z0-9_])",right=@"(?![A-Za-z0-9_])";
        value=Regex.Replace(value,left+@"w\s*i\s*n\s*i\s*n\s*t\s*e\s*l(?:\s*(?:点|\.|dot)\s*|\s+)c\s*o\s*m"+right,"winintel.com",options);
        value=Regex.Replace(value,left+@"(?:n\s*v\s*l\s*i\s*n\s*k|envy\s+link)"+right,"NVLink",options);
        return Regex.Replace(value,left+@"(?:s\s*x\s*m|x\s*s\s*m)\s*([2345二三四五])"+right,
            m=>"SXM"+("二三四五".IndexOf(m.Groups[1].Value,StringComparison.Ordinal)>=0?
                ("二三四五".IndexOf(m.Groups[1].Value,StringComparison.Ordinal)+2).ToString():m.Groups[1].Value),options);
    }
    internal string Accept(string utterance){
        string value=(utterance??"").Trim(' ','\t','\r');if(value.Length==0)return "";
        // Only a complete utterance beginning with this prefix can edit the draft.
        Match prefix=Regex.Match(value,@"^(?:数萌指令|输入法指令)[，,：:\s]*(.*)$");
        if(!prefix.Success){bool spacing=Text.Length>0&&Regex.IsMatch(Text,@"[A-Za-z0-9,.!?]$")&&Regex.IsMatch(value,@"^[A-Za-z0-9]");Edit(JoinTechnicalTerms(Text+(spacing?" ":"")+value));return "";}
        string command=prefix.Groups[1].Value.Trim().TrimEnd('。','！','!','?','？','.');
        string compact=Regex.Replace(command,@"[\s，,：:]","");
        if(compact=="清空"||compact=="清除"||compact=="清空文字"||compact=="清除文字"){Clear();return "已清空草稿，可撤销";}
        if(compact=="撤销"||compact=="撤销上一步"){Undo();return "已撤销";}
        if(compact=="删除上一句"||compact=="删除最后一句"){DeleteSentence();return "已删除上一句";}
        Match replace=Regex.Match(command,@"^把(.+?)(?:改成|替换为)(.+)$");
        if(replace.Success){
            string from=replace.Groups[1].Value.Trim(' ','，',',','“','”','"','。');
            string to=replace.Groups[2].Value.Trim(' ','，',',','“','”','"','。');
            if(from.Length>0&&to.Length>0&&Text.Contains(from)){Edit(Text.Replace(from,to));return "已替换草稿中的文字";}
            return "草稿中未找到要替换的文字";
        }
        return "未识别此指令；可说：输入法指令，清空";
    }
}
}
