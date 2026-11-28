// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
package com.winintel.ime.check;
import android.app.Instrumentation;
import android.os.Bundle;
import android.content.Context;
import java.io.*;
import java.util.*;
import com.winintel.ime.Dictionary;
import com.winintel.ime.IndustryLexicon;
import com.winintel.ime.PersonalLexicon;
public final class IndustryCheck extends Instrumentation {
 private int checks;
 private void check(boolean ok,String label){if(!ok)throw new AssertionError(label);checks++;}
 @Override public void onCreate(Bundle args){super.onCreate(args);start();}
 @Override public void onStart(){
  Bundle result=new Bundle();Context context=getTargetContext();
  String previous=context.getSharedPreferences("industry",0).getString("profile","general");
  try{
   check(context.getPackageManager().getPackageInfo("com.winintel.ime",0).versionName.equals("0.1.10"),"deployed version");
   File root=new File(context.getFilesDir(),"lexicon-019");
   for(String n:new String[]{"dictionary.bin","character-readings.tsv","name-parts.tsv"})check(new File(root,n).isFile(),"deployed "+n);
   Dictionary d=new Dictionary(new File(root,"dictionary.bin"));
   IndustryLexicon industry=new IndustryLexicon(context.getAssets().open("industry-words.tsv"));
   for(String profile:new String[]{"general","programming","ai","medical","finance","law","education","manufacturing","ecommerce"}){
    check(context.getSharedPreferences("industry",0).edit().putString("profile",profile).commit(),"save profile");
    check(context.getSharedPreferences("industry",0).getString("profile","").equals(profile),"read profile");
   }
   check(industry.rank("programming","std::v",Collections.emptyList(),true,false).contains("std::vector"),"scope completion");
   check(industry.rank("programming","json.p",Collections.emptyList(),true,false).contains("JSON.parse"),"canonical dot completion");
   check(industry.rank("ai","pytorch",Collections.emptyList(),true,false).get(0).equals("PyTorch"),"canonical case");
   String code="banbenkongzhi";check(industry.rank("programming",code,d.query(code),false,false).contains("版本控制"),"Chinese industry word");
   Class<?> nineClass=Class.forName("com.winintel.ime.NineKeyDictionary");
   java.lang.reflect.Constructor<?> ctor=nineClass.getDeclaredConstructor(File.class);ctor.setAccessible(true);
   Object nine=ctor.newInstance(new File(root,"dictionary.bin"));
   java.lang.reflect.Method toDigits=nineClass.getDeclaredMethod("digits",String.class);toDigits.setAccessible(true);
   java.lang.reflect.Method query=nineClass.getDeclaredMethod("query",String.class);query.setAccessible(true);
   String digits=(String)toDigits.invoke(null,code);
   check(industry.rank("programming",digits,(List<String>)query.invoke(nine,digits),false,true).contains("版本控制"),"industry T9");
   File user=new File(context.getCacheDir(),"industry019-learning.tsv");
   PersonalLexicon p=new PersonalLexicon(user,context.getAssets().open("character-readings.tsv"),new File(root,"name-parts.tsv"));p.learn(code,"个人用词");
   p=new PersonalLexicon(user,context.getAssets().open("character-readings.tsv"),new File(root,"name-parts.tsv"));
   for(String profile:new String[]{"general","programming","medical"})check(p.rank(code,industry.rank(profile,code,d.query(code),false,false)).get(0).equals("个人用词"),"personal priority");
   check(industry.rank("general","yhm",d.query("yhm"),false,false).equals(d.query("yhm")),"general regression");
   context.getSharedPreferences("industry",0).edit().putString("profile",previous).commit();
   result.putString("result","PASS");result.putInt("checks",checks);finish(-1,result);
  }catch(Throwable e){context.getSharedPreferences("industry",0).edit().putString("profile",previous).commit();result.putString("result","FAIL: "+e);result.putInt("checks",checks);finish(0,result);}
 }
}
