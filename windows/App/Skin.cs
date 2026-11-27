// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace Winintel.Windows {
internal static class Skin {
    internal static readonly Color Ink=Color.FromArgb(32,33,36),Muted=Color.FromArgb(95,99,104),Blue=Color.FromArgb(26,115,232),Surface=Color.FromArgb(248,249,250),Outline=Color.FromArgb(218,220,224);
    internal static GraphicsPath Round(Rectangle r,int radius){
        var path=new GraphicsPath();int d=radius*2;
        path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;
    }
    internal static Label Label(string text,int size=10,bool bold=false){return new Label{Text=text,AutoSize=true,ForeColor=Ink,Font=new Font("Microsoft YaHei UI",size,bold?FontStyle.Bold:FontStyle.Regular),BackColor=Color.Transparent};}
    internal static MaterialButton Button(string text,bool primary=false){return new MaterialButton{Text=text,Primary=primary,Size=new Size(112,36),Margin=new Padding(4)};}
    internal static void Editor(TextBox box){box.BorderStyle=BorderStyle.None;box.BackColor=Color.White;box.ForeColor=Ink;box.Font=new Font("Microsoft YaHei UI",11);}
    [DllImport("dwmapi.dll")]static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
    internal static void RoundWindow(Form form){try{int value=2;DwmSetWindowAttribute(form.Handle,33,ref value,4);}catch{}}
}
internal sealed class MaterialButton:Button {
    internal bool Primary,Warm;bool hover;
    internal MaterialButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;ForeColor=Skin.Blue;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Font=new Font("Microsoft YaHei UI",10);}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?Color.White:Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        using(var path=Skin.Round(new Rectangle(1,1,Width-3,Height-3),9)){
            using(var brush=new SolidBrush(!Enabled?Color.FromArgb(241,243,244):Primary?(Warm?QuickStyle.Orange:hover?Color.FromArgb(24,100,201):Skin.Blue):(hover?(Warm?QuickStyle.Pale:Color.FromArgb(232,240,254)):Color.White)))e.Graphics.FillPath(brush,path);
            if(!Primary)using(var pen=new Pen(Skin.Outline))e.Graphics.DrawPath(pen,path);
        }
        TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,!Enabled?Color.FromArgb(154,160,166):Primary?Color.White:Warm?Skin.Muted:Skin.Blue,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-10,Height-10));
    }
}
internal sealed class MaterialCard:System.Windows.Forms.Panel {
    internal MaterialCard(){BackColor=Color.White;Padding=new Padding(18);SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var path=Skin.Round(new Rectangle(0,0,Width-1,Height-1),12))using(var pen=new Pen(Skin.Outline))e.Graphics.DrawPath(pen,path);}
}
internal sealed class MaterialPage:System.Windows.Forms.Panel {
    internal MaterialPage(string title){Text=title;BackColor=Color.White;}
}
internal sealed class MaterialTabs:UserControl {
    internal sealed class Pages {
        readonly MaterialTabs owner;
        internal readonly System.Collections.Generic.List<MaterialPage> Items=new System.Collections.Generic.List<MaterialPage>();
        internal Pages(MaterialTabs tabs){owner=tabs;}
        internal void AddRange(MaterialPage[] pages){foreach(var page in pages){Items.Add(page);page.Dock=DockStyle.Fill;owner.body.Controls.Add(page);owner.AddNavigation(page);}owner.SelectedIndex=0;}
    }
    readonly System.Windows.Forms.Panel body=new System.Windows.Forms.Panel{Dock=DockStyle.Fill,BackColor=Color.White};
    readonly FlowLayoutPanel navigation=new FlowLayoutPanel{Dock=DockStyle.Top,Height=44,WrapContents=false,BackColor=Color.White,Margin=Padding.Empty};
    readonly System.Collections.Generic.List<Button> buttons=new System.Collections.Generic.List<Button>();
    internal readonly Pages TabPages;
    int selected=-1;
    public int TabCount{get{return TabPages.Items.Count;}}
    public int SelectedIndex{get{return selected;}set{if(value<0||value>=TabCount)return;selected=value;for(int i=0;i<TabCount;i++){TabPages.Items[i].Visible=i==value;buttons[i].ForeColor=i==value?Skin.Blue:Skin.Muted;buttons[i].Invalidate();}TabPages.Items[value].BringToFront();}}
    internal MaterialTabs(){BackColor=Color.White;Font=new Font("Microsoft YaHei UI",10);TabPages=new Pages(this);Controls.Add(body);Controls.Add(navigation);}
    void AddNavigation(MaterialPage page){
        int index=buttons.Count;
        var button=new Button{Text=page.Text,Width=150,Height=44,Margin=Padding.Empty,FlatStyle=FlatStyle.Flat,BackColor=Color.White,Font=Font,Cursor=Cursors.Hand};
        button.FlatAppearance.BorderSize=0;button.FlatAppearance.MouseOverBackColor=Color.FromArgb(248,249,250);
        button.Click+=(s,e)=>SelectedIndex=index;
        button.KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Left){SelectedIndex=(selected+(e.KeyCode==Keys.Right?1:TabCount-1))%TabCount;buttons[selected].Focus();e.Handled=true;}};
        button.Paint+=(s,e)=>{if(selected==index)using(var brush=new SolidBrush(Skin.Blue))e.Graphics.FillRectangle(brush,20,button.Height-3,button.Width-40,3);};
        buttons.Add(button);navigation.Controls.Add(button);
    }
}
}
