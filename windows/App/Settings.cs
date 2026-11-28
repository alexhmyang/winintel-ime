// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
namespace Winintel.Windows {
internal sealed class Settings {
    public uint Microphone=0xffffffff;
    public int CandidateFontSize=20;
    public string IndustryProfile="general";
    public bool CodeAssist=true;
    internal static readonly string Folder=Path.Combine(Environment.GetEnvironmentVariable("LOCALAPPDATA")??Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"winintel-ime-native");
    internal static Settings Load() {try {return new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(Path.Combine(Folder,"settings.json")));}catch{return new Settings();}}
    internal void Save(){CandidateFontSize=Math.Max(16,Math.Min(28,CandidateFontSize));if(Array.IndexOf(new[]{"general","programming","ai","medical","finance","law","education","manufacturing","ecommerce"},IndustryProfile)<0)IndustryProfile="general";Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"settings.json"),new JavaScriptSerializer().Serialize(this),Encoding.UTF8);File.WriteAllText(Path.Combine(Folder,"candidate-ui.ini"),"[Candidate]\r\nFontSize="+CandidateFontSize+"\r\n",Encoding.Unicode);File.WriteAllText(Path.Combine(Folder,"industry.ini"),"[Industry]\r\nProfile="+IndustryProfile+"\r\nCodeAssist="+(CodeAssist?"1":"0")+"\r\n",Encoding.Unicode);}
}
}
