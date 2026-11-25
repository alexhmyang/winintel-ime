// GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace Winintel.Desktop {
internal sealed class Runtime : IDisposable {
    internal readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
    internal readonly string Data=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"winintel-ime");
    internal int Port;
    internal string Token="";
    internal string Error="";
    Process server,bridge;
    ChildJob childJob;
    readonly object logLock=new object();
    readonly JavaScriptSerializer json=new JavaScriptSerializer();
    internal bool AiEnabled;
    internal string Model="qwen3:0.6b";
    internal Runtime() {
        Directory.CreateDirectory(Data);
        try {
            var settings=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(Data,"desktop-settings.json")));
            AiEnabled=(bool)settings["ai"];Model=(string)settings["model"];
        } catch {}
    }
    internal void Save() {
        File.WriteAllText(Path.Combine(Data,"desktop-settings.json"),json.Serialize(new {ai=AiEnabled,model=Model}));
    }
    internal void Log(string text) {
        if(String.IsNullOrEmpty(text)) return;
        lock(logLock) {
            string path=Path.Combine(Data,"desktop.log");
            try {
                if(File.Exists(path) && new FileInfo(path).Length>1024*1024) File.Move(path,path+"."+DateTime.Now.Ticks);
                File.AppendAllText(path,DateTime.Now.ToString("s")+" "+text+Environment.NewLine);
            } catch {}
        }
    }
    Process Start(string script,string directory) {
        string node=Path.Combine(Root,"runtime","node.exe");
        ProcessStartInfo info=new ProcessStartInfo(node,"\""+Path.Combine(Root,script)+"\"") {
            WorkingDirectory=Root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
        };
        info.EnvironmentVariables["PORT"]=Port.ToString();
        info.EnvironmentVariables["WININTEL_DATA_DIR"]=directory;
        info.EnvironmentVariables["AI_ENABLED"]=AiEnabled?"1":"0";
        info.EnvironmentVariables["OLLAMA_MODEL"]=Model;
        using(var key=Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Rime\Weasel")) {
            if(key!=null) {
                string rime=key.GetValue("RimeUserDir") as string;
                if(!String.IsNullOrEmpty(rime)) info.EnvironmentVariables["RIME_USER_DIR"]=Environment.ExpandEnvironmentVariables(rime);
            }
        }
        // Do not inherit developer proxy/TLS overrides into the customer runtime.
        info.EnvironmentVariables.Remove("NODE_TLS_REJECT_UNAUTHORIZED");
        info.EnvironmentVariables["NODE_OPTIONS"]="";
        Process process=new Process {StartInfo=info};
        process.OutputDataReceived+=(s,e)=>Log(e.Data);
        process.ErrorDataReceived+=(s,e)=>Log(e.Data);
        process.Start();childJob.Add(process);process.BeginOutputReadLine();process.BeginErrorReadLine();return process;
    }
    internal async Task Start() {
        childJob=new ChildJob();
        TcpListener listener=new TcpListener(IPAddress.Loopback,0);listener.Start();
        Port=((IPEndPoint)listener.LocalEndpoint).Port;listener.Stop();
        string directory=Path.Combine(Data,"desktop-runtime");
        Directory.CreateDirectory(directory);
        string session=Path.Combine(directory,"session.json");if(File.Exists(session)) File.Delete(session);
        server=Start("src/server.js",directory);
        for(int attempt=0;attempt<200;attempt++) {
            if(server.HasExited) throw new IOException("本地引擎启动失败，请查看日志。");
            if(File.Exists(session)) {
                try {
                    var value=json.Deserialize<Dictionary<string,object>>(File.ReadAllText(session));
                    if(Convert.ToInt32(value["port"])==Port) {Token=(string)value["token"];break;}
                } catch {}
            }
            await Task.Delay(100);
        }
        if(Token.Length==0) throw new IOException("本地引擎启动超时。");
        bridge=Start("src/rime-bridge.js",directory);
    }
    internal async Task<Dictionary<string,object>> Status() {
        using(WebClient client=new WebClient()) {
            client.Headers["X-Winintel-Token"]=Token;
            string text=await client.DownloadStringTaskAsync("http://127.0.0.1:"+Port+"/api/status");
            return json.Deserialize<Dictionary<string,object>>(text);
        }
    }
    internal async Task Configure() {
        ProcessStartInfo info=new ProcessStartInfo("powershell.exe","-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \""+Path.Combine(Root,"desktop","configure-ime.ps1")+"\"") {
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Root
        };
        using(Process process=new Process {StartInfo=info}) {
            process.Start();
            Task<string> output=process.StandardOutput.ReadToEndAsync(),error=process.StandardError.ReadToEndAsync();
            await Task.Run(delegate {if(!process.WaitForExit(240000)) {process.Kill();throw new IOException("输入法部署超时，请重试。");}});
            Log(await output);string message=await error;Log(message);
            if(process.ExitCode!=0) throw new IOException("系统输入法配置失败，请查看日志。"+message);
        }
    }
    public void Dispose() {
        foreach(Process process in new [] {bridge,server}) {
            if(process==null) continue;
            try {if(!process.HasExited) {process.Kill();process.WaitForExit(3000);}process.Dispose();} catch {}
        }
        bridge=null;server=null;Token="";
        if(childJob!=null) {childJob.Dispose();childJob=null;}
    }
}
}
