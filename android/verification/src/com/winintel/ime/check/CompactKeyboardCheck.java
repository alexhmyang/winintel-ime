// SPDX-License-Identifier: GPL-3.0-or-later
// 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime.check;
import android.app.Instrumentation;
import android.content.*;
import android.os.Bundle;
import android.view.*;
import android.widget.*;
import com.winintel.ime.ImeService;
import java.lang.reflect.*;
import java.util.*;

/** Verify 25-item pages at different widths, including the final partial page. */
public final class CompactKeyboardCheck extends Instrumentation {
    private int passed;
    private Throwable failure;
    private void check(boolean ok,String label){if(!ok)throw new AssertionError(label);passed++;}
    private Object field(Object value,String name)throws Exception{Field f=value.getClass().getDeclaredField(name);f.setAccessible(true);return f.get(value);}
    private void set(Object value,String name,Object data)throws Exception{Field f=value.getClass().getDeclaredField(name);f.setAccessible(true);f.set(value,data);}
    private Object call(Object value,String name,Class<?>[] types,Object...args)throws Exception{Method m=value.getClass().getDeclaredMethod(name,types);m.setAccessible(true);return m.invoke(value,args);}
    @Override public void onCreate(Bundle args){super.onCreate(args);start();}
    @Override public void onStart(){
        runOnMainSync(()->{
            ImeService ime=new ImeService();
            try{
                Method attach=ContextWrapper.class.getDeclaredMethod("attachBaseContext",Context.class);attach.setAccessible(true);attach.invoke(ime,getTargetContext());
                View root=ime.onCreateInputView();
                check(((View)field(ime,"editorTools")).getVisibility()==View.GONE,"editor toolbar hidden");
                List<String> list=new ArrayList<>();
                for(int i=0;i<137;i++)list.add(i%7==0?"完整显示的很长候选句子"+i:"候选"+i);
                set(ime,"results",list);set(ime,"code","64");
                for(int width:new int[]{240,320,411}){
                    int px=(int)(width*getTargetContext().getResources().getDisplayMetrics().density);
                    root.measure(View.MeasureSpec.makeMeasureSpec(px,View.MeasureSpec.EXACTLY),View.MeasureSpec.makeMeasureSpec(2000,View.MeasureSpec.AT_MOST));root.layout(0,0,px,root.getMeasuredHeight());
                    set(ime,"page",0);((List<?>)field(ime,"previousPages")).clear();call(ime,"showCandidates",new Class[]{});
                    int seen=0,rounds=0;
                    while(true){
                        int start=(Integer)field(ime,"page"),end=(Integer)field(ime,"candidateEnd");
                        check(start==seen&&end==Math.min(start+25,list.size()),"25 item page at width "+width);
                        LinearLayout row=(LinearLayout)field(ime,"candidates");
                        check(row.getChildCount()>0&&row.getChildCount()<=end-start,"compact preview fits page");
                        for(int i=0;i<row.getChildCount();i++)check(((Button)row.getChildAt(i)).getText().toString().equals(list.get(start+i)),"candidate order");
                        set(ime,"expandedCandidates",true);call(ime,"updateCandidatePanel",new Class[]{});
                        LinearLayout grid=(LinearLayout)field(ime,"candidateGrid");
                        int shown=0;
                        for(int r=1;r<grid.getChildCount();r++){
                            LinearLayout line=(LinearLayout)grid.getChildAt(r);
                            check(line.getChildCount()>0,"continuous candidate row");
                            for(int c=0;c<line.getChildCount();c++)if(line.getChildAt(c) instanceof Button){
                                check(((Button)line.getChildAt(c)).getText().toString().equals(list.get(start+shown)),"grid candidate order");shown++;
                                check(line.getChildAt(c).getMinimumHeight()>=48*getTargetContext().getResources().getDisplayMetrics().density,"48dp candidate target");
                            }
                        }
                        check(shown==end-start,"all page candidates visible in grid");
                        seen=end;rounds++;check(rounds<=137,"paging makes progress");
                        Button next=(Button)field(ime,"candidateNext");
                        if(!next.isEnabled())break;
                        next.performClick();
                    }
                    check(seen==list.size(),"all 137 candidates reachable");
                    Button previous=(Button)field(ime,"candidatePrevious");
                    while(previous.isEnabled())previous.performClick();
                    check((Integer)field(ime,"page")==0,"previous page returns to first");
                }
                int h=((View)field(ime,"keys")).getMeasuredHeight();
                check(h<=getTargetContext().getResources().getDisplayMetrics().density*216,"compact four rows");
                set(ime,"expandedCandidates",false);set(ime,"nine",true);set(ime,"english",false);
                call(ime,"renderKeys",new Class[]{});call(ime,"updateCandidatePanel",new Class[]{});
                root.measure(View.MeasureSpec.makeMeasureSpec(1080,View.MeasureSpec.EXACTLY),View.MeasureSpec.makeMeasureSpec(2000,View.MeasureSpec.AT_MOST));root.layout(0,0,1080,root.getMeasuredHeight());
                LinearLayout keyboard=(LinearLayout)field(ime,"keys");
                int labels=0;for(int r=0;r<keyboard.getChildCount();r++){
                    LinearLayout line=(LinearLayout)keyboard.getChildAt(r);
                    for(int c=0;c<line.getChildCount();c++)if(line.getChildAt(c) instanceof Button){
                        Button b=(Button)line.getChildAt(c);
                        if(b.getText().toString().contains("\n")){labels++;check(b.getLayout()!=null&&b.getLayout().getHeight()<=b.getHeight(),"two-line key labels fit");}
                    }
                }
                check(labels==9,"all nine two-line labels verified");
                set(ime,"expandedCandidates",false);call(ime,"toggleCandidates",new Class[]{});
                check(((View)field(ime,"keys")).getVisibility()==View.GONE,"expanded list replaces keys");
                View panel=(View)field(ime,"candidatePanel");check(panel.getVisibility()==View.VISIBLE,"expanded list visible");
                check(panel.getLayoutParams().height<=getTargetContext().getResources().getDisplayMetrics().density*216,"expanded list bounded height");
                call(ime,"toggleCandidates",new Class[]{});
                check(((View)field(ime,"keys")).getVisibility()==View.VISIBLE,"collapse restores keyboard");
                set(ime,"numbers",true);call(ime,"renderKeys",new Class[]{});
                check(((LinearLayout)field(ime,"keys")).getChildCount()==4,"numbers have four rows");
            }catch(Throwable e){failure=e;}
            finally{try{((java.util.concurrent.ExecutorService)field(ime,"lookup")).shutdownNow();}catch(Exception ignored){}}
        });
        Bundle result=new Bundle();result.putString("result",failure==null?"PASS":"FAIL: "+failure);result.putInt("checks",passed);finish(failure==null?-1:0,result);
    }
}
