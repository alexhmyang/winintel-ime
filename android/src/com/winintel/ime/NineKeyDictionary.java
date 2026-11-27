// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import java.io.*;
import java.nio.*;
import java.nio.channels.*;
import java.nio.charset.*;
import java.util.*;

/** An offline T9 index of the existing scored pinyin dictionary. */
final class NineKeyDictionary {
    private static final String KEYS="22233344455566677778889999";
    private final ByteBuffer data;
    private final int strings;
    private final int[] entries;
    private final NameCandidates names;
    private final Map<String,String> hotwords=new HashMap<>();
    static String digits(String code){
        StringBuilder b=new StringBuilder();
        for(char c:code.toLowerCase(Locale.ROOT).toCharArray()){
            if(c>='a'&&c<='z')b.append(KEYS.charAt(c-'a'));
            else if(c>='2'&&c<='9')b.append(c);
        }
        return b.toString();
    }
    NineKeyDictionary(File file)throws IOException{
        names=new NameCandidates(new File(file.getParentFile(),"name-parts.tsv"));
        try(FileInputStream f=new FileInputStream(file)){data=f.getChannel().map(FileChannel.MapMode.READ_ONLY,0,f.getChannel().size()).order(ByteOrder.LITTLE_ENDIAN);}
        int count=data.getInt(8);strings=data.getInt(12);entries=new int[count];
        for(int i=0;i<count;i++)entries[i]=i;
        sort(0,count-1);
    }
    void loadHotwords(InputStream input)throws IOException{
        try(BufferedReader r=new BufferedReader(new InputStreamReader(input,StandardCharsets.UTF_8))){
            String line;while((line=r.readLine())!=null){String[] f=line.split("\t");if(f.length==2)hotwords.put(digits(f[0]),f[1]);}
        }
    }
    private int codeOffset(int entry){return strings+data.getInt(16+entry*12);}
    private int digitAt(int offset){int c=data.get(offset)&255;return c>='a'&&c<='z'?KEYS.charAt(c-'a'):0;}
    private int compare(int a,int b){
        int x=codeOffset(a),y=codeOffset(b);
        for(;;x++,y++){int p=digitAt(x),q=digitAt(y);if(p!=q)return p-q;if(p==0)return 0;}
    }
    private void sort(int lo,int hi){
        while(lo<hi){
            int i=lo,j=hi,pivot=entries[(lo+hi)>>>1];
            while(i<=j){while(compare(entries[i],pivot)<0)i++;while(compare(entries[j],pivot)>0)j--;if(i<=j){int t=entries[i];entries[i++]=entries[j];entries[j--]=t;}}
            if(j-lo<hi-i){if(lo<j)sort(lo,j);lo=i;}else{if(i<hi)sort(i,hi);hi=j;}
        }
    }
    private int compareKey(int entry,String key){
        int offset=codeOffset(entry);
        for(int i=0;i<key.length();i++){int c=digitAt(offset+i);if(c!=key.charAt(i))return c-key.charAt(i);}
        return digitAt(offset+key.length())==0?0:1;
    }
    private boolean startsWith(int entry,String key){
        int offset=codeOffset(entry);for(int i=0;i<key.length();i++)if(digitAt(offset+i)!=key.charAt(i))return false;return true;
    }
    private String read(int offset){
        int end=offset;while(end<data.capacity()&&data.get(end)!=0)end++;
        byte[] bytes=new byte[end-offset];for(int i=0;i<bytes.length;i++)bytes[i]=data.get(offset+i);
        return new String(bytes,StandardCharsets.UTF_8);
    }
    private int lower(String key){int lo=0,hi=entries.length;while(lo<hi){int m=(lo+hi)>>>1;if(compareKey(entries[m],key)<0)lo=m+1;else hi=m;}return lo;}
    private void collect(String key,Map<String,Float> found,boolean prefix){
        int scanned=0;
        for(int i=lower(key);i<entries.length;i++){
            int e=entries[i];if(!startsWith(e,key))break;
            boolean exact=compareKey(e,key)==0;
            if(!exact&&(!prefix||scanned++>=4096))break;
            float score=data.getFloat(24+e*12)-(exact?0:14);
            String text=read(strings+data.getInt(20+e*12));found.put(text,Math.max(found.getOrDefault(text,-999f),score));
        }
        if(hotwords.containsKey(key))found.put(hotwords.get(key),110f);
    }
    List<String> query(String key){
        Map<String,Float> found=new HashMap<>();collect(key,found,true);
        if(key.isEmpty())return new ArrayList<>();
        int at=lower(key);boolean exact=(at<entries.length&&compareKey(entries[at],key)==0)||hotwords.containsKey(key);
        if(!exact&&key.length()>=4&&key.length()<=48){
            float[] score=new float[key.length()+1];Arrays.fill(score,-99999);score[0]=0;
            String[] text=new String[key.length()+1];text[0]="";
            for(int end=1;end<=key.length();end++)for(int start=Math.max(0,end-24);start<end;start++){
                if(text[start]==null)continue;
                Map<String,Float> part=new HashMap<>();collect(key.substring(start,end),part,false);
                for(Map.Entry<String,Float> e:part.entrySet())if(score[start]+e.getValue()-34>score[end]){
                    score[end]=score[start]+e.getValue()-34;text[end]=text[start]+e.getKey();
                }
            }
            if(text[key.length()]!=null)found.put(text[key.length()],Math.max(found.getOrDefault(text[key.length()],-999f),score[key.length()]));
        }
        List<String> list=new ArrayList<>(found.keySet());
        list.sort((a,b)->{int c=Float.compare(found.get(b),found.get(a));return c==0?a.compareTo(b):c;});
        names.merge(key,true,exact,list);
        return list;
    }
}
