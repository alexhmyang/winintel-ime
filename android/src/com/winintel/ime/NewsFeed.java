// SPDX-License-Identifier: GPL-3.0-or-later
package com.winintel.ime;
import org.json.*;
import org.xmlpull.v1.*;
import android.util.Xml;
import java.io.*;
import java.net.*;
import java.time.*;
import java.time.format.*;
import java.util.*;

/** Downloads public headlines only; independent of input and audio. */
public final class NewsFeed {
    public static final String[] SOURCES={"https://www.ithome.com/rss/","https://feeds.npr.org/1004/rss.xml"};
    public static boolean validUrl(String value){
        try{URI uri=new URI(value);return "https".equalsIgnoreCase(uri.getScheme())&&uri.getHost()!=null&&uri.getUserInfo()==null;}
        catch(Exception e){return false;}
    }
    static String download(String address) throws Exception {
        for(int redirects=0;redirects<4;redirects++){
            if(!validUrl(address))throw new IOException("请使用 HTTPS 地址");
            HttpURLConnection connection=(HttpURLConnection)new URL(address).openConnection();
            connection.setConnectTimeout(10000);connection.setReadTimeout(10000);
            connection.setInstanceFollowRedirects(false);
            connection.setRequestProperty("User-Agent","winintel-ime-android-news/0.1.2");
            try{
                int code=connection.getResponseCode();
                if(code>=300&&code<400){address=new URL(new URL(address),connection.getHeaderField("Location")).toString();continue;}
                if(code!=200)throw new IOException("HTTP "+code);
                try(InputStream in=connection.getInputStream();ByteArrayOutputStream out=new ByteArrayOutputStream()){
                    byte[] buffer=new byte[8192];int count;
                    while((count=in.read(buffer))!=-1){if(out.size()+count>2097152)throw new IOException("新闻超过 2 MB");out.write(buffer,0,count);}
                    return out.toString("UTF-8");
                }
            }finally{connection.disconnect();}
        }
        throw new IOException("跳转过多");
    }
    private static long time(String value){
        try{return OffsetDateTime.parse(value.trim()).toInstant().toEpochMilli();}catch(Exception e){}
        try{return ZonedDateTime.parse(value.trim(),DateTimeFormatter.RFC_1123_DATE_TIME).toInstant().toEpochMilli();}catch(Exception e){}
        try{java.text.SimpleDateFormat f=new java.text.SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss Z",Locale.US);f.setLenient(false);return f.parse(value.trim()).getTime();}catch(Exception e){return 0;}
    }
    private static JSONObject item(String title,String url,String source,String date) throws Exception {
        long published=time(date);
        if(title==null||title.trim().isEmpty()||title.length()>300||!validUrl(url)||!validUrl(source)||published<=0||published>System.currentTimeMillis()+600000)throw new IOException("新闻条目格式错误");
        return new JSONObject().put("title",title.trim()).put("url",url).put("source",source).put("published_at",Instant.ofEpochMilli(published).toString());
    }
    public static JSONArray parseJson(String text) throws Exception {
        JSONObject pack=new JSONObject(text);
        if(pack.getInt("schema")!=1)throw new IOException("新闻 schema 错误");
        JSONArray input=pack.getJSONArray("items"),output=new JSONArray();
        if(input.length()>200)throw new IOException("最多 200 条新闻");
        for(int i=0;i<input.length();i++){JSONObject row=input.getJSONObject(i);output.put(item(row.getString("title"),row.getString("url"),row.getString("source"),row.getString("published_at")));}
        return sorted(output);
    }
    public static JSONArray parseRss(String text,String source) throws Exception {
        if(text.toUpperCase(Locale.ROOT).contains("<!DOCTYPE")||text.toUpperCase(Locale.ROOT).contains("<!ENTITY"))throw new IOException("不支持 XML 外部实体");
        XmlPullParser parser=Xml.newPullParser();parser.setFeature(XmlPullParser.FEATURE_PROCESS_NAMESPACES,true);
        parser.setInput(new StringReader(text));JSONArray rows=new JSONArray();
        String title="",url="",date="";boolean active=false;
        for(int event=parser.getEventType();event!=XmlPullParser.END_DOCUMENT;event=parser.next()){
            String name=parser.getName();
            if(event==XmlPullParser.START_TAG){
                if("item".equals(name)||"entry".equals(name)){active=true;title="";url="";date="";}
                else if(active&&"title".equals(name))title=parser.nextText();
                else if(active&&"link".equals(name)){
                    String href=parser.getAttributeValue(null,"href"),rel=parser.getAttributeValue(null,"rel");
                    if(href!=null){if(rel==null||"alternate".equals(rel))url=href;}
                    else url=parser.nextText().trim();
                }
                else if(active&&("pubDate".equals(name)||"published".equals(name)||"updated".equals(name)))date=parser.nextText();
            }
            if(event==XmlPullParser.END_TAG&&active&&("item".equals(name)||"entry".equals(name))){
                try{rows.put(item(title,url,source,date));}catch(Exception invalid){}
                active=false;if(rows.length()==200)break;
            }
        }
        if(rows.length()==0)throw new IOException("新闻源没有有效条目");
        return sorted(rows);
    }
    static JSONArray sorted(JSONArray rows) throws Exception {
        Map<String,JSONObject> unique=new LinkedHashMap<>();
        for(int i=0;i<rows.length();i++){JSONObject row=rows.getJSONObject(i);unique.put(row.getString("url"),row);}
        List<JSONObject> list=new ArrayList<>(unique.values());
        list.sort((a,b)->Long.compare(time(b.optString("published_at")),time(a.optString("published_at"))));
        JSONArray result=new JSONArray();for(JSONObject row:list){result.put(row);if(result.length()==200)break;}return result;
    }
    static JSONArray freeSources(StringBuilder status) throws Exception {
        JSONArray result=new JSONArray();int success=0;
        for(String source:SOURCES)try{
            JSONArray rows=parseRss(download(source),source);success++;
            for(int i=0;i<rows.length();i++)result.put(rows.getJSONObject(i));
        }catch(Exception e){status.append(" · ").append(new URI(source).getHost()).append("暂不可用");}
        if(success==0)throw new IOException("免费源暂不可用，保留缓存");
        return sorted(result);
    }
}
