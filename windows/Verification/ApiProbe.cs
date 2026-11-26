// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Winintel.Windows;
internal static class ApiProbe {
    static int Main(string[] args){return Verify(args[0]).GetAwaiter().GetResult();}
    static async Task<int> Verify(string output){
        var server=new HttpListener();server.Prefixes.Add("http://127.0.0.1:65031/");server.Start();
        var mock=Task.Run(async()=>{
            var context=await server.GetContextAsync();string request;using(var reader=new StreamReader(context.Request.InputStream))request=await reader.ReadToEndAsync();
            if(!request.Contains("test-model")||!request.Contains("hello"))throw new Exception("API payload");
            byte[] body=Encoding.UTF8.GetBytes("{\"choices\":[{\"message\":{\"content\":\"连接成功\"}}]}");context.Response.ContentType="application/json";await context.Response.OutputStream.WriteAsync(body,0,body.Length);context.Response.Close();
        });
        var settings=new Settings{Endpoint="http://127.0.0.1:65031/v1",Model="test-model"};
        string result=await ModelApi.Request(settings,"hello","hello");await mock;server.Stop();
        bool parsed=result=="连接成功",fallback=false;
        settings.Endpoint="http://127.0.0.1:65032/v1";try{await ModelApi.Request(settings,"hello","hello");}catch{fallback=true;}
        var stalled=new HttpListener();stalled.Prefixes.Add("http://127.0.0.1:65033/");stalled.Start();settings.Endpoint="http://127.0.0.1:65033/v1";
        var waiting=stalled.GetContextAsync();var watch=System.Diagnostics.Stopwatch.StartNew();bool bounded=false;
        try{await ModelApi.Request(settings,"hello","hello");}catch{bounded=watch.ElapsedMilliseconds>=7000&&watch.ElapsedMilliseconds<11000;}stalled.Stop();
        File.WriteAllText(output,"| 场景 | 结果 |\n|---|---|\n| OpenAI 格式请求与响应 | "+(parsed?"通过":"失败")+" |\n| 连接失败返回本地回退路径 | "+(fallback?"通过":"失败")+" |\n| 8 秒超时释放后台请求 | "+(bounded?"通过":"失败")+" |\n");
        return parsed&&fallback&&bounded?0:1;
    }
}
