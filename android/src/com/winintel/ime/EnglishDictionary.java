// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/** Local prefix completion shared with the Windows word list. */
public final class EnglishDictionary {
    private final TreeMap<String,String> words=new TreeMap<>();
    private final Map<String,Integer> weights=new HashMap<>();
    public EnglishDictionary(InputStream input) throws IOException {
        try(BufferedReader reader=new BufferedReader(new InputStreamReader(input,StandardCharsets.UTF_8))){
            String row;
            while((row=reader.readLine())!=null){
                String[] fields=row.split("\t");
                if(fields.length!=3)continue;
                words.put(fields[0],fields[1]);weights.put(fields[0],Integer.parseInt(fields[2]));
            }
        }
    }
    public List<String> query(String prefix){
        String key=prefix.toLowerCase(Locale.ROOT);
        List<String> matches=new ArrayList<>();
        if(key.isEmpty())return matches;
        for(String word:words.tailMap(key).keySet()){
            if(!word.startsWith(key))break;
            matches.add(word);
        }
        matches.sort((a,b)->{
            if(a.equals(key))return b.equals(key)?0:-1;
            if(b.equals(key))return 1;
            int score=Integer.compare(weights.get(b),weights.get(a));
            return score!=0?score:Integer.compare(a.length(),b.length())!=0?Integer.compare(a.length(),b.length()):a.compareTo(b);
        });
        List<String> result=new ArrayList<>();
        for(String word:matches){
            String text=words.get(word);
            if(prefix.equals(prefix.toUpperCase(Locale.ROOT)))text=text.toUpperCase(Locale.ROOT);
            else if(Character.isUpperCase(prefix.charAt(0)))text=Character.toUpperCase(text.charAt(0))+text.substring(1);
            if(!result.contains(text))result.add(text);
            if(result.size()==90)break;
        }
        return result;
    }
}
