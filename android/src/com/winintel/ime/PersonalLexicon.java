// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Device-local words and corrections. Accessed only by the lookup worker. */
public final class PersonalLexicon {
    private final File file;
    private final NameCandidates names;
    private final Map<String,String> readings=new HashMap<>();
    private final Map<String,Entry> words=new HashMap<>();
    private long lastTouched;
    private static final class Entry {
        String code,text;int frequency;long touched;
        Entry(String c,String t,int f,long time){code=c;text=t;frequency=f;touched=time;}
    }
    public PersonalLexicon(File file,InputStream input)throws IOException{
        this.file=file;
        names=new NameCandidates(new File(file.getParentFile(),"name-parts.tsv"));
        try(BufferedReader r=new BufferedReader(new InputStreamReader(input,StandardCharsets.UTF_8))){
            String line;while((line=r.readLine())!=null){String[] f=line.split("\t");if(f.length==2)readings.put(f[0],f[1].split(" ")[0]);}
        }
        if(file.isFile())try(BufferedReader r=new BufferedReader(new InputStreamReader(new FileInputStream(file),StandardCharsets.UTF_8))){
            String line;while((line=r.readLine())!=null){String[] f=line.split("\t");if(f.length==4)try{Entry e=new Entry(f[0],f[1],Integer.parseInt(f[2]),Long.parseLong(f[3]));words.put(e.code+"\t"+e.text,e);lastTouched=Math.max(lastTouched,e.touched);}catch(NumberFormatException ignored){}}
        }
    }
    private void remember(String code,String text){
        if(code.isEmpty())return;
        String key=code+"\t"+text;Entry e=words.get(key);
        if(e==null){e=new Entry(code,text,0,0);words.put(key,e);}
        e.frequency=Math.min(100000,e.frequency+1);e.touched=lastTouched=Math.max(lastTouched+1,System.currentTimeMillis());
    }
    public void learn(String typed,String text)throws IOException{
        if(text==null||text.codePointCount(0,text.length())<2||text.length()>48||!text.codePoints().allMatch(c->c>=0x3400&&c<=0x9fff))return;
        StringBuilder full=new StringBuilder(),initials=new StringBuilder();
        for(int i=0;i<text.length();i++){String p=readings.get(text.substring(i,i+1));if(p==null){full.setLength(0);break;}full.append(p);initials.append(p.charAt(0));}
        String[] name=names.spelling(text);
        if(name!=null){full=new StringBuilder(name[0]);initials=new StringBuilder(name[1]);}
        if(typed!=null&&!typed.isEmpty())remember(typed.replace("'",""),text);
        if(full.length()>0){
            remember(full.toString(),text);remember(initials.toString(),text);
            remember("t9:"+NineKeyDictionary.digits(full.toString()),text);
            remember("t9:"+NineKeyDictionary.digits(initials.toString()),text);
        }
        if(words.size()>20000){
            List<Entry> entries=new ArrayList<>(words.values());entries.sort(Comparator.comparingLong(e->e.touched));
            for(int i=0;i<entries.size()-18000;i++){Entry e=entries.get(i);words.remove(e.code+"\t"+e.text);}
        }
        File temp=new File(file.getPath()+".tmp");
        try(Writer out=new OutputStreamWriter(new FileOutputStream(temp),StandardCharsets.UTF_8)){
            for(Entry e:words.values())out.write(e.code+"\t"+e.text+"\t"+e.frequency+"\t"+e.touched+"\n");
        }
        java.nio.file.Files.move(temp.toPath(),file.toPath(),java.nio.file.StandardCopyOption.REPLACE_EXISTING);
    }
    public List<String> rank(String code,List<String> base){
        List<Entry> personal=new ArrayList<>();
        for(Entry e:words.values())if(e.code.equals(code))personal.add(e);
        personal.sort((a,b)->{int c=Long.compare(b.touched,a.touched);return c!=0?c:Integer.compare(b.frequency,a.frequency);});
        List<String> result=new ArrayList<>();for(Entry e:personal)if(!result.contains(e.text))result.add(e.text);
        for(String s:base)if(!result.contains(s))result.add(s);
        return result;
    }
}
