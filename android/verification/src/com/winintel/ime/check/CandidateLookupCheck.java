// SPDX-License-Identifier: GPL-3.0-or-later
// 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime.check;
import android.app.Instrumentation;
import android.os.Bundle;
import com.winintel.ime.Dictionary;
import java.io.*;
import java.lang.reflect.*;
import java.util.*;
public final class CandidateLookupCheck extends Instrumentation{
    @Override public void onCreate(Bundle args){super.onCreate(args);start();}
    @Override public void onStart(){
        Bundle result=new Bundle();int checks=0;
        try{
            File root=new File(getTargetContext().getFilesDir(),"lexicon-019");
            Dictionary d=new Dictionary(new File(root,"dictionary.bin"));
            d.loadHotwords(getTargetContext().getAssets().open("hot-chinese.tsv"));
            Class<?> type=Class.forName("com.winintel.ime.NineKeyDictionary");
            Constructor<?> constructor=type.getDeclaredConstructor(File.class);constructor.setAccessible(true);Object t9=constructor.newInstance(new File(root,"dictionary.bin"));
            Method hot=type.getDeclaredMethod("loadHotwords",InputStream.class);hot.setAccessible(true);hot.invoke(t9,getTargetContext().getAssets().open("hot-chinese.tsv"));
            Method digits=type.getDeclaredMethod("digits",String.class),query=type.getDeclaredMethod("query",String.class);digits.setAccessible(true);query.setAccessible(true);
            String[] expected={"天眼查","让谁","模力赢","计算机","输入法","图书馆","火车站","文件夹"};
            String[] keys={"tianyancha","rangshei","moliying","jisuanij","shurfa","tushugan","huochezahn","wenjainjia"};
            for(int i=0;i<keys.length;i++){
                List<String> rows=d.expandCandidates(keys[i],d.query(keys[i]));
                if(rows.size()<50||new HashSet<>(rows).size()!=rows.size()||rows.indexOf(expected[i])<0||rows.indexOf(expected[i])>=25)throw new AssertionError("QWERTY "+keys[i]);checks++;
                List<String> nine=(List<String>)query.invoke(t9,digits.invoke(null,keys[i]));
                if(nine.size()<50||new HashSet<>(nine).size()!=nine.size()||nine.indexOf(expected[i])<0||nine.indexOf(expected[i])>=25)throw new AssertionError("T9 "+keys[i]);checks++;
            }
            result.putString("result","PASS");result.putInt("checks",checks);finish(-1,result);
        }catch(Throwable e){result.putString("result","FAIL: "+e);result.putInt("checks",checks);finish(0,result);}
    }
}
