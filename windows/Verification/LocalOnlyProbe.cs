// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
internal static class LocalOnlyProbe {
    static System.Collections.Generic.IEnumerable<Control> Descendants(Control parent){foreach(Control child in parent.Controls){yield return child;foreach(Control nested in Descendants(child))yield return nested;}}
    static int failures;
    static void Check(bool ok,string label){Console.WriteLine(label+": "+(ok?"PASS":"FAIL"));if(!ok)failures++;}
    [STAThread]static int Main(string[] args){
        Application.EnableVisualStyles();
        var assembly=Assembly.LoadFrom(args[0]);
        var settingsType=assembly.GetType("Winintel.Windows.Settings",true);
        var serializer=new JavaScriptSerializer();
        // Existing users may have previously enabled API ranking; it must stay ignored.
        var settings=serializer.Deserialize("{\"Microphone\":4294967295,\"ApiEnabled\":true,\"Endpoint\":\"https://example.invalid/v1\",\"Model\":\"legacy\",\"Secret\":\"legacy\"}",settingsType);
        Check(settings!=null,"legacy settings still load");
        Check(settingsType.GetField("ApiEnabled")==null&&settingsType.GetField("Endpoint")==null&&settingsType.GetField("Secret")==null,"API configuration fields removed");
        Check(serializer.Serialize(settings).IndexOf("legacy",StringComparison.Ordinal)<0,"saving settings discards legacy endpoint and credentials");
        Check(assembly.GetType("Winintel.Windows.ModelApi")==null&&assembly.GetType("Winintel.Windows.RankBroker")==null,"HTTP model client and ranking broker absent from executable");
        var panelType=assembly.GetType("Winintel.Windows.Panel",true);
        using(var panel=(Form)Activator.CreateInstance(panelType,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{settings,(Action)(()=>{})},null)){
            panel.Show();Application.DoEvents();
            int textBoxes=0;bool remoteControls=false;
            foreach(Control control in Descendants(panel)){
                if(control is TextBox)textBoxes++;
                if(control is CheckBox||control.Text.Contains("\u6d4b\u8bd5\u6a21\u578b\u8fde\u63a5")||control.Text.Contains("API \u5730\u5740"))remoteControls=true;
            }
            Check(!remoteControls&&textBoxes==1,"settings panel only retains input test box and local controls");
            panel.Close();
        }
        return failures==0?0:1;
    }
}
