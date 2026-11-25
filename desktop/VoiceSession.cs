// GPL-3.0-or-later. 16 kHz microphone capture and local streaming recognition.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
namespace Winintel.Desktop {
internal sealed class VoiceSession : IDisposable {
    [StructLayout(LayoutKind.Sequential,Pack=2)] struct Format { public ushort tag,channels; public uint rate,bytes; public ushort align,bits,extra; }
    [StructLayout(LayoutKind.Sequential)] struct Header { public IntPtr data; public uint length,recorded; public UIntPtr user; public uint flags,loops; public IntPtr next; public UIntPtr reserved; }
    delegate void Callback(IntPtr device,uint message,UIntPtr instance,IntPtr header,IntPtr unused);
    [DllImport("winmm.dll")] static extern uint waveInOpen(out IntPtr device,uint id,ref Format format,Callback callback,UIntPtr instance,uint flags);
    [DllImport("winmm.dll")] static extern uint waveInPrepareHeader(IntPtr device,IntPtr header,uint size);
    [DllImport("winmm.dll")] static extern uint waveInUnprepareHeader(IntPtr device,IntPtr header,uint size);
    [DllImport("winmm.dll")] static extern uint waveInAddBuffer(IntPtr device,IntPtr header,uint size);
    [DllImport("winmm.dll")] static extern uint waveInStart(IntPtr device);
    [DllImport("winmm.dll")] static extern uint waveInReset(IntPtr device);
    [DllImport("winmm.dll")] static extern uint waveInClose(IntPtr device);
    readonly ClientWebSocket socket=new ClientWebSocket();
    readonly CancellationTokenSource cancel=new CancellationTokenSource();
    readonly BlockingCollection<byte[]> audio=new BlockingCollection<byte[]>(12);
    readonly List<IntPtr> headers=new List<IntPtr>(),buffers=new List<IntPtr>();
    readonly object captureLock=new object();
    readonly Callback callback;
    readonly Action<string,string> report;
    IntPtr device;
    volatile bool recording;
    int disposed,stopped;
    internal VoiceSession(Action<string,string> onResult) {report=onResult;callback=OnAudio;}
    internal async Task Start(int port,string token) {
        socket.Options.AddSubProtocol("winintel."+token);
        cancel.CancelAfter(5000);
        await socket.ConnectAsync(new Uri("ws://127.0.0.1:"+port+"/ws/voice"),cancel.Token);
        await SendControl("start");
        var result=await Receive(socket,cancel.Token);
        if((string)result["type"]!="started") throw new IOException(result.ContainsKey("error")?(string)result["error"]:"语音模型未就绪");
        cancel.CancelAfter(Timeout.Infinite);
        Format format=new Format {tag=1,channels=1,rate=16000,bytes=32000,align=2,bits=16};
        Check(waveInOpen(out device,0xffffffff,ref format,callback,UIntPtr.Zero,0x30000));
        uint size=(uint)Marshal.SizeOf(typeof(Header));
        for(int i=0;i<4;i++) {
            IntPtr buffer=Marshal.AllocHGlobal(3200),header=Marshal.AllocHGlobal((int)size);
            buffers.Add(buffer);headers.Add(header);
            Marshal.StructureToPtr(new Header {data=buffer,length=3200},header,false);
            Check(waveInPrepareHeader(device,header,size));Check(waveInAddBuffer(device,header,size));
        }
        recording=true;Check(waveInStart(device));
        Task.Run((Func<Task>)SendLoop);
        Task.Run((Func<Task>)ReceiveLoop);
    }
    static void Check(uint result) { if(result!=0) throw new IOException("无法打开麦克风（"+result+"）。请检查 Windows 麦克风权限。"); }
    void OnAudio(IntPtr handle,uint message,UIntPtr instance,IntPtr header,IntPtr unused) {
        if(message!=0x3c0 || !recording || !System.Threading.Monitor.TryEnter(captureLock)) return;
        try {
            if(!recording) return;
            Header wave=(Header)Marshal.PtrToStructure(header,typeof(Header));
            byte[] pcm=new byte[wave.recorded];Marshal.Copy(wave.data,pcm,0,pcm.Length);
            float[] floats=new float[pcm.Length/2];
            for(int i=0;i<floats.Length;i++) floats[i]=(short)(pcm[i*2]|pcm[i*2+1]<<8)/32768f;
            byte[] bytes=new byte[floats.Length*4];Buffer.BlockCopy(floats,0,bytes,0,bytes.Length);
            if(bytes.Length>0 && !audio.TryAdd(bytes)) {
                recording=false;
                Task.Run(delegate {report("error","处理速度不足，请停止录音后重试。");Dispose();});
                return;
            }
            if(recording) waveInAddBuffer(handle,header,(uint)Marshal.SizeOf(typeof(Header)));
        } catch(Exception) {cancel.Cancel();}
        finally {System.Threading.Monitor.Exit(captureLock);}
    }
    internal void Stop() {
        if(Interlocked.Exchange(ref stopped,1)!=0) return;
        CloseMicrophone();audio.CompleteAdding();
        cancel.CancelAfter(15000);
    }
    void CloseMicrophone() {
        recording=false;
        lock(captureLock) {
            if(device!=IntPtr.Zero) {
                waveInReset(device);
                foreach(IntPtr header in headers) waveInUnprepareHeader(device,header,(uint)Marshal.SizeOf(typeof(Header)));
                waveInClose(device);device=IntPtr.Zero;
            }
            foreach(IntPtr header in headers) Marshal.FreeHGlobal(header);
            foreach(IntPtr buffer in buffers) Marshal.FreeHGlobal(buffer);
            headers.Clear();buffers.Clear();
        }
    }
    async Task SendControl(string type) {
        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"type\":\""+type+"\"}")),WebSocketMessageType.Text,true,cancel.Token);
    }
    async Task SendLoop() {
        try {
            foreach(byte[] bytes in audio.GetConsumingEnumerable(cancel.Token))
                await socket.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Binary,true,cancel.Token);
            await SendControl("finish");
        } catch(OperationCanceledException) {} catch(Exception e) {report("error",e.Message);Dispose();}
    }
    async Task ReceiveLoop() {
        try {
            while(!cancel.IsCancellationRequested) {
                var result=await Receive(socket,cancel.Token);
                string kind=(string)result["type"];
                report(kind,result.ContainsKey("text")?(string)result["text"]:result.ContainsKey("error")?(string)result["error"]:"");
                if(kind=="error" || (result.ContainsKey("finished") && (bool)result["finished"])) break;
            }
        } catch(OperationCanceledException) {}
          catch(Exception e) {report("error",e.Message);}
        finally {Dispose();report("done","");}
    }
    internal static async Task<Dictionary<string,object>> Receive(ClientWebSocket client,CancellationToken token) {
        using(MemoryStream data=new MemoryStream()) {
            byte[] buffer=new byte[4096];WebSocketReceiveResult result;
            do {
                result=await client.ReceiveAsync(new ArraySegment<byte>(buffer),token);
                if(result.MessageType==WebSocketMessageType.Close) throw new IOException("本地语音连接已关闭");
                data.Write(buffer,0,result.Count);
                if(data.Length>16384) throw new IOException("语音响应超出限制");
            } while(!result.EndOfMessage);
            return new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(data.ToArray()));
        }
    }
    public void Dispose() {
        if(Interlocked.Exchange(ref disposed,1)!=0) return;
        cancel.Cancel();CloseMicrophone();socket.Dispose();
        if(!audio.IsAddingCompleted) audio.CompleteAdding();
    }
}
}
