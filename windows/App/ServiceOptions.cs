// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
namespace Winintel.Windows {
internal sealed class ServiceOptions {
    public bool LexiconEnabled=false,SponsorEnabled=false;
    public string Endpoint="",LexiconId="hardware",ProtectedToken="";
    internal static ServiceOptions Load(){try{return new JavaScriptSerializer().Deserialize<ServiceOptions>(File.ReadAllText(Path.Combine(Settings.Folder,"services.json")))??new ServiceOptions();}catch{return new ServiceOptions();}}
    internal void Save(){Directory.CreateDirectory(Settings.Folder);LocalFiles.Replace(Path.Combine(Settings.Folder,"services.json"),new JavaScriptSerializer().Serialize(this));}
    internal string Token {
        get{try{return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(ProtectedToken),null,DataProtectionScope.CurrentUser));}catch{return "";}}
        set{ProtectedToken=string.IsNullOrEmpty(value)?"":Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value),null,DataProtectionScope.CurrentUser));}
    }
}
internal static class LocalFiles {
    internal static void Replace(string path,string text){
        Directory.CreateDirectory(Path.GetDirectoryName(path));var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{File.WriteAllText(temporary,text,new UTF8Encoding(false));if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);}
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
}
}
