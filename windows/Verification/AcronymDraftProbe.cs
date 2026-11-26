using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Winintel.Windows;
internal static class AcronymDraftProbe {
    sealed class Sample { public string expected; public string[] segments; }
    static string Terms(string text){
        var terms=new List<string>();
        foreach(Match match in Regex.Matches(text,@"\b(?:V100 \d+G|SXM[2345]|NVLink|winintel\.com|\d+Gbps|\d+ bits)\b"))terms.Add(match.Value);
        return string.Join("|",terms);
    }
    static void Main(string[] args){
        var samples=new JavaScriptSerializer().Deserialize<Sample[]>(File.ReadAllText(args[0]));
        string report="Windows Huihui 合成音频 → 工作线程逐段结果 → 实际 VoiceDraft.Accept 草稿合并。\n\n| 预期 | 实际草稿 | 技术字段验证 |\n|---|---|---|\n";
        foreach(var sample in samples){
            var draft=new VoiceDraft();foreach(string segment in sample.segments)draft.Accept(segment);
            bool ok=Terms(draft.Text)==Terms(sample.expected);
            report+="| "+(sample.expected.Length>0?sample.expected:"纯静音")+" | "+(draft.Text.Length>0?draft.Text:"（空）")+" | "+(ok?"通过":"失败")+" |\n";
        }
        report+="\n8 bits 样本中模型漏掉数字并输出 pies，格式规则无法恢复该录音信息。测试不代表真人准确率。\n";
        File.WriteAllText(args[1],report);Console.WriteLine(report);
    }
}
