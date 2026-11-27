// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Drawing;
using System.Windows.Forms;
namespace Winintel.Windows {
internal sealed class EmojiPalette:PassivePopup {
    readonly FlowLayoutPanel symbols=new FlowLayoutPanel{AutoScroll=true,WrapContents=true};
    readonly Label status=new Label();
    readonly Timer timer=new Timer{Interval=200};
    IntPtr target;
    internal event Action<IntPtr,string> Selected;
    internal static readonly string[] Categories={"常用","心情","手势","生活","符号","颜文字"};
    internal static readonly string[][] Items={
        new[]{"😀","😊","😂","🤣","🥰","😍","😎","🤔","😭","👍","🙏","❤️","🎉","🔥","✅","💯","🚀","🌹"},
        new[]{"😄","😁","😅","😉","🙃","😋","😘","🥳","😴","😢","😡","😱","😤","🥺","🤗","🤩","🤐","🤓"},
        new[]{"👋","👌","✌️","🤝","👏","💪","🙌","🤞","🫶","👀","🤙","👎","✋","☝️","👉","👇"},
        new[]{"☀️","🌙","⭐","🌈","☕","🍵","🍎","🍰","🎂","🎁","💻","📱","📚","💡","🔒","📌","📝","📅","⏰","🏠","🚗","✈️"},
        new[]{"★","☆","✓","✗","√","×","÷","±","≈","≠","≤","≥","∞","℃","℉","°","㎡","㎥","©","®","™","→","←","↑","↓","↔","●","○","■","□","▲","△","·","…","—","「","」","『","』"},
        new[]{"(＾▽＾)","(｡♥‿♥｡)","(≧▽≦)","( •̀ ω •́ )✧","(╯°□°）╯︵ ┻━┻","┬─┬ノ( º _ ºノ)","¯\\_(ツ)_/¯","(；′⌒`)","(￣▽￣)","(ง •̀_•́)ง","ヽ(✿ﾟ▽ﾟ)ノ","(๑•̀ㅂ•́)و✧"}
    };
    internal EmojiPalette(){
        Text="数萌 AI · 表情与符号";ClientSize=new Size(500,332);
        var title=Skin.Label("表情与符号",12,true);title.SetBounds(14,12,420,26);Controls.Add(title);
        var close=new QuickButton{Symbol="close",Location=new Point(465,10),Size=new Size(26,26)};close.Click+=(s,e)=>Hide();Controls.Add(close);
        var tabs=new FlowLayoutPanel{Location=new Point(10,46),Size=new Size(480,36),WrapContents=false};Controls.Add(tabs);
        for(int i=0;i<Categories.Length;i++){int index=i;var b=new QuickButton{Text=Categories[i],Width=76,Height=30};b.Click+=(s,e)=>ShowCategory(index);tabs.Controls.Add(b);}
        symbols.SetBounds(10,91,480,198);Controls.Add(symbols);
        status.SetBounds(14,297,470,28);status.Text="点击即输入，保留当前软件焦点。";status.ForeColor=Skin.Muted;Controls.Add(status);
        timer.Tick+=(s,e)=>{if(Visible&&TextInput.GetForegroundWindow()!=target)Hide();};timer.Start();ShowCategory(0);
    }
    void ShowCategory(int index){
        while(symbols.Controls.Count>0)symbols.Controls[0].Dispose();
        foreach(var item in Items[index]){
            var text=item;var b=new QuickButton{Text=text,Width=index==5?222:56,Height=index==5?44:48,Font=new Font(index==5?"Microsoft YaHei UI":"Segoe UI Emoji",index==5?11:19),AccessibleName=text};
            b.Click+=(s,e)=>{if(TextInput.Ready(target)){if(Selected!=null)Selected(target,text);}else{status.Text="请重新点击输入框，再打开表情面板。";}};
            symbols.Controls.Add(b);
        }
    }
    internal void Open(IntPtr destination){target=destination;if(!TextInput.Ready(target))return;var work=Screen.FromHandle(target).WorkingArea;Location=new Point(Math.Max(work.Left,work.Right-Width-12),Math.Max(work.Top,work.Bottom-Height-58));Show();}
    protected override void Dispose(bool disposing){if(disposing)timer.Dispose();base.Dispose(disposing);}
}
}
