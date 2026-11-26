// SPDX-License-Identifier: GPL-3.0-or-later
package com.winintel.ime.check;
import android.app.Instrumentation;
import android.os.Bundle;
import android.content.Context;
import com.winintel.ime.EnglishDictionary;
import com.winintel.ime.NewsFeed;
import org.json.*;
import java.io.*;
import java.util.*;

public final class FeatureCheck extends Instrumentation {
    private int passed;
    private void check(boolean condition,String name){if(!condition)throw new AssertionError(name);passed++;}
    private interface Action{void run() throws Exception;}
    private void reject(Action action,String name) throws Exception {boolean rejected=false;try{action.run();}catch(Exception e){rejected=true;}check(rejected,name);}
    @Override public void onCreate(Bundle args){super.onCreate(args);start();}
    @Override public void onStart(){
        Bundle result=new Bundle();
        try{
            Context context=getTargetContext();
            EnglishDictionary english=new EnglishDictionary(context.getAssets().open("english-words.tsv"));
            check(english.query("hel").get(0).equals("hello"),"hello prefix");
            check(english.query("deepse").get(0).equals("DeepSeek"),"DeepSeek");
            check(english.query("nvl").contains("NVLink"),"NVLink");
            check(english.query("HEL").get(0).equals("HELLO"),"uppercase");
            check(english.query("Hel").get(0).equals("Hello"),"initial capital");
            check(english.query("hello").get(0).equals("hello"),"exact word");
            check(english.query("zzzzzzzz").isEmpty(),"unknown");
            File dictionaryFile=new File(context.getCacheDir(),"feature-dictionary.bin");
            try(InputStream in=context.getAssets().open("dictionary.bin");OutputStream out=new FileOutputStream(dictionaryFile)){byte[] block=new byte[65536];int n;while((n=in.read(block))!=-1)out.write(block,0,n);}
            com.winintel.ime.Dictionary dictionary=new com.winintel.ime.Dictionary(dictionaryFile);
            dictionary.loadHotwords(context.getAssets().open("hot-chinese.tsv"));
            check(dictionary.query("nihao").contains("你好"),"pinyin regression");
            check(dictionary.query("zhinengti").contains("智能体"),"Chinese hotword");
            String date="2026-11-26T12:00:00Z";
            String json="{\"schema\":1,\"items\":[{\"title\":\"中文新闻\",\"url\":\"https://example.com/a\",\"source\":\"https://example.com\",\"published_at\":\""+date+"\"}]}";
            check(NewsFeed.parseJson(json).getJSONObject(0).getString("title").equals("中文新闻"),"own API");
            reject(()->NewsFeed.parseJson(json.replace("\"schema\":1","\"schema\":2")),"schema rejection");
            reject(()->NewsFeed.parseJson(json.replace("https://example.com/a","http://example.com/a")),"HTTP rejection");
            reject(()->NewsFeed.parseJson(json.replace(date,"invalid-date")),"date rejection");
            reject(()->NewsFeed.parseJson(json.replace(date,"2099-01-01T00:00:00Z")),"future rejection");
            String rss="<rss><channel><item><title>RSS title</title><link>https://example.com/a</link><pubDate>Thu, 26 Nov 2026 12:00:00 +0000</pubDate></item></channel></rss>";
            check(NewsFeed.parseRss(rss,"https://example.com").length()==1,"RSS");
            String atom="<feed xmlns=\"http://www.w3.org/2005/Atom\"><entry><title>Atom title</title><link href=\"https://example.com/b\"/><updated>"+date+"</updated></entry></feed>";
            check(NewsFeed.parseRss(atom,"https://example.com").length()==1,"Atom");
            reject(()->NewsFeed.parseRss("<!DOCTYPE rss [<!ENTITY a SYSTEM 'file:///etc/passwd'>]>"+rss,"https://example.com"),"XML entities rejection");
            check(!NewsFeed.validUrl("https://user:password@example.com"),"credentials rejection");
            check(!context.getSharedPreferences("news",0).getBoolean("enabled",false),"news opt-in default");
            context.getSharedPreferences("choices",0).edit().putString("upgrade_check","保留个人记录").commit();
            result.putString("result","PASS");result.putString("checks",""+passed);finish(-1,result);
        }catch(Throwable e){result.putString("result","FAIL: "+e);result.putString("checks",""+passed);finish(0,result);}
    }
}
