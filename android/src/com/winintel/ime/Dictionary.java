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
    private final Map<String,String> hotwords=new HashMap<>();
    public void loadHotwords(InputStream input) throws IOException {
        try(BufferedReader reader=new BufferedReader(new InputStreamReader(input,StandardCharsets.UTF_8))){
            String row;while((row=reader.readLine())!=null){String[] fields=row.split("\t");if(fields.length==2)hotwords.put(fields[0],fields[1]);}
        }
    }
    public Dictionary(File file) throws IOException {
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
        for(int at=lower(key);at<count && code(at).equals(key);at++){
            String text=readString(strings+data.getInt(20+at*12),193);
            float score=data.getFloat(24+at*12)-penalty;
            found.put(text,Math.max(found.getOrDefault(text,-999f),score));
        }
    }
    public List<String> query(String input) {
        String key=input.toLowerCase(Locale.ROOT).replace("'","");
        Map<String,Float> found=new HashMap<>();
        if(key.isEmpty())return new ArrayList<>();
        collect(key,0,found);
        if(hotwords.containsKey(key))found.put(hotwords.get(key),110f);
        if(key.length()>1){
            int at=lower(key), scanned=0;
            while(at<count && scanned++<128){
                String c=code(at);if(!c.startsWith(key))break;
                if(!c.equals(key))collect(c,9+Math.min(6,c.length()-key.length()),found);
                do{at++;}while(at<count && code(at).equals(c));
            }
        }
        if(key.length()>=3 && key.length()<=24){
            for(int i=0;i<key.length();i++){
                collect(key.substring(0,i)+key.substring(i+1),13,found);
                if(i+1<key.length())collect(key.substring(0,i)+key.charAt(i+1)+key.charAt(i)+key.substring(i+2),12,found);
                for(char c='a';c<='z';c++)collect(key.substring(0,i)+c+key.substring(i+1),14,found);
                for(char c='a';c<='z';c++)collect(key.substring(0,i)+c+key.substring(i),13,found);
            }
            for(char c='a';c<='z';c++)collect(key+c,13,found);
        }
        if(found.isEmpty() && key.length()<=48){
            float[] scores=new float[key.length()+1];Arrays.fill(scores,-9999);scores[0]=0;
            String[] texts=new String[key.length()+1];texts[0]="";
            for(int end=1;end<=key.length();end++)for(int start=Math.max(0,end-24);start<end;start++){
                if(texts[start]==null)continue;
                Map<String,Float> part=new HashMap<>();collect(key.substring(start,end),0,part);
                for(Map.Entry<String,Float> p:part.entrySet()){
                    if(p.getKey().isEmpty() || p.getKey().charAt(0)<0x3400)continue;
                    float score=scores[start]+p.getValue()-34;
                    if(score>scores[end]){scores[end]=score;texts[end]=texts[start]+p.getKey();}
                }
            }
            if(texts[key.length()]!=null)found.put(texts[key.length()],scores[key.length()]);
        }
        List<String> result=new ArrayList<>(found.keySet());
        result.sort((a,b)->{int c=Float.compare(found.get(b),found.get(a));return c!=0?c:a.compareTo(b);});
        if(result.isEmpty())result.add(input);
        return result.size()>90?new ArrayList<>(result.subList(0,90)):result;
    }
}
