// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import java.io.*;
import java.util.*;
public final class Dictionary015Check {
    private static int checks;
    private static void check(boolean condition,String label){if(!condition)throw new AssertionError(label);checks++;System.out.println("| "+label+" | PASS |");}
    public static void main(String[] args)throws Exception{
        File assets=new File(args[0]);Dictionary d=new Dictionary(new File(assets,"dictionary.bin"));d.loadHotwords(new FileInputStream(new File(assets,"hot-chinese.tsv")));
        long start=System.nanoTime();NineKeyDictionary t9=new NineKeyDictionary(new File(assets,"dictionary.bin"));t9.loadHotwords(new FileInputStream(new File(assets,"hot-chinese.tsv")));
        System.out.println("| Check | Result |\n|---|---|\n| T9 index build | "+(System.nanoTime()-start)/1000000+" ms |");
        for(String[] word:new String[][]{{"nihao","你好"},{"rengongzhineng","人工智能"},{"zhinengti","智能体"},{"jushenzhineng","具身智能"},{"xinz hishengchanli".replace(" ",""),"新质生产力"},{"zhonghuarenmingongheguo","中华人民共和国"}}){
            check(d.query(word[0]).contains(word[1]),"Full pinyin "+word[1]);
            check(t9.query(NineKeyDictionary.digits(word[0])).contains(word[1]),"T9 "+word[1]);
        }
        check(d.query("shi").size()>9,"More than nine same-code candidates");
        File personalFile=new File(args[1],"personal-test.tsv");
        PersonalLexicon p=new PersonalLexicon(personalFile,new FileInputStream(new File(assets,"character-readings.tsv")));
        p.learn("shumengsuanliji qun".replace(" ",""),"数萌算力集群");
        check(p.rank("shumengsuanlijiqun",Collections.emptyList()).get(0).equals("数萌算力集群"),"New phrase learned");
        p.learn("shumengsuanlijiqun","数萌算力机群");
        check(p.rank("shumengsuanlijiqun",Collections.singletonList("数萌算力集群")).get(0).equals("数萌算力机群"),"One correction ranks first");
        PersonalLexicon reloaded=new PersonalLexicon(personalFile,new FileInputStream(new File(assets,"character-readings.tsv")));
        check(reloaded.rank("shumengsuanlijiqun",Collections.emptyList()).get(0).equals("数萌算力机群"),"Correction persists after reload");
        check(reloaded.rank("t9:"+NineKeyDictionary.digits("shumengsuanlijiqun"),Collections.emptyList()).contains("数萌算力机群"),"Learned word available in T9");
        check(reloaded.rank("smsljq",Collections.emptyList()).contains("数萌算力机群"),"Learned word available with initials");
        System.out.println("| Total | "+checks+" PASS |");
    }
}
