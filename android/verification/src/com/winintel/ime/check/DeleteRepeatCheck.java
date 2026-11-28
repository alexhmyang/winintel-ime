// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
package com.winintel.ime.check;
import android.app.Instrumentation;
import android.content.*;
import android.os.*;
import android.view.*;
import android.widget.*;
import com.winintel.ime.ImeService;
import java.lang.reflect.*;
public final class DeleteRepeatCheck extends Instrumentation {
    private ImeService ime;private Button key;private Throwable failure;private int before,checks;
    private Field field(String name)throws Exception{Field f=ImeService.class.getDeclaredField(name);f.setAccessible(true);return f;}
    private Object call(String name)throws Exception{Method m=ImeService.class.getDeclaredMethod(name);m.setAccessible(true);return m.invoke(ime);}
    private void check(boolean b,String label){if(!b)throw new AssertionError(label);checks++;}
    private void main(Runnable r){runOnMainSync(()->{try{r.run();}catch(Throwable t){failure=t;}});}
    private void touch(int action){key.dispatchTouchEvent(MotionEvent.obtain(0,SystemClock.uptimeMillis(),action,20,20,0));}
    private int length(){try{return ((String)field("code").get(ime)).length();}catch(Exception e){throw new RuntimeException(e);}}
    @Override public void onCreate(Bundle args){super.onCreate(args);start();}
    @Override public void onStart(){
        main(()->{try{
            ime=new ImeService();Method attach=ContextWrapper.class.getDeclaredMethod("attachBaseContext",Context.class);attach.setAccessible(true);attach.invoke(ime,getTargetContext());ime.onCreateInputView();
            field("code").set(ime,"abcdefghijklmnopqrstuvwxyz");key=(Button)call("deleteKey");
            key.measure(View.MeasureSpec.makeMeasureSpec(100,1073741824),View.MeasureSpec.makeMeasureSpec(60,1073741824));key.layout(0,0,100,60);touch(MotionEvent.ACTION_DOWN);
        }catch(Exception e){throw new RuntimeException(e);}});
        SystemClock.sleep(550);
        main(()->{before=length();check(before<26,"long press repeats");touch(MotionEvent.ACTION_UP);check(length()==before,"release has no extra delete");});
        SystemClock.sleep(150);
        main(()->{check(length()==before,"repeat stopped on release");touch(MotionEvent.ACTION_DOWN);});
        SystemClock.sleep(450);
        main(()->{touch(MotionEvent.ACTION_CANCEL);before=length();});
        SystemClock.sleep(150);
        main(()->{check(length()==before,"cancel stops repeats");touch(MotionEvent.ACTION_DOWN);ime.onFinishInputView(false);before=length();});
        SystemClock.sleep(450);
        main(()->{check(length()==before,"hiding stops pending repeats");try{((java.util.concurrent.ExecutorService)field("lookup").get(ime)).shutdownNow();}catch(Exception ignored){}});
        Bundle b=new Bundle();b.putString("result",failure==null?"PASS":"FAIL: "+failure);b.putInt("checks",checks);finish(failure==null?-1:0,b);
    }
}
