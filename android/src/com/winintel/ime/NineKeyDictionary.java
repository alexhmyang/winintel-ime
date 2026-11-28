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
    private final Map<Character,List<String>> readings=new HashMap<>();
    private final Map<String,List<Map.Entry<String,Float>>> segments=new HashMap<>();
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
        File cache=new File(file.getParentFile(),"t9-index-v2.bin");
        boolean loaded=false;
        if(cache.length()==16L+4L*count)try(DataInputStream in=new DataInputStream(new BufferedInputStream(new FileInputStream(cache)))){
            if(in.readLong()==file.length()&&in.readLong()==data.getLong(8)){
                loaded=true;for(int i=0;i<count;i++){entries[i]=in.readInt();if(entries[i]<0||entries[i]>=count){loaded=false;break;}}
            }
        }catch(IOException ignored){}
        if(!loaded){
            for(int i=0;i<count;i++)entries[i]=i;
            sort(0,count-1);
            try(DataOutputStream out=new DataOutputStream(new BufferedOutputStream(new FileOutputStream(cache)))){
                out.writeLong(file.length());out.writeLong(data.getLong(8));for(int e:entries)out.writeInt(e);
            }catch(IOException ignored){}
        }
        File readingFile=new File(file.getParentFile(),"character-readings.tsv");
        if(readingFile.isFile())try(BufferedReader r=new BufferedReader(new InputStreamReader(new FileInputStream(readingFile),StandardCharsets.UTF_8))){
            String row;while((row=r.readLine())!=null){String[] f=row.split("\t");if(f.length==2&&f[0].length()==1){List<String> ds=new ArrayList<>();for(String s:f[1].split(" "))ds.add(digits(s));readings.put(f[0].charAt(0),ds);}}
        }
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
        // Keep the strongest prefix entries across the whole matching range.
        // Stopping at the first 4096 made frequent words disappear by sort order.
        PriorityQueue<Integer> extensions=new PriorityQueue<>((a,b)->Float.compare(data.getFloat(24+a*12),data.getFloat(24+b*12)));
        for(int i=lower(key);i<entries.length;i++){
            int e=entries[i];if(!startsWith(e,key))break;
            boolean exact=compareKey(e,key)==0;
            if(!exact){
                if(!prefix)break;
                if(extensions.size()<512)extensions.add(e);
                else if(data.getFloat(24+e*12)>data.getFloat(24+extensions.peek()*12)){extensions.poll();extensions.add(e);}
                continue;
            }
            float score=data.getFloat(24+e*12)-(exact?0:14);
            String text=read(strings+data.getInt(20+e*12));found.put(text,Math.max(found.getOrDefault(text,-999f),score));
        }
        for(int e:extensions){String text=read(strings+data.getInt(20+e*12));float score=data.getFloat(24+e*12)-14;found.put(text,Math.max(found.getOrDefault(text,-999f),score));}
        if(hotwords.containsKey(key))found.put(hotwords.get(key),110f);
    }
    private boolean full(String key,String text,int at,int ch,Set<Integer> failed){
        if(ch==text.length())return at==key.length();
        int state=ch*(key.length()+1)+at;if(failed.contains(state))return false;
        List<String> ds=readings.get(text.charAt(ch));
        if(ds!=null)for(String s:ds)if(key.startsWith(s,at)&&full(key,text,at+s.length(),ch+1,failed))return true;
        failed.add(state);return false;
    }
    private List<Map.Entry<String,Float>> segment(String key){
        List<Map.Entry<String,Float>> cached=segments.get(key);if(cached!=null)return cached;
        Map<String,Float> found=new HashMap<>();collect(key,found,false);
        List<Map.Entry<String,Float>> list=new ArrayList<>();
        for(Map.Entry<String,Float> e:found.entrySet()){
            boolean common=true;for(char c:e.getKey().toCharArray())if(c<0x4e00||c>0x9fff){common=false;break;}
            if(common&&key.length()>=e.getKey().length()*2&&full(key,e.getKey(),0,0,new HashSet<Integer>()))list.add(e);
        }
        list.sort((a,b)->{int c=Float.compare(b.getValue(),a.getValue());return c!=0?c:a.getKey().compareTo(b.getKey());});
        if(list.size()>32)list=new ArrayList<>(list.subList(0,32));
        if(segments.size()>4096)segments.clear();segments.put(key,list);return list;
    }
    List<String> query(String key){
        return query(key,true);
    }
    private List<String> query(String key,boolean expand){
        if(key.isEmpty())return new ArrayList<>();
        Map<String,Float> found=new HashMap<>();collect(key,found,true);
        boolean exactFull=false;for(String t:found.keySet())if(full(key,t,0,0,new HashSet<Integer>())){exactFull=true;break;}
        int at=lower(key);boolean exact=(at<entries.length&&compareKey(entries[at],key)==0)||hotwords.containsKey(key);
        Set<String> matchedCorrections=new LinkedHashSet<>();
        if(found.size()<50&&key.length()>=3&&key.length()<=24){
            Set<String> edits=new HashSet<>();
            for(int i=0;i<key.length();i++){
                edits.add(key.substring(0,i)+key.substring(i+1));
                if(i+1<key.length())edits.add(key.substring(0,i)+key.charAt(i+1)+key.charAt(i)+key.substring(i+2));
                for(char c='2';c<='9';c++){
                    edits.add(key.substring(0,i)+c+key.substring(i+1));
                    edits.add(key.substring(0,i)+c+key.substring(i));
                }
            }
            for(char c='2';c<='9';c++)edits.add(key+c);edits.remove(key);
            for(String edit:edits){Map<String,Float> corrected=new HashMap<>();collect(edit,corrected,false);
                if(!corrected.isEmpty())matchedCorrections.add(edit);
                for(Map.Entry<String,Float> e:corrected.entrySet())found.put(e.getKey(),Math.max(found.getOrDefault(e.getKey(),-999f),e.getValue()-25));
            }
        }
        if((!exact||found.size()<50)&&key.length()>=4&&key.length()<=48){
            List<List<Map.Entry<String,Float>>> paths=new ArrayList<>();
            for(int i=0;i<=key.length();i++)paths.add(new ArrayList<>());
            paths.get(0).add(new AbstractMap.SimpleEntry<>("",0f));
            for(int end=1;end<=key.length();end++){
                Map<String,Float> next=new HashMap<>();
                for(int start=Math.max(0,end-24);start<end;start++)if(!paths.get(start).isEmpty()){
                    for(Map.Entry<String,Float> before:paths.get(start))for(Map.Entry<String,Float> e:segment(key.substring(start,end))){
                        String text=before.getKey()+e.getKey();float score=before.getValue()+e.getValue()*e.getKey().length()-50;
                        next.put(text,Math.max(next.getOrDefault(text,-99999f),score));
                    }
                }
                List<Map.Entry<String,Float>> list=new ArrayList<>(next.entrySet());
                list.sort((a,b)->{if(a.getKey().length()!=b.getKey().length())return Integer.compare(a.getKey().length(),b.getKey().length());int c=Float.compare(b.getValue(),a.getValue());return c!=0?c:a.getKey().compareTo(b.getKey());});
                if(list.size()>128)list=new ArrayList<>(list.subList(0,128));paths.set(end,list);
            }
            for(Map.Entry<String,Float> e:paths.get(key.length()))found.put(e.getKey(),Math.max(found.getOrDefault(e.getKey(),-999f),e.getValue()/e.getKey().length()-100));
        }
        if(expand&&found.size()<50)for(String edit:matchedCorrections){
            float score=-50f;for(String t:query(edit,false)){if(!found.containsKey(t))found.put(t,score);score-=0.05f;}
            if(found.size()>=50)break;
        }
        if(expand)for(int length=key.length()-1;length>=2&&found.size()<50;length--){
            float score=-220f;for(String t:query(key.substring(0,length),false)){if(!found.containsKey(t))found.put(t,score);score-=0.05f;}
        }
        Set<String> preferred=new HashSet<>();
        if(!exactFull&&key.length()>=6)for(String t:found.keySet())if(t.length()==2&&full(key,t,0,0,new HashSet<Integer>()))preferred.add(t);
        List<String> list=new ArrayList<>(found.keySet());
        list.sort((a,b)->{if(preferred.contains(a)!=preferred.contains(b))return preferred.contains(a)?-1:1;int c=Float.compare(found.get(b),found.get(a));return c==0?a.compareTo(b):c;});
        names.merge(key,true,exact,list);
        return list;
    }
}
