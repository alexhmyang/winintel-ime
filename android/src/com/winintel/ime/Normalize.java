package com.winintel.ime;
import java.util.regex.*;
public final class Normalize {
    public static String apply(String text) {
        text=text.replaceAll("<\\|[^|]*\\|>","");
        text=text.replaceAll("(?i)n\\s*v\\s*link","NVLink").replaceAll("(?i)v\\s*llm","vLLM");
        text=text.replaceAll("(?i)s\\s*x\\s*m\\s*([23二三])","SXM$1").replace("SXM二","SXM2").replace("SXM三","SXM3");
        text=text.replaceAll("(?i)\\bv\\s*100\\s*(\\d+)\\s*g\\b","V100 $1G");
        text=text.replaceAll("(?i)(\\d+)\\s*g\\s*b\\s*p\\s*s","$1Gbps");
        text=text.replaceAll("(?i)win\\s*intel","winintel");
        Pattern numbers=Pattern.compile("[零〇一二两三四五六七八九十百千万]+");
        Matcher m=numbers.matcher(text);StringBuffer out=new StringBuffer();
        while(m.find()){
            String s=m.group();long val=number(s);
            String prefix=text.substring(0,m.start()),suffix=text.substring(m.end());
            boolean idiom=s.equals("万一") || (s.equals("千万") && suffix.matches("^(不要|别|要|不能|记住).*"));
            boolean singleWord=s.length()==1 && !prefix.matches(".*(第|星期|周)$") && suffix.matches("^[\\u3400-\\u9fff].*") && !suffix.matches("^(个|张|台|次|年|月|日|点|分|秒|度|元|万|亿|百|千|十|兆|吉|天|晚|号|路|块|小时).*");
            m.appendReplacement(out,Matcher.quoteReplacement(idiom||singleWord?s:Long.toString(val)));
        }
        m.appendTail(out);
        return out.toString().replaceAll("(?i)\\b(V100)\\s*(\\d+)\\s*g\\b","$1 $2G").replaceAll("(?i)(\\d+)\\s*g\\s*b\\s*p\\s*s","$1Gbps").trim();
    }
    static long number(String s){
        String digits="零一二三四五六七八九";
        if(!s.matches(".*[十百千万].*")){
            long n=0;for(char c:s.toCharArray())n=n*10+Math.max(0,digits.indexOf(c=='两'?'二':c=='〇'?'零':c));return n;
        }
        long total=0,section=0,n=0;
        for(char c:s.toCharArray()){
            int d=digits.indexOf(c=='两'?'二':c=='〇'?'零':c);
            if(d>=0)n=d;
            else if(c=='万'){total+=(section+n)*10000;section=0;n=0;}
            else{int unit=c=='十'?10:c=='百'?100:1000;section+=(n==0?1:n)*unit;n=0;}
        }
        return total+section+n;
    }
}
