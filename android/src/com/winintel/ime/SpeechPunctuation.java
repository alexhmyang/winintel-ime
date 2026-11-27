// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;

/** Conservative local punctuation; preserves model punctuation and spoken commands. */
public final class SpeechPunctuation {
    public static String clean(String input){
        String text=input.replaceAll("<\\|[^>]*\\|>","").trim();
        // Chinese/English dictation: discard stray kana without changing Latin hardware names.
        text=text.replaceAll("[\\u3040-\\u30ff\\u31f0-\\u31ff]+","").trim();
        for(int i=0;i<3;i++){
            text=text.replaceAll("(^|[\\s，。！？,!?])(?:呃+|嗯+|啊+)(?:[\\s，。,.]+|$)","$1");
            text=text.replaceAll("^(?:呃+|嗯+)[\\s，,.]*","");
            text=text.replaceAll("(^|[\\s，。！？,!?])(?:这个|那个)(?:[\\s，,。.]+|(?=这个|那个)|$)","$1");
        }
        text=text.replaceAll("[。]{2,}","。").replaceAll("[，,]{2,}","，")
            .replaceAll("[，,]+(?=[。！？!?])","").replaceAll("^[\\s，,。]+","").trim();
        return text.replaceAll("[\\p{P}\\p{Z}\\s]","").isEmpty()?"":text;
    }
    public static String apply(String input,boolean sentenceEnd){
        String text=clean(input);
        if(text.isEmpty())return text;
        if(text.replaceAll("[\\s，。！？,.!?]","").matches("清除|清空|撤销"))return text;
        String[][] commands={{"逗号","，"},{"句号","。"},{"问号","？"},{"感叹号","！"},{"分号","；"},{"冒号","："},{"换行","\n"},{"下一段","\n\n"}};
        for(String[] c:commands)text=text.replaceAll(c[0]+"[，。,.]?",c[1]);
        boolean chinese=text.matches("(?s).*[\\u3400-\\u9fff].*");
        if(chinese){
            text=text.replaceAll("(?<=[\\u3400-\\u9fff]),","，").replaceAll("(?<=[\\u3400-\\u9fff])\\?","？").replaceAll("(?<=[\\u3400-\\u9fff])!","！");
            text=text.replaceAll("([，。！？；：])\\s+(?=[\\u3400-\\u9fff])","$1");
        }
        if(!text.matches("(?s).*[，。！？；：,.!?;:\\n]$")){
            if(sentenceEnd){
                boolean question=chinese?text.matches("(?s).*(吗|么|呢|是不是|可不可以|能不能)$")||text.matches("(?s)^(请问|为什么|怎么|哪里|谁|多少|什么时候).*"):text.matches("(?is)^(what|why|how|when|where|who|can you|could you|do you|is it|are you)\\b.*");
                text+=chinese?(question?"？":"。"):(question?"?":".");
            }else text+=chinese?"，":",";
        }
        return text;
    }
    public static String join(String previous,String next){
        if(previous.isEmpty()||next.isEmpty())return previous+next;
        boolean latin=previous.matches("(?s).*[A-Za-z0-9,.!?]$")&&next.matches("(?s)^[A-Za-z0-9].*");
        return previous+(latin?" ":"")+next;
    }
}
