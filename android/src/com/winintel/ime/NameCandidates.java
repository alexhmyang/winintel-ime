// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Bounded offline surname + one/two given characters, full/mixed/initial/T9. */
final class NameCandidates {
    private static final class Part {
        String text;String[] syllables;float rank;
        Part(String[] f){text=f[1];syllables=f[2].split(" ");rank=Float.parseFloat(f[3]);}
    }
    private final List<Part> surnames=new ArrayList<>(),given=new ArrayList<>();
    private final Map<String,List<String>> cache=new HashMap<>();
    private final Map<String,Float> priors=new HashMap<>();
    float prior(String text){
        for(Part s:surnames)if(text.length()==s.text.length()+2&&text.startsWith(s.text)){
            Float p=priors.get(text.substring(s.text.length()));if(p!=null)return p-s.rank*.05f;
        }return 0;
    }
    String[] spelling(String text){
        for(Part surname:surnames)if(text.startsWith(surname.text)&&text.length()>surname.text.length()&&text.length()<=surname.text.length()+2){
            StringBuilder full=new StringBuilder(),initials=new StringBuilder();
            for(String s:surname.syllables){full.append(s);initials.append(s.charAt(0));}
            boolean valid=true;
            for(int i=surname.text.length();i<text.length();i++){
                Part chosen=null;for(Part p:given)if(p.text.equals(text.substring(i,i+1))){chosen=p;break;}
                if(chosen==null){valid=false;break;}full.append(chosen.syllables[0]);initials.append(chosen.syllables[0].charAt(0));
            }
            if(valid)return new String[]{full.toString(),initials.toString()};
        }
        return null;
    }
    private static final class Step {
        Part part;int end;Step(Part p,int e){part=p;end=e;}
    }
    NameCandidates(File file)throws IOException{
        if(!file.isFile())return;
        try(BufferedReader r=new BufferedReader(new InputStreamReader(new FileInputStream(file),StandardCharsets.UTF_8))){
            String row;while((row=r.readLine())!=null){String[] f=row.split("\t");if(f.length==4){
                if(f[0].equals("N"))priors.put(f[1],Float.parseFloat(f[3]));
                else (f[0].equals("S")?surnames:given).add(new Part(f));
            }}
        }
    }
    private List<Integer> match(Part part,String key,int at,boolean t9){
        List<Integer> positions=Collections.singletonList(at);
        for(String s:part.syllables){
            String full=t9?NineKeyDictionary.digits(s):s;
            String initial=t9?NineKeyDictionary.digits(s.substring(0,1)):s.substring(0,1);
            Set<Integer> next=new LinkedHashSet<>();
            for(int p:positions){
                if(key.startsWith(full,p))next.add(p+full.length());
                if(key.startsWith(initial,p))next.add(p+initial.length());
            }
            positions=new ArrayList<>(next);
        }
        return positions;
    }
    private void visit(String key,boolean t9,int at,int depth,String text,float rank,Map<String,Float> found,Map<Integer,List<Step>> transitions){
        if(at==key.length()){if(depth>0)found.put(text,Math.min(found.getOrDefault(text,Float.MAX_VALUE),rank));return;}
        if(depth==2)return;
        List<Step> next=transitions.get(at);
        if(next==null){next=new ArrayList<>();for(Part p:given)for(int end:match(p,key,at,t9))next.add(new Step(p,end));transitions.put(at,next);}
        for(Step s:next)visit(key,t9,s.end,depth+1,text+s.part.text,rank+s.part.rank*(depth>0?1f:.15f),found,transitions);
    }
    List<String> query(String key,boolean t9){
        if(key.length()<2||key.length()>24||!key.matches(t9?"[2-9]+":"[a-z]+"))return Collections.emptyList();
        String cacheKey=(t9?"9:":"")+key;
        if(cache.containsKey(cacheKey))return cache.get(cacheKey);
        Map<String,Float> found=new HashMap<>();
        Map<Integer,List<Step>> transitions=new HashMap<>();
        for(Part p:surnames)for(int end:match(p,key,0,t9))if(end<key.length())visit(key,t9,end,0,p.text,p.rank,found,transitions);
        List<String> result=new ArrayList<>(found.keySet());
        List<String> legacy=new ArrayList<>(result);
        legacy.sort((a,b)->{int c=Float.compare(found.get(a),found.get(b));return c!=0?c:a.compareTo(b);});
        for(String text:found.keySet())found.put(text,found.get(text)-Math.max(0,prior(text))*8);
        result.sort((a,b)->{int c=Float.compare(found.get(a),found.get(b));return c!=0?c:a.compareTo(b);});
        if(!t9&&key.length()==3){
            List<String> diverse=new ArrayList<>(result.subList(0,Math.min(3,result.size())));
            for(String text:legacy.subList(0,Math.min(3,legacy.size())))if(!diverse.contains(text))diverse.add(text);
            for(String text:result)if(!diverse.contains(text))diverse.add(text);result=diverse;
        }
        if(result.size()>512)result=new ArrayList<>(result.subList(0,512));
        if(cache.size()>=128)cache.clear();cache.put(cacheKey,result);return result;
    }
    void merge(String key,boolean t9,boolean exact,List<String> result){
        Set<String> seen=new HashSet<>(result);
        int slots=!t9&&key.length()==3?6:3;
        int at=Math.min(slots==6?3:5,result.size()),visible=0;
        for(String text:query(key,t9))if(seen.add(text)){
            if(visible++<slots)result.add(at++,text);else result.add(text);
        }
    }
}
