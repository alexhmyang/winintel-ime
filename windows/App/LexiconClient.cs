// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;
using System.Web.Script.Serialization;
namespace Winintel.Windows {
internal sealed class LexiconPack {
    public int schema;
    public string id,name;
    public LexiconEntry[] entries;
}
internal sealed class LexiconEntry {public string text,pinyin;}
internal static class LexiconClient {
    internal static Uri Address(ServiceOptions options){
        if(!options.LexiconEnabled)throw new InvalidOperationException("请先启用手动词库同步。");
        Uri endpoint;
        if(!Uri.TryCreate((options.Endpoint??"").TrimEnd('/')+"/",UriKind.Absolute,out endpoint)||endpoint.UserInfo.Length>0||endpoint.Query.Length>0||endpoint.Fragment.Length>0||
            (endpoint.Scheme!="https"&&!(endpoint.Scheme=="http"&&endpoint.IsLoopback)))throw new ArgumentException("服务地址需要 HTTPS；本机测试可用 http://127.0.0.1:8787。");
        if(!Regex.IsMatch(options.LexiconId??"","^[a-z0-9-]{1,64}$"))throw new ArgumentException("词库 ID 只能包含小写字母、数字和连字符。");
        return new Uri(endpoint,"v1/lexicons/"+options.LexiconId);
    }
    internal static string Validate(LexiconPack pack,string requested,out int count){
        if(pack==null||pack.schema!=1||pack.id!=requested||pack.entries==null||pack.entries.Length>10000)throw new InvalidDataException("词库格式不正确，或超过 10000 个词条。");
        var rows=new HashSet<string>();var result=new StringBuilder();
        foreach(var entry in pack.entries){
            if(entry==null||string.IsNullOrWhiteSpace(entry.text)||entry.text.Length>48||entry.text.Any(char.IsControl)||string.IsNullOrWhiteSpace(entry.pinyin))throw new InvalidDataException("词条文字不正确。");
            var spelling=entry.pinyin.ToLowerInvariant().Replace('ü','v');
            if(!Regex.IsMatch(spelling,@"^[a-z]+(?: [a-z]+)*$"))throw new InvalidDataException("词条拼音必须用空格分隔，并使用英文字母。");
            var code=spelling.Replace(" ","");if(code.Length>256)throw new InvalidDataException("词条拼音过长。");
            var row=code+"\t"+entry.text.Trim();if(rows.Add(row))result.AppendLine(row);
        }count=rows.Count;return result.ToString();
    }
    internal static async Task<int> Sync(ServiceOptions options){
        var address=Address(options);
        using(var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15)))
        using(var handler=new HttpClientHandler{AllowAutoRedirect=false})
        using(var client=new HttpClient(handler){Timeout=TimeSpan.FromSeconds(15)})
        using(var request=new HttpRequestMessage(HttpMethod.Get,address)){
            if(!string.IsNullOrEmpty(options.Token))request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",options.Token);
            using(var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,deadline.Token).ConfigureAwait(false)){
                response.EnsureSuccessStatusCode();
                const int limit=2*1024*1024;
                if(response.Content.Headers.ContentLength>limit)throw new InvalidDataException("词库超过 2 MB。");
                using(var input=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using(var output=new MemoryStream()){
                    var buffer=new byte[8192];int bytes;
                    while((bytes=await input.ReadAsync(buffer,0,buffer.Length,deadline.Token).ConfigureAwait(false))>0){if(output.Length+bytes>limit)throw new InvalidDataException("词库超过 2 MB。");output.Write(buffer,0,bytes);}
                    var serializer=new JavaScriptSerializer{MaxJsonLength=limit};
                    var pack=serializer.Deserialize<LexiconPack>(new UTF8Encoding(false,true).GetString(output.ToArray()));int count;
                    var text=Validate(pack,options.LexiconId,out count);
                    LocalFiles.Replace(Path.Combine(Settings.Folder,"api-phrases.tsv"),text);return count;
                }
            }
        }
    }
}
}
