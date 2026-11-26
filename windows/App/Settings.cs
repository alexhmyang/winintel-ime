// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
namespace Winintel.Windows {
internal sealed class Settings {
    public uint Microphone=0xffffffff;
    internal static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"winintel-ime-native");
    internal static Settings Load() {try {return new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(Path.Combine(Folder,"settings.json")));}catch{return new Settings();}}
    internal void Save(){Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"settings.json"),new JavaScriptSerializer().Serialize(this),Encoding.UTF8);}
}
}
