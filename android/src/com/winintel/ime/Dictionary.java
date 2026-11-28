// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;

import java.io.*;
import java.nio.*;
import java.nio.channels.FileChannel;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Portable read-only WIME0002 dictionary. No network or Android dependency. */
public final class Dictionary {
    private final ByteBuffer data;
    private final int count, strings;
    private final NameCandidates names;
    private final Map<String,String> hotwords=new HashMap<>();
    private final Map<Character,List<String>> readings=new HashMap<>();
    private final Map<String,List<Map.Entry<String,Float>>> segments=new HashMap<>();
    private float correctionPenalty;
    public void loadHotwords(InputStream input) throws IOException {
        try(BufferedReader reader=new BufferedReader(new InputStreamReader(input,StandardCharsets.UTF_8))){
            String row;while((row=reader.readLine())!=null){String[] fields=row.split("\t");if(fields.length==2)hotwords.put(fields[0],fields[1]);}
        }
    }
    public Dictionary(File file) throws IOException {
        names=new NameCandidates(new File(file.getParentFile(),"name-parts.tsv"));
        File readingFile=new File(file.getParentFile(),"character-readings.tsv");
        if(readingFile.isFile())try(BufferedReader r=new BufferedReader(new InputStreamReader(new FileInputStream(readingFile),StandardCharsets.UTF_8))){
            String row;while((row=r.readLine())!=null){String[] f=row.split("\t");if(f.length==2&&f[0].length()==1)readings.put(f[0].charAt(0),Arrays.asList(f[1].split(" ")));}
        }
        try (FileInputStream in = new FileInputStream(file)) {
            data = in.getChannel().map(FileChannel.MapMode.READ_ONLY,0,in.getChannel().size()).order(ByteOrder.LITTLE_ENDIAN);
        }
        if (data.capacity()<16 || !readString(0,8).equals("WIME0002")) throw new IOException("Invalid dictionary");
        count=data.getInt(8); strings=data.getInt(12);
        if(count<0 || 16L+count*12L!=strings || strings>=data.capacity()) throw new IOException("Invalid index");
    }
    private String readString(int at, int limit) {
        int end=at;
        while(end<data.capacity() && end-at<limit && data.get(end)!=0) end++;
        byte[] bytes=new byte[end-at];
        for(int i=0;i<bytes.length;i++) bytes[i]=data.get(at+i);
        return new String(bytes, StandardCharsets.UTF_8);
    }
    private String code(int i){return readString(strings+data.getInt(16+i*12),65);}
    private int lower(String key) {
        int lo=0,hi=count;
        while(lo<hi){int m=(lo+hi)>>>1;if(code(m).compareTo(key)<0)lo=m+1;else hi=m;}
        return lo;
    }
    private void collect(String key,float penalty,Map<String,Float> found){
        int scanned=0;
        for(int at=lower(key);at<count && code(at).equals(key) && (penalty==0||scanned++<24);at++){
            String text=readString(strings+data.getInt(20+at*12),193);
            float score=data.getFloat(24+at*12)-penalty;
            found.put(text,Math.max(found.getOrDefault(text,-999f),score));
        }
    }
    private boolean fullSpelling(String key,String text,int at,int ch,Set<Integer> failed){
        if(ch==text.length())return at==key.length();
        int state=ch*(key.length()+1)+at;if(failed.contains(state))return false;
        List<String> syllables=readings.get(text.charAt(ch));
        if(syllables!=null)for(String s:syllables)if(key.startsWith(s,at)&&fullSpelling(key,text,at+s.length(),ch+1,failed))return true;
        failed.add(state);return false;
    }
    private List<Map.Entry<String,Float>> segment(String key){
        List<Map.Entry<String,Float>> cached=segments.get(key);if(cached!=null)return cached;
        Map<String,Float> parts=new HashMap<>();collect(key,0,parts);
        List<Map.Entry<String,Float>> result=new ArrayList<>();
        for(Map.Entry<String,Float> p:parts.entrySet()){
            String t=p.getKey();
            if(t.isEmpty()||key.length()<t.length()*2)continue;
            boolean common=true;for(char c:t.toCharArray())if(c<0x4e00||c>0x9fff){common=false;break;}if(!common)continue;
            if(!fullSpelling(key,t,0,0,new HashSet<Integer>()))continue;
            result.add(p);
        }
        result.sort((a,b)->{int c=Float.compare(b.getValue(),a.getValue());return c!=0?c:a.getKey().compareTo(b.getKey());});
        if(result.size()>32)result=new ArrayList<>(result.subList(0,32));
        if(segments.size()>4096)segments.clear();segments.put(key,result);return result;
    }
    public List<String> query(String input) {
        return query(input,true);
    }
    private List<String> query(String input,boolean expand) {
        String key=input.toLowerCase(Locale.ROOT).replace("'","");
        Map<String,Float> found=new HashMap<>();
        if(key.isEmpty())return new ArrayList<>();
        collect(key,0,found);
        Set<String> exactSpelled=new HashSet<>();
        for(String t:found.keySet())if(fullSpelling(key,t,0,0,new HashSet<Integer>()))exactSpelled.add(t);
        if(key.length()==1){
            Set<String> syllables=new HashSet<>();
            for(List<String> r:readings.values())for(String s:r)if(s.startsWith(key))syllables.add(s);
            for(String s:syllables)collect(s,9,found);
        }
        if(hotwords.containsKey(key))found.put(hotwords.get(key),110f);
        int exactCount=found.size();
        boolean exact=exactCount>0;
        if(key.length()>0){
            int at=lower(key), scanned=0;
            while(at<count && scanned++<128){
                String c=code(at);if(!c.startsWith(key))break;
                if(!c.equals(key))collect(c,9+Math.min(6,c.length()-key.length()),found);
                do{at++;}while(at<count && code(at).equals(c));
            }
        }
        Set<String> corrections=new LinkedHashSet<>();
        correctionPenalty=exactCount>=9?100f:0f;
        if(exactCount<50 && key.length()>=3 && key.length()<=24){
            for(int i=0;i<key.length();i++){
                collectCorrection(key.substring(0,i)+key.substring(i+1),13,found,corrections);
                if(i+1<key.length())collectCorrection(key.substring(0,i)+key.charAt(i+1)+key.charAt(i)+key.substring(i+2),12,found,corrections);
                for(char c='a';c<='z';c++)if(c!=key.charAt(i))collectCorrection(key.substring(0,i)+c+key.substring(i+1),14,found,corrections);
                for(char c='a';c<='z';c++)collectCorrection(key.substring(0,i)+c+key.substring(i),13,found,corrections);
            }
            for(char c='a';c<='z';c++)collectCorrection(key+c,13,found,corrections);
        }
        Map<String,Float> legacyScores=new HashMap<>();
        if(exactCount<50 && key.length()>=4 && key.length()<=256){
            int beam=128;
            List<List<Map.Entry<String,Float>>> paths=new ArrayList<>();
            List<List<Map.Entry<String,Float>>> legacy=new ArrayList<>();
            for(int i=0;i<=key.length();i++){paths.add(new ArrayList<>());legacy.add(new ArrayList<>());}
            paths.get(0).add(new AbstractMap.SimpleEntry<>("",0f));
            legacy.get(0).add(new AbstractMap.SimpleEntry<>("",0f));
            for(int end=1;end<=key.length();end++){
                Map<String,Float> foundPaths=new HashMap<>();
                for(int start=Math.max(0,end-24);start<end;start++){
                    if(paths.get(start).isEmpty())continue;
                    List<Map.Entry<String,Float>> choices=segment(key.substring(start,end));
                    for(Map.Entry<String,Float> before:paths.get(start))for(Map.Entry<String,Float> p:choices){
                        String text=before.getKey()+p.getKey();
                        float score=before.getValue()+p.getValue()*p.getKey().length()-50;
                        foundPaths.put(text,Math.max(foundPaths.getOrDefault(text,-9999f),score));
                    }
                    for(Map.Entry<String,Float> before:legacy.get(start))for(int i=0;i<Math.min(4,choices.size());i++){
                        Map.Entry<String,Float> p=choices.get(i);
                        legacy.get(end).add(new AbstractMap.SimpleEntry<>(before.getKey()+p.getKey(),before.getValue()+p.getValue()-34-(p.getKey().length()==1?5:0)));
                    }
                }
                List<Map.Entry<String,Float>> next=new ArrayList<>(foundPaths.entrySet());
                next.sort((a,b)->{if(a.getKey().length()!=b.getKey().length())return Integer.compare(a.getKey().length(),b.getKey().length());int c=Float.compare(b.getValue(),a.getValue());return c!=0?c:a.getKey().compareTo(b.getKey());});
                if(next.size()>beam)next=new ArrayList<>(next.subList(0,beam));paths.set(end,next);
                List<Map.Entry<String,Float>> old=legacy.get(end);
                old.sort((a,b)->{int c=Float.compare(b.getValue(),a.getValue());return c!=0?c:a.getKey().compareTo(b.getKey());});
                if(old.size()>8)legacy.set(end,new ArrayList<>(old.subList(0,8)));
            }
            for(Map.Entry<String,Float> p:legacy.get(key.length()))if(fullSpelling(key,p.getKey(),0,0,new HashSet<Integer>())){
                legacyScores.put(p.getKey(),Math.max(legacyScores.getOrDefault(p.getKey(),-9999f),p.getValue()));
                found.put(p.getKey(),Math.max(found.getOrDefault(p.getKey(),-999f),p.getValue()-(exactCount>0?100:0)));
            }
            for(Map.Entry<String,Float> p:paths.get(key.length()))found.put(p.getKey(),Math.max(found.getOrDefault(p.getKey(),-999f),p.getValue()/p.getKey().length()-100));
        }
        if(expand&&found.size()<50){
            for(String corrected:corrections){
                float score=-50f;
                for(String t:query(corrected,false)){if(!found.containsKey(t))found.put(t,score);score-=0.05f;}
                if(found.size()>=50)break;
            }
        }
        for(String text:names.query(key,false)){
            String[] spelling=names.spelling(text);float prior=names.prior(text);
            if(spelling!=null&&spelling[0].equals(key)&&prior>0)found.put(text,Math.max(found.getOrDefault(text,-999f),32+prior));
        }
        List<String> result=new ArrayList<>(found.keySet());
        Set<String> preferred=new HashSet<>();
        if(exactSpelled.isEmpty()&&key.length()>=6)for(String t:result)if(t.length()==2&&legacyScores.containsKey(t))preferred.add(t);
        result.sort((a,b)->{
            if(preferred.contains(a)!=preferred.contains(b))return preferred.contains(a)?-1:1;
            int c=Float.compare(found.get(b),found.get(a));return c!=0?c:a.compareTo(b);
        });
        names.merge(key,false,exact,result);
        if(result.isEmpty())result.add(input);
        return result;
    }
    private void collectCorrection(String key,float penalty,Map<String,Float> found,Set<String> corrections){
        int at=lower(key);if(at<count&&code(at).equals(key))corrections.add(key);
        collect(key,penalty+correctionPenalty,found);
    }
    public List<String> expandCandidates(String input,List<String> seed){
        List<String> result=new ArrayList<>(seed);Set<String> seen=new HashSet<>(seed);
        if(result.size()>=50)return result;
        for(String text:seed){
            StringBuilder full=new StringBuilder();
            for(char c:text.toCharArray()){List<String> r=readings.get(c);if(r==null||r.isEmpty()){full.setLength(0);break;}full.append(r.get(0));}
            if(full.length()==0||full.toString().equals(input.replace("'","")))continue;
            for(String t:query(full.toString(),false))if(seen.add(t))result.add(t);
            if(result.size()>=50)break;
        }
        for(int length=input.length()-1;length>=2&&result.size()<50;length--)
            for(String t:query(input.substring(0,length),false))if(seen.add(t))result.add(t);
        return result;
    }
}
