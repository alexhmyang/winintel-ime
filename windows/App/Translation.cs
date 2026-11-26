// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
namespace Winintel.Windows {
internal sealed class Translation:IDisposable {
    sealed class Work {internal string Text,Direction;internal Action<string,string> Done;internal bool Preview;}
    readonly BlockingCollection<Work> queue=new BlockingCollection<Work>(24);
    readonly Thread worker;
    readonly object gate=new object();
    Process process;volatile bool disposed;
    Work latest;bool previewQueued;
    internal Translation(){worker=new Thread(Run){IsBackground=true,Name="winintel offline translation"};worker.Start();}
    internal bool Enqueue(string text,string direction,Action<string,string> done){
        if(disposed)return false;
        try{return queue.TryAdd(new Work{Text=text,Direction=direction,Done=done});}catch(InvalidOperationException){return false;}
    }
    internal bool EnqueuePreview(string text,string direction,Action<string,string> done){
        lock(gate){
            if(disposed)return false;
            latest=new Work{Text=text,Direction=direction,Done=done};
            if(previewQueued)return true;
            previewQueued=true;
            try{if(queue.TryAdd(new Work{Preview=true}))return true;}catch(InvalidOperationException){}
            previewQueued=false;latest=null;return false;
        }
    }
    internal void CancelPreview(){lock(gate){latest=null;}}
    void Run(){
        foreach(var entry in queue.GetConsumingEnumerable()){
            Work work=entry;
            if(entry.Preview){lock(gate){work=latest;latest=null;previewQueued=false;}if(work==null)continue;}
            if(disposed)break;
            try{
                Process child;
                lock(gate){
                    if(disposed)return;
                    if(process==null||process.HasExited){
                        string root=AppDomain.CurrentDomain.BaseDirectory;
                        process=Process.Start(new ProcessStartInfo(Path.Combine(root,"runtime","node.exe"),"\""+Path.Combine(root,"standalone","translation-host.mjs")+"\""){
                            UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,
                            StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8,WorkingDirectory=root
                        });
                        process.ErrorDataReceived+=(s,e)=>{};process.BeginErrorReadLine();
                    }
                    child=process;
                }
                var json=new JavaScriptSerializer();
                // Write bytes explicitly: .NET Framework's default console encoding can corrupt Chinese.
                byte[] bytes=Encoding.UTF8.GetBytes(json.Serialize(new {id=1,text=work.Text,direction=work.Direction})+"\n");
                child.StandardInput.BaseStream.Write(bytes,0,bytes.Length);child.StandardInput.BaseStream.Flush();
                var read=child.StandardOutput.ReadLineAsync();
                if(!read.Wait(120000)){lock(gate){if(!child.HasExited)child.Kill();}throw new IOException("离线翻译超时，请缩短文字后重试");}
                string line=read.Result;if(line==null)throw new IOException("翻译模型进程已退出");
                var result=json.Deserialize<Dictionary<string,object>>(line);
                if(result.ContainsKey("error"))throw new IOException((string)result["error"]);
                if(!disposed)work.Done((string)result["text"],"");
            }catch(Exception e){if(!disposed)work.Done("",e.GetBaseException().Message);}
        }
    }
    public void Dispose(){disposed=true;queue.CompleteAdding();lock(gate){if(process!=null){try{if(!process.HasExited)process.Kill();}catch{}process.Dispose();}}}
}
}
