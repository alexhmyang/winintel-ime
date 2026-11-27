// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import java.io.*;
import java.util.*;
public final class Dictionary016Check {
    private static int checks;
    private static void check(boolean value,String label){if(!value)throw new AssertionError(label);checks++;System.out.println("| "+label+" | PASS |");}
    public static void main(String[] args)throws Exception{
        File assets=new File(args[0]);Dictionary d=new Dictionary(new File(assets,"dictionary.bin"));
        System.out.println("| Check | Result |\n|---|---|");
        for(String[] row:new String[][]{{"yhm","杨慧明"},{"yanghuiming","杨慧明"},{"yanghm","杨慧明"},{"yhuiming","杨慧明"},{"zhanghaoyu","张浩宇"},{"chenjiaming","陈嘉明"},{"oyzh","欧阳子涵"},{"simayuhan","司马雨涵"},{"zhonghuarenmingongheguo","中华人民共和国"}})
            check(d.query(row[0]).contains(row[1]),row[0]+" → "+row[1]);
        check(d.query("yhm").indexOf("杨慧明")<9,"杨慧明 appears on first page");
        check(d.query("zg").get(0).equals("中国"),"Common word retains first position");
        check(d.query("shi").size()>96,"Full spelling candidates retained beyond old 96 cap");
        check(d.query("zz").size()>192,"Initial candidates retained beyond old 192 cap");
        NineKeyDictionary t9=new NineKeyDictionary(new File(assets,"dictionary.bin"));
        for(String key:new String[]{"yanghuiming","yhm"})check(t9.query(NineKeyDictionary.digits(key)).contains("杨慧明"),"T9 "+key);
        File personalFile=new File(args[1],"personal016-test.tsv");
        PersonalLexicon p=new PersonalLexicon(personalFile,new FileInputStream(new File(assets,"character-readings.tsv")));
        p.learn("yhm","杨慧明");
        PersonalLexicon reloaded=new PersonalLexicon(personalFile,new FileInputStream(new File(assets,"character-readings.tsv")));
        for(String key:new String[]{"yhm","yanghuiming","t9:"+NineKeyDictionary.digits("yhm"),"t9:"+NineKeyDictionary.digits("yanghuiming")})
            check(reloaded.rank(key,Collections.emptyList()).get(0).equals("杨慧明"),"Learning persists "+key);
        check(d.query("shanhaoming").contains("单浩明"),"Surname-specific pronunciation");
        reloaded.learn("shm","单浩明");
        PersonalLexicon surnameReload=new PersonalLexicon(personalFile,new FileInputStream(new File(assets,"character-readings.tsv")));
        check(surnameReload.rank("shanhaoming",Collections.emptyList()).get(0).equals("单浩明"),"Surname pronunciation retained after learning");
        long start=System.nanoTime();for(int i=0;i<100;i++)d.query("yhm");
        System.out.println("| 100 cached yhm queries | "+((System.nanoTime()-start)/1000000)+" ms |\n| Total | "+checks+" PASS |");
    }
}
