// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime.check;
import android.app.Instrumentation;
import android.content.Context;
import android.content.ContextWrapper;
import android.os.Bundle;
import android.widget.EditText;
import android.widget.Button;
import android.view.*;
import android.view.inputmethod.EditorInfo;
import android.text.InputType;
import android.graphics.Color;
import com.winintel.ime.ImeService;
import com.winintel.ime.SpeechPunctuation;
import com.winintel.ime.VoiceService;
import com.winintel.ime.PersonalLexicon;
import java.lang.reflect.*;

/** Exercise the production draft paths on Android, including UTF-16 selection ranges. */
public final class CaretCheck extends Instrumentation {
    private int passed;
    private Throwable failure;
    private void check(boolean ok,String label){if(!ok)throw new AssertionError(label);passed++;}
    private static Object field(Object value,String name)throws Exception{Field f=value.getClass().getDeclaredField(name);f.setAccessible(true);return f.get(value);}
    private static void set(Object value,String name,Object data)throws Exception{Field f=value.getClass().getDeclaredField(name);f.setAccessible(true);f.set(value,data);}
    private static void call(Object value,String name,Class<?>[] types,Object...args)throws Exception{Method m=value.getClass().getDeclaredMethod(name,types);m.setAccessible(true);m.invoke(value,args);}
    private static Button language(View view){
        if(view instanceof Button&&((Button)view).getText().toString().equals("中 / EN"))return (Button)view;
        if(view instanceof ViewGroup){ViewGroup group=(ViewGroup)view;for(int i=0;i<group.getChildCount();i++){Button b=language(group.getChildAt(i));if(b!=null)return b;}}
        return null;
    }
    private static boolean contains(View view,String text){
        if(view instanceof Button&&((Button)view).getText().toString().equals(text))return true;
        if(view instanceof ViewGroup){ViewGroup g=(ViewGroup)view;for(int i=0;i<g.getChildCount();i++)if(contains(g.getChildAt(i),text))return true;}
        return false;
    }
    @Override public void onCreate(Bundle args){super.onCreate(args);start();}
    @Override public void onStart(){
        runOnMainSync(()->{
            ImeService ime=null;
            try{
                ime=new ImeService();
                Method attach=ContextWrapper.class.getDeclaredMethod("attachBaseContext",Context.class);attach.setAccessible(true);attach.invoke(ime,getTargetContext());
                ime.onCreateInputView();
                View root=(View)field(ime,"root");
                check(!contains(root,"编辑")&&!contains(root,"编辑草稿")&&!contains(root,"发送"),"no edit or send buttons");
                Button mode=language(root);mode.setPressed(true);check(mode.getCurrentTextColor()==Color.WHITE,"pressed key inverse white text");mode.setPressed(false);mode.setSelected(true);check(mode.getCurrentTextColor()==Color.WHITE,"selected key inverse white text");mode.setSelected(false);
                EditorInfo info=new EditorInfo();info.packageName="com.winintel.ime.check";info.fieldId=17;info.inputType=InputType.TYPE_CLASS_TEXT;info.imeOptions=EditorInfo.IME_ACTION_SEND;
                ime.onStartInput(info,false);set(ime,"code","nihao");ime.onStartInput(info,true);
                check(field(ime,"code").equals("nihao"),"same editor restart preserves Chinese composition");
                ime.onUpdateSelection(0,0,2,2,-1,-1);check(field(ime,"code").equals("nihao"),"delayed host selection callback preserves Chinese composition");
                info.fieldId=18;ime.onStartInput(info,true);check(field(ime,"code").equals(""),"new field clears old composition");
                EditText draft=(EditText)field(ime,"draft");draft.setVisibility(View.VISIBLE);
                android.view.MotionEvent touch=android.view.MotionEvent.obtain(0,0,android.view.MotionEvent.ACTION_DOWN,10,10,0);draft.dispatchTouchEvent(touch);touch.recycle();
                check((Boolean)field(ime,"editing"),"touch draft directly enables keyboard editing");
                draft.setText("前旧后");draft.setSelection(1,2);call(ime,"commit",new Class[]{String.class},"新");
                check(draft.getText().toString().equals("前新后")&&draft.getSelectionStart()==2,"draft selection replacement");
                draft.setSelection(1);call(ime,"commit",new Class[]{String.class},"插入");
                check(draft.getText().toString().equals("前插入新后")&&draft.getSelectionStart()==3,"draft middle insertion");
                draft.setSelection(1,3);call(ime,"backspace",new Class[]{});
                check(draft.getText().toString().equals("前新后")&&draft.getSelectionStart()==1,"draft selection deletion");
                draft.setText("前😀后");draft.setSelection(3);call(ime,"backspace",new Class[]{});
                check(draft.getText().toString().equals("前后")&&draft.getSelectionStart()==1,"whole Unicode code point deletion");
                draft.setText("前😀后");draft.setSelection(3);call(ime,"moveCursor",new Class[]{int.class},-1);
                check(draft.getSelectionStart()==1,"cursor skips surrogate pair");
                set(ime,"selecting",true);set(ime,"selectionAnchor",-1);call(ime,"moveCursor",new Class[]{int.class},1);
                check(draft.getSelectionStart()==1&&draft.getSelectionEnd()==3,"selection spans complete emoji");
                draft.setText("前旧后");draft.setSelection(1,2);call(ime,"voiceEvent",new Class[]{int.class,String.class},VoiceService.TEXT,"追加。");
                check(draft.getText().toString().equals("前旧后追加。")&&draft.getSelectionStart()==1&&draft.getSelectionEnd()==2,"voice callback preserves editing range");
                language((View)field(ime,"root")).performClick();
                check((Boolean)field(ime,"editing"),"language switch keeps draft editing target");
                java.io.File store=new java.io.File(getTargetContext().getCacheDir(),"check-personal.tsv");
                PersonalLexicon personal=new PersonalLexicon(store,getTargetContext().getAssets().open("character-readings.tsv"));
                personal.learn("shumengsuanlijiqun","数萌算力集群");personal.learn("shumengsuanlijiqun","数萌算力机群");
                personal=new PersonalLexicon(store,getTargetContext().getAssets().open("character-readings.tsv"));
                check(personal.rank("shumengsuanlijiqun",java.util.Collections.emptyList()).get(0).equals("数萌算力机群"),"Android local correction persists");
                for(String[] sample:new String[][]{
                    {"呃，啊，这个，那个，开始测试","开始测试。"},
                    {"这个方案和那个配置都可以","这个方案和那个配置都可以。"},
                    {"你好啊","你好啊。"},
                    {"V100 NVLink 3.14","V100 NVLink 3.14."},
                    {"。。。",""},
                    {"こんにちは",""},
                    {"测试，ありがとう，继续","测试，继续。"},
                    {"测试。。。。","测试。"},
                    {"换行","\n"},
                    {"句号","。"},
                    {"为什么失败","为什么失败？"}
                })check(SpeechPunctuation.apply(sample[0],true).equals(sample[1]),"speech cleanup: "+sample[0]);
            }catch(Throwable e){failure=e;}
            finally{if(ime!=null)try{((java.util.concurrent.ExecutorService)field(ime,"lookup")).shutdownNow();}catch(Exception ignored){}}
        });
        Bundle result=new Bundle();result.putString("result",failure==null?"PASS":("FAIL: "+failure));result.putInt("checks",passed);finish(failure==null?-1:0,result);
    }
}
