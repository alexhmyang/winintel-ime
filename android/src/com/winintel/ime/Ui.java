package com.winintel.ime;
import android.content.Context;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.view.*;
import android.widget.*;
final class Ui {
    static final int INK=Color.rgb(25,45,43), GREEN=Color.rgb(17,121,104), BG=Color.rgb(235,243,240);
    static int dp(Context c,int n){return (int)(n*c.getResources().getDisplayMetrics().density+0.5f);}
    static LinearLayout column(Context c){LinearLayout l=new LinearLayout(c);l.setOrientation(1);return l;}
    static TextView text(Context c,String s,int size){
        TextView t=new TextView(c);t.setText(s);t.setTextSize(size);t.setTextColor(INK);t.setPadding(dp(c,12),dp(c,8),dp(c,12),dp(c,8));return t;
    }
    static Button button(Context c,String s,View.OnClickListener click){
        Button b=new Button(c);b.setText(s);b.setTextSize(16);b.setAllCaps(false);b.setTextColor(INK);
        GradientDrawable bg=new GradientDrawable();bg.setColor(Color.WHITE);bg.setCornerRadius(dp(c,8));
        b.setBackground(bg);b.setMinWidth(0);b.setMinimumWidth(0);b.setMinHeight(0);b.setMinimumHeight(0);
        b.setPadding(0,0,0,0);b.setOnClickListener(click);return b;
    }
}
