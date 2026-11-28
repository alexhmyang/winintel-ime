// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
package com.winintel.ime;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Portable offline domain overlay. PersonalLexicon ranks the returned list afterwards. */
public final class IndustryLexicon {
    private static final class Word {
        final String code,text;final float score;
        Word(String c,String t,float s){code=c;text=t;score=s;}
    }
    private final Map<String,Map<String,List<Word>>> chinese=new HashMap<>(),nine=new HashMap<>();
    private final Map<String,List<Word>> english=new HashMap<>();
    public IndustryLexicon(InputStream input)throws IOException{
        try(BufferedReader r=new BufferedReader(new InputStreamReader(input,StandardCharsets.UTF_8))){
            String line;int loaded=0;
            while((line=r.readLine())!=null&&++loaded<=50000){
                String[] f=line.split("\t",-1);if(f.length!=5||f[2].isEmpty()||f[2].length()>128)continue;
                float weight;try{weight=Float.parseFloat(f[4]);}catch(NumberFormatException ignored){continue;}
                Word word=new Word(f[2].toLowerCase(Locale.ROOT),f[3],weight);
                if(f[1].equals("E"))english.computeIfAbsent(f[0],k->new ArrayList<>()).add(word);
                else if(f[1].equals("C")){
                    chinese.computeIfAbsent(f[0],k->new HashMap<>()).computeIfAbsent(word.code,k->new ArrayList<>()).add(word);
                    nine.computeIfAbsent(f[0],k->new HashMap<>()).computeIfAbsent(NineKeyDictionary.digits(word.code),k->new ArrayList<>()).add(word);
                }
            }
        }
    }
    public List<String> rank(String profile,String typed,List<String> base,boolean en,boolean t9){
        String key=typed.toLowerCase(Locale.ROOT).replace("'","");
        if(key.isEmpty()||profile.equals("general"))return new ArrayList<>(base);
        List<Word> matched=new ArrayList<>();
        if(en){
            for(Word w:english.getOrDefault(profile,Collections.emptyList()))if(w.code.startsWith(key))matched.add(w);
            matched.sort((a,b)->{boolean ax=a.code.equals(key),bx=b.code.equals(key);if(ax!=bx)return ax?-1:1;int c=Float.compare(b.score-(b.code.length()-key.length()),a.score-(a.code.length()-key.length()));return c!=0?c:a.text.compareTo(b.text);});
        }else{
            Map<String,List<Word>> pack=(t9?nine:chinese).get(profile);
            if(pack!=null)matched.addAll(pack.getOrDefault(key,Collections.emptyList()));
            matched.sort((a,b)->{int c=Float.compare(b.score,a.score);return c!=0?c:a.text.compareTo(b.text);});
        }
        // Exact general English words retain precedence over non-exact completions.
        List<String> result=new ArrayList<>();
        if(en&&!base.isEmpty()&&base.get(0).equalsIgnoreCase(typed)&&matched.stream().noneMatch(w->w.code.equals(key)))result.add(base.get(0));
        for(Word w:matched)if(!result.contains(w.text))result.add(w.text);
        for(String text:base)if(!result.contains(text))result.add(text);
        return result;
    }
}
