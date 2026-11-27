// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace Winintel.Windows {
internal sealed class Caption {
    internal long Id;internal double Start,End;internal string Source="",Target="",Error="";
}
internal sealed class Meeting {
    internal readonly List<Caption> Captions=new List<Caption>();
    internal double Duration {get {return Captions.Count==0?0:Captions.Max(c=>c.End);}}
    internal int Generation;long next;
    internal void Clear(){Captions.Clear();next=0;Generation++;}
    internal Caption Add(double start,double end,string text){
        var caption=new Caption{Id=++next,Start=Math.Max(0,start),End=Math.Max(start+0.1,end),Source=text};
        Captions.Add(caption);return caption;
    }
    static string Time(double seconds){
        var span=TimeSpan.FromMilliseconds(Math.Max(0,Math.Round(seconds*1000)));
        return string.Format("{0:00}:{1:00}:{2:00},{3:000}",(int)span.TotalHours,span.Minutes,span.Seconds,span.Milliseconds);
    }
    internal string Srt(){
        var result=new StringBuilder();int i=0;double end=0;
        foreach(var caption in Captions.OrderBy(c=>c.Start).ThenBy(c=>c.Id)){
            double start=Math.Max(end,caption.Start);end=Math.Max(start+0.1,caption.End);
            result.Append(++i).Append("\r\n").Append(Time(start)).Append(" --> ").Append(Time(end)).Append("\r\n")
                .Append(Regex.Replace(caption.Source,@"[\r\n]+"," ")).Append("\r\n");
            if(caption.Target.Length>0)result.Append(Regex.Replace(caption.Target,@"[\r\n]+"," ")).Append("\r\n");
            result.Append("\r\n");
        }
        return result.ToString();
    }
    internal string Notes(){
        var result=new StringBuilder("# 会议记录\r\n\r\n");
        result.Append("导出时间：").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("\r\n\r\n");
        result.Append("记录来源：本机麦克风离线识别；时间为录音分段时间。以下决策与待办按关键词提取，请核对上下文。\r\n\r\n");
        result.Append("## 决策与待办摘录\r\n\r\n");
        var actions=Captions.Where(c=>Regex.IsMatch(c.Source,@"决定|确认|负责|截止|待办|下周|明天|需要|行动|decid|agreed|action|deadline|responsible|tomorrow",RegexOptions.IgnoreCase)).ToList();
        if(actions.Count==0)result.Append("本次记录没有匹配到决策或待办关键词。\r\n");
        foreach(var c in actions)result.Append("- [").Append(Time(c.Start)).Append("] ").Append(c.Source).Append("\r\n");
        result.Append("\r\n## 逐句记录\r\n\r\n");
        foreach(var c in Captions.OrderBy(c=>c.Start).ThenBy(c=>c.Id)){
            result.Append("[").Append(Time(c.Start)).Append(" – ").Append(Time(c.End)).Append("] ").Append(c.Source).Append("\r\n");
            if(c.Target.Length>0)result.Append("译文：").Append(c.Target).Append("\r\n");
            if(c.Error.Length>0)result.Append("翻译状态：").Append(c.Error).Append("\r\n");
            result.Append("\r\n");
        }
        return result.ToString();
    }
}
}
