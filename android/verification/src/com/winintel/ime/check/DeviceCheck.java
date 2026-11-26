package com.winintel.ime.check;
import android.app.Instrumentation;
import android.os.*;
import com.winintel.ime.SpeechEngine;
import java.io.*;
import java.nio.*;

public final class DeviceCheck extends Instrumentation {
 @Override public void onCreate(Bundle args){super.onCreate(args);start();}
 @Override public void onStart(){
  Bundle result=new Bundle();
  try{
   long start=SystemClock.elapsedRealtime();
   SpeechEngine engine=new SpeechEngine(getTargetContext());
   result.putString("model_load_ms",""+(SystemClock.elapsedRealtime()-start));
   String[][] fixtures={{"0-zh.wav","地铁站"},{"0-en.wav","subway"},{"10-zh.wav","CPU"},{"12-zh.wav","1250"}};
   for(String[] item:fixtures){
    byte[] bytes;try(InputStream in=getContext().getAssets().open(item[0]);ByteArrayOutputStream out=new ByteArrayOutputStream()){byte[] block=new byte[4096];int n;while((n=in.read(block))!=-1)out.write(block,0,n);bytes=out.toByteArray();}
    ByteBuffer b=ByteBuffer.wrap(bytes).order(ByteOrder.LITTLE_ENDIAN);int pos=12,data=0,length=0,rate=0;
    while(pos+8<=bytes.length){int size=b.getInt(pos+4);String name=new String(bytes,pos,4,"US-ASCII");if(name.equals("fmt ")){if(b.getShort(pos+8)!=1||b.getShort(pos+10)!=1||b.getShort(pos+22)!=16)throw new Exception("Expected mono PCM16");rate=b.getInt(pos+12);}if(name.equals("data")){data=pos+8;length=size;break;}pos+=8+size+(size&1);}
    if(rate!=16000||data==0)throw new Exception("Expected 16kHz WAV");
    float[] audio=new float[length/2];for(int i=0;i<audio.length;i++)audio[i]=b.getShort(data+i*2)/32768f;
    start=SystemClock.elapsedRealtime();String text=engine.decode(audio);long elapsed=SystemClock.elapsedRealtime()-start;
    result.putString(item[0],text+" | "+elapsed+" ms");
    if(!text.toLowerCase().contains(item[1].toLowerCase()))throw new AssertionError(item[0]+": "+text);
   }
   engine.close();result.putString("result","PASS");finish(-1,result);
  }catch(Throwable e){result.putString("result","FAIL: "+e);finish(0,result);}
 }
}
