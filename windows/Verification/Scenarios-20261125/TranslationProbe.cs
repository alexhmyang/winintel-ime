using System;
using System.Threading;
using Winintel.Windows;
internal static class TranslationProbe {
    static int count;
    static void Run(Translation engine,string source,string direction,Func<string,bool> check){
        string value="",error="";using(var done=new ManualResetEvent(false)){
            if(!engine.Enqueue(source,direction,(text,failure)=>{value=text;error=failure;done.Set();}))throw new Exception("Queue rejected");
            if(!done.WaitOne(120000))throw new Exception("Callback timeout");
        }
        if(error.Length>0||!check(value))throw new Exception(source+" => "+value+" "+error);
        Console.WriteLine("PASS "+(++count)+": "+value);
    }
    static int Main(){
      try {
        using(var engine=new Translation()){
            Run(engine,"订单金额1250元，包含运费。","zh-en",s=>s.Contains("1250 yuan")&&!s.Contains("$"));
            Run(engine,"The price is $25.","en-zh",s=>s.Contains("25美元"));
            Run(engine,"退款金额为-1250元。","zh-en",s=>s.Contains("-1250 yuan"));
            Run(engine,"部署 DeepSeek 模型，使用 vLLM 测试并发性能。","zh-en",s=>s.Contains("DeepSeek")&&s.Contains("vLLM")&&s.Contains("concurrent performance"));
            Run(engine,"We run on the CPU without using a GPU.","en-zh",s=>s.Contains("CPU")&&s.Contains("GPU")&&(s.Contains("不")||s.Contains("没有")));
            Run(engine,"The link is 56Gbps and storage writes at 56GBps.","en-zh",s=>s.Contains("56Gbps")&&s.Contains("56GBps"));
        }
        return 0;
      } catch(Exception e) {
        Console.WriteLine("FAIL "+e.GetType().Name+": "+e.Message);
        System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"translation-probe-error.txt"),e.Message);
        return 1;
      }
    }
}
