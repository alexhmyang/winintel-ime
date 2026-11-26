// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Web.Script.Serialization;
namespace Winintel.Windows {
internal sealed class SponsorPlacement {
    public string label,url,expires;
    internal static SponsorPlacement Load(){
        if(!ServiceOptions.Load().SponsorEnabled)return null;
        try{
            var value=new JavaScriptSerializer().Deserialize<SponsorPlacement>(File.ReadAllText(Path.Combine(Settings.Folder,"sponsor.json")));
            Uri address;DateTime expiry;
            if(value==null||string.IsNullOrWhiteSpace(value.label)||value.label.Length>40||
                !Uri.TryCreate(value.url,UriKind.Absolute,out address)||address.Scheme!="https"||address.UserInfo.Length>0||
                !DateTime.TryParse(value.expires,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal,out expiry)||expiry<=DateTime.UtcNow)return null;
            return value;
        }catch{return null;}
    }
}
}
