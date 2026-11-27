package com.winintel.ime;
import java.io.*;
import java.nio.charset.*;
import java.util.*;
public final class LayoutVoiceCheck {
    private static final List<String> rows=new ArrayList<>();
    private static void check(boolean ok,String name){if(!ok)throw new AssertionError(name);rows.add("| "+name+" | 通过 |");}
    public static void main(String[] args)throws Exception{
        rows.add("| 检查 | 结果 |");rows.add("|---|---|");
        long start=System.nanoTime();NineKeyDictionary d=new NineKeyDictionary(new File(args[0]));
        rows.add("| 九宫格索引构建 | "+((System.nanoTime()-start)/1000000)+" ms |");
        check(NineKeyDictionary.digits("nihao").equals("64426"),"字母到九键映射");
        check(d.query("64426").contains("你好"),"九宫格你好");
        check(d.query("94664486").contains("中国"),"九宫格中国");
        check(d.query("74878").contains("输入"),"九宫格输入");
        check(!d.query("64").isEmpty(),"九宫格短码候选");
        check(d.query("999999999999999999999999999999999999999999999999999999").isEmpty(),"超长未知九键");
        check(SpeechPunctuation.apply("今天开会",true).equals("今天开会。"),"中文句尾");
        check(SpeechPunctuation.apply("可以吗",true).equals("可以吗？"),"中文问句");
        check(SpeechPunctuation.apply("今天开会",false).equals("今天开会，"),"持续长段落");
        check(SpeechPunctuation.apply("第一句。",true).equals("第一句。"),"保留已有标点");
        check(SpeechPunctuation.apply("你好逗号明天见句号",true).equals("你好，明天见。"),"口述标点");
        check(SpeechPunctuation.apply("你好换行明天见",true).equals("你好\n明天见。"),"口述换行");
        check(SpeechPunctuation.apply("V100 32G NVLink 3.5",true).equals("V100 32G NVLink 3.5."),"技术术语和小数");
        check(SpeechPunctuation.apply("How are you",true).equals("How are you?"),"英文问句");
        check(SpeechPunctuation.apply("清空。",true).equals("清空。"),"保留草稿指令");
        check(SpeechPunctuation.join("你好，","明天见。").equals("你好，明天见。"),"中文连续草稿");
        check(SpeechPunctuation.join("Hello.","World.").equals("Hello. World."),"英文草稿间空格");
        check(SpeechPunctuation.apply("",true).isEmpty(),"静音不补标点");
        try(Writer out=new OutputStreamWriter(new FileOutputStream(args[1]),StandardCharsets.UTF_8)){for(String row:rows)out.write(row+"\n");}
        for(String row:rows)System.out.println(row);
    }
}
