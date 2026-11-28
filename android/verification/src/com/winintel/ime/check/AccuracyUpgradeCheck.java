// 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime.check;
import android.app.Instrumentation;
import android.os.Bundle;
import java.io.*;
import java.util.*;
import com.winintel.ime.Dictionary;
import com.winintel.ime.PersonalLexicon;
public final class AccuracyUpgradeCheck extends Instrumentation {
    @Override public void onCreate(Bundle args){super.onCreate(args);start();}
    @Override public void onStart(){
        Bundle result=new Bundle();int checks=0;
        try{
            File root=new File(getTargetContext().getFilesDir(),"lexicon-019");
            // Read the files installed and prepared by the real IME service.
            for(String name:new String[]{"dictionary.bin","character-readings.tsv","name-parts.tsv"}){
                if(!new File(root,name).isFile())throw new AssertionError("Missing deployed "+name);checks++;
            }
            Dictionary dictionary=new Dictionary(new File(root,"dictionary.bin"));
            for(String[] f:new String[][]{
                {"wangxiaoming","王小明"},{"liuyuxin","刘雨欣"},{"zhaozihan","赵子涵"},
                {"ouyangzihan","欧阳子涵"},{"simayuhan","司马雨涵"},
                {"xiangliangshujuku","向量数据库"},{"zhegexiangmuwanquankaiyuan","这个项目完全开源"},
                {"qingbashengyintiaodayidian","请把声音调大一点"},{"huanghaoran","黄浩然"},
                {"womenxuyaoxiangliangjiansuo","我们需要向量检索"}
            }){
                if(!dictionary.query(f[0]).get(0).equals(f[1]))throw new AssertionError(Arrays.toString(f));checks++;
            }
            if(dictionary.query("yhm").indexOf("杨慧明")<0||dictionary.query("yhm").indexOf("杨慧明")>=9)throw new AssertionError("yhm visibility");checks++;
            File fixture=new File(getTargetContext().getCacheDir(),"accuracy018-learning.tsv");
            PersonalLexicon learned=new PersonalLexicon(fixture,new FileInputStream(new File(root,"character-readings.tsv")),new File(root,"name-parts.tsv"));
            learned.learn("shm","单浩明");
            PersonalLexicon reopened=new PersonalLexicon(fixture,new FileInputStream(new File(root,"character-readings.tsv")),new File(root,"name-parts.tsv"));
            if(!reopened.rank("shanhaoming",Collections.emptyList()).get(0).equals("单浩明"))throw new AssertionError("surname persistence");checks++;
            result.putString("result","PASS");result.putInt("checks",checks);finish(-1,result);
        }catch(Throwable e){result.putString("result","FAIL: "+e);result.putInt("checks",checks);finish(0,result);}
    }
}
