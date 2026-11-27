// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import android.content.Context;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.graphics.drawable.StateListDrawable;
import android.content.res.ColorStateList;
import android.view.*;
import android.widget.*;
final class Ui {
    static final int INK=Color.rgb(32,33,36), GREEN=Color.rgb(0,122,78), BG=Color.rgb(238,244,240);
    private static GradientDrawable surface(Context c,int color){
        GradientDrawable d=new GradientDrawable();d.setColor(color);d.setCornerRadius(dp(c,8));return d;
    }
    static void colors(Button b,int normal){
        b.setTextColor(new ColorStateList(new int[][]{new int[]{android.R.attr.state_pressed},new int[]{android.R.attr.state_selected},new int[]{}},new int[]{Color.WHITE,Color.WHITE,normal}));
    }
    static int dp(Context c,int n){return (int)(n*c.getResources().getDisplayMetrics().density+0.5f);}
    static LinearLayout column(Context c){LinearLayout l=new LinearLayout(c);l.setOrientation(1);return l;}
    static TextView text(Context c,String s,int size){
        TextView t=new TextView(c);t.setText(s);t.setTextSize(size);t.setTextColor(INK);t.setPadding(dp(c,12),dp(c,8),dp(c,12),dp(c,8));return t;
    }
    static Button button(Context c,String s,View.OnClickListener click){
        Button b=new Button(c);b.setText(s);b.setTextSize(18);b.setAllCaps(false);b.setTextColor(INK);
        StateListDrawable bg=new StateListDrawable();
        bg.addState(new int[]{android.R.attr.state_pressed},surface(c,Color.rgb(0,87,56)));
        bg.addState(new int[]{android.R.attr.state_selected},surface(c,GREEN));
        bg.addState(new int[]{},surface(c,Color.WHITE));colors(b,INK);
        b.setBackground(bg);b.setMinWidth(0);b.setMinimumWidth(0);b.setMinHeight(0);b.setMinimumHeight(0);
        b.setFocusable(false);b.setPadding(0,0,0,0);b.setOnClickListener(click);return b;
    }
}
