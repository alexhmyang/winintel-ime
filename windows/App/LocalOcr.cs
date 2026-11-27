// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
namespace Winintel.Windows {
internal sealed class OcrHit {internal string Text;internal Rectangle Bounds;}
internal static class LocalOcr {
    [StructLayout(LayoutKind.Sequential)]struct WindowRect{public int L,T,R,B;}
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out WindowRect r);
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
    [DllImport("user32.dll")]static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")]static extern uint GetDpiForWindow(IntPtr window);
    internal static Rectangle Bounds(IntPtr window){
        var previous=SetThreadDpiAwarenessContext(new IntPtr(-4));
        try{WindowRect r;if(!GetWindowRect(window,out r))throw new InvalidOperationException("目标窗口已关闭。");return Rectangle.FromLTRB(r.L,r.T,r.R,r.B);}
        finally{if(previous!=IntPtr.Zero)SetThreadDpiAwarenessContext(previous);}
    }
    internal static double Scale(IntPtr window){uint dpi=GetDpiForWindow(window);return Math.Max(1,dpi/96.0);}
    internal static async Task<List<OcrHit>> Read(Rectangle area){
        using(var image=new Bitmap(area.Width,area.Height))
        using(var graphics=Graphics.FromImage(image)){
            graphics.CopyFromScreen(area.Location,Point.Empty,area.Size);
            return await Recognize(image,area.Location);
        }
    }
    internal static async Task<List<OcrHit>> ReadWindow(IntPtr window,Rectangle area){
        var bounds=Bounds(window);
        using(var full=new Bitmap(bounds.Width,bounds.Height))
        using(var g=Graphics.FromImage(full)){
            var previous=SetThreadDpiAwarenessContext(new IntPtr(-4));
            var dc=g.GetHdc();bool rendered;try{rendered=PrintWindow(window,dc,2);}finally{g.ReleaseHdc(dc);if(previous!=IntPtr.Zero)SetThreadDpiAwarenessContext(previous);}
            if(!rendered)throw new InvalidOperationException("无法读取目标窗口，请使用软件内的搜索。");
            var crop=Rectangle.Intersect(new Rectangle(area.X-bounds.Left,area.Y-bounds.Top,area.Width,area.Height),new Rectangle(Point.Empty,full.Size));
            if(crop.IsEmpty)throw new InvalidOperationException("搜索区域不可见。");
            using(var image=full.Clone(crop,PixelFormat.Format32bppArgb))return await Recognize(image,new Point(bounds.Left+crop.X,bounds.Top+crop.Y));
        }
    }
    static async Task<List<OcrHit>> Recognize(Bitmap image,Point origin){
        var engine=OcrEngine.TryCreateFromLanguage(new Language("zh-Hans"))??OcrEngine.TryCreateFromUserProfileLanguages();
        if(engine==null)throw new InvalidOperationException("请在 Windows 安装中文 OCR 语言包。");
        using(var encoded=new MemoryStream()){
            image.Save(encoded,ImageFormat.Bmp);
            using(var stream=new InMemoryRandomAccessStream()){
                using(var writer=new DataWriter(stream.GetOutputStreamAt(0))){
                    writer.WriteBytes(encoded.ToArray());await writer.StoreAsync();await writer.FlushAsync();writer.DetachStream();
                }
                var decoder=await BitmapDecoder.CreateAsync(stream);
                using(var bitmap=await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied)){
                    var result=await engine.RecognizeAsync(bitmap);var hits=new List<OcrHit>();
                    foreach(var line in result.Lines){
                        string text="";Rectangle bounds=Rectangle.Empty;
                        foreach(var word in line.Words){text+=word.Text;var r=word.BoundingRect;var b=new Rectangle(origin.X+(int)r.X,origin.Y+(int)r.Y,(int)Math.Ceiling(r.Width),(int)Math.Ceiling(r.Height));bounds=bounds.IsEmpty?b:Rectangle.Union(bounds,b);}
                        hits.Add(new OcrHit{Text=text,Bounds=bounds});
                    }return hits;
                }
            }
        }
    }
}
}
