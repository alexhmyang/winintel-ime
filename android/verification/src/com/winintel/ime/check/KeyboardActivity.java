// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime.check;
import android.app.*;
import android.os.*;
import android.text.*;
import android.view.inputmethod.*;
import android.widget.*;

/** A test host for chat actions; never sends a message. */
public final class KeyboardActivity extends Activity {
    public void onCreate(Bundle state){
        super.onCreate(state);if(getActionBar()!=null)getActionBar().hide();LinearLayout root=new LinearLayout(this);root.setOrientation(1);root.setPadding(24,120,24,24);
        TextView result=new TextView(this);result.setText("Keyboard test host");root.addView(result);
        EditText text=new EditText(this);text.setTextSize(22);text.setSingleLine(true);text.setHint("CHAT_INPUT");text.setImeOptions(EditorInfo.IME_ACTION_SEND);
        text.setOnEditorActionListener((view,action,event)->{result.setText("ACTION_"+action);return true;});root.addView(text);
        final boolean[] restart={false};
        text.addTextChangedListener(new TextWatcher(){
            public void beforeTextChanged(CharSequence s,int start,int count,int after){}
            public void onTextChanged(CharSequence s,int start,int before,int count){}
            public void afterTextChanged(Editable s){if(restart[0])text.post(()->((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).restartInput(text));}
        });
        LinearLayout lifecycle=new LinearLayout(this);
        Button auto=new Button(this);auto.setText("AUTO_RESTART");auto.setOnClickListener(v->{restart[0]=!restart[0];result.setText(restart[0]?"AUTO_ON":"AUTO_OFF");});lifecycle.addView(auto,new LinearLayout.LayoutParams(0,48,1));
        Button now=new Button(this);now.setText("RESTART_NOW");now.setOnClickListener(v->((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).restartInput(text));lifecycle.addView(now,new LinearLayout.LayoutParams(0,48,1));root.addView(lifecycle);
        Button chat=new Button(this);chat.setText("CHAT");chat.setOnClickListener(v->{text.setText("");text.setInputType(InputType.TYPE_CLASS_TEXT);text.setSingleLine(true);text.setImeOptions(EditorInfo.IME_ACTION_SEND);text.requestFocus();((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).restartInput(text);});root.addView(chat);
        Button number=new Button(this);number.setText("NUMBER");number.setOnClickListener(v->{text.setText("");text.setInputType(InputType.TYPE_CLASS_NUMBER);text.requestFocus();((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).restartInput(text);});root.addView(number);
        Button password=new Button(this);password.setText("PASSWORD");password.setOnClickListener(v->{text.setText("");text.setInputType(InputType.TYPE_CLASS_TEXT|InputType.TYPE_TEXT_VARIATION_PASSWORD);text.requestFocus();((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).restartInput(text);});root.addView(password);
        LinearLayout presets=new LinearLayout(this);
        for(String name:new String[]{"MID","SELECT","EMOJI"}){
            Button preset=new Button(this);preset.setText(name);preset.setOnClickListener(v->{text.requestFocus();text.setText(name.equals("MID")?"前后":name.equals("SELECT")?"前旧后":"前😀后");text.setSelection(name.equals("EMOJI")?3:1,name.equals("SELECT")?2:name.equals("EMOJI")?3:1);});presets.addView(preset,new LinearLayout.LayoutParams(0,48,1));
        }root.addView(presets);
        Button correction=new Button(this);correction.setText("CORRECT");correction.setOnClickListener(v->{text.requestFocus();text.setText("数萌算力机群");text.setSelection(4,5);});root.addView(correction);
        setContentView(root);text.requestFocus();getWindow().setSoftInputMode(5);
        text.postDelayed(()->((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).showSoftInput(text,InputMethodManager.SHOW_FORCED),300);
    }
}
