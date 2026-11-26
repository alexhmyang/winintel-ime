using System;
using System.IO;
using Winintel.Windows;
internal static class VoiceDraftProbe {
    static int checks;static void Check(bool value,string name){checks++;if(!value)throw new Exception(name);}
    static void Main(string[] args){
        var draft=new VoiceDraft();
        draft.Accept("第一句。");draft.Accept("第二句。");
        draft.Accept("输入法指令，删除上一句。");Check(draft.Text=="第一句。","delete sentence");
        draft.Accept("输入法指令，撤销。");Check(draft.Text=="第一句。第二句。","undo delete");
        draft.Accept("输入法指令，清空文字。");Check(draft.Text=="","clear");
        draft.Undo();Check(draft.Text=="第一句。第二句。","undo clear");
        draft.Accept("输入法指令，把第一句改成新的句子。");Check(draft.Text=="新的句子。第二句。","replace");
        draft.Accept("输入法指令，把没有的字改成测试。");Check(draft.Text=="新的句子。第二句。","replace missing");
        draft.Accept("请不要清空这些文字。");Check(draft.Text.EndsWith("请不要清空这些文字。"),"literal clear");
        draft.Accept("我说输入法指令清空只是举例。");Check(draft.Text.EndsWith("我说输入法指令清空只是举例。"),"literal prefix");
        string before=draft.Text;draft.Accept("输入法指令，未知操作。");Check(draft.Text==before,"unknown command");
        draft.Clear();draft.Accept("测试");draft.Accept("测试");Check(draft.Text=="测试 测试","intentional repetition");
        draft.Edit("手动编辑");draft.Undo();Check(draft.Text=="测试 测试","manual undo");
        draft.Clear();draft.Accept("没有句号的文字");draft.DeleteSentence();Check(draft.Text=="","unpunctuated sentence");
        draft.Accept("上一句话");draft.Accept("最后一句话");draft.DeleteSentence();Check(draft.Text=="上一句话","unpunctuated segments");
        draft.Clear();draft.Accept("我们的网站叫 w i n intel");draft.Accept("com");Check(draft.Text=="我们的网站叫 winintel.com","domain across endpoint");
        draft.Undo();Check(draft.Text=="我们的网站叫 w i n intel","domain joining undo");
        draft.Accept("点 com");Check(draft.Text=="我们的网站叫 winintel.com","domain dot across endpoint");
        draft.Clear();draft.Accept("n v");draft.Accept("link");draft.Accept("envy link");Check(draft.Text=="NVLink NVLink","acronym across endpoint and repetition");
        draft.Clear();draft.Accept("V100 32G s x");draft.Accept("m 三");Check(draft.Text=="V100 32G SXM3","sxm across endpoint");
        draft.Clear();draft.Accept("winintel company envy linkage");Check(draft.Text=="winintel company envy linkage","term boundaries");
        draft.Clear();draft.Accept("网址w i n intel 点 com");Check(draft.Text=="网址winintel.com","Chinese adjacent boundary");
        File.WriteAllText(args[0],"| 项目 | 结果 |\n|---|---|\n| 清空、撤销、删除句子、替换、普通口述保护、重复文字、手动编辑 | "+checks+" 项通过 |\n");
    }
}
