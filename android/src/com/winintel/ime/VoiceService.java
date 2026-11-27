// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
package com.winintel.ime;
import android.app.Service;
import android.content.Intent;
import android.media.*;
import android.os.*;
import com.k2fsa.sherpa.onnx.*;
import java.util.*;
import java.util.concurrent.*;

/** Mic capture and CPU inference live in :voice, never in the typing process. */
public final class VoiceService extends Service {
    public static final int START=1, STOP=2, TEXT=3, STATUS=4, ERROR=5;
    private final ExecutorService decoder=Executors.newSingleThreadExecutor();
    private SpeechEngine recognizer;
    private volatile boolean recording;
    private volatile AudioRecord mic;
    private Thread capture;
    private Messenger client;
    private volatile int session;
    private final Handler main=new Handler(Looper.getMainLooper());
    private Messenger nextClient;
    private int nextSession;
    private final Messenger inbox=new Messenger(new Handler(Looper.getMainLooper(),m->{
        if(m.what==START){if(recording || capture!=null){nextClient=m.replyTo;nextSession=m.arg1;recording=false;AudioRecord r=mic;if(r!=null)try{r.stop();}catch(Exception ignored){}return true;}client=m.replyTo;session=m.arg1;start();}
        if(m.what==STOP && m.arg1==nextSession)nextClient=null;
        if(m.what==STOP && m.arg1==session){recording=false;AudioRecord r=mic;if(r!=null)try{r.stop();}catch(Exception ignored){}}
        return true;
    }));
    @Override public IBinder onBind(Intent intent){return inbox.getBinder();}
    private static void reply(Messenger to,int type,int session,String text){
        if(to==null)return;
        Message m=Message.obtain(null,type,session,0);Bundle b=new Bundle();b.putString("text",text);m.setData(b);
        try{to.send(m);}catch(RemoteException ignored){}
    }
    private void prepare() throws Exception {
        if(recognizer!=null)return;
        recognizer=new SpeechEngine(this);
    }
    private void start(){
        final Messenger to=client;final int id=session;
        recording=true;
        capture=new Thread(()->{
            AudioRecord r=null;
            try {
                int min=AudioRecord.getMinBufferSize(16000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT);
                r=new AudioRecord(MediaRecorder.AudioSource.VOICE_RECOGNITION,16000,AudioFormat.CHANNEL_IN_MONO,AudioFormat.ENCODING_PCM_16BIT,Math.max(min,8192));
                if(r.getState()!=AudioRecord.STATE_INITIALIZED)throw new IllegalStateException("麦克风初始化失败");
                mic=r;r.startRecording();reply(to,STATUS,id,"正在听…停顿后出字，再次点击停止");
                float[] samples=new float[320000],preroll=new float[3200];int used=0,silent=0,preUsed=0;boolean voiced=false;short[] buf=new short[1600];double noise=60;
                while(recording){
                    int n=r.read(buf,0,buf.length);if(n<0){if(recording)throw new IllegalStateException("麦克风读取失败 "+n);break;}
                    double energy=0;for(int i=0;i<n;i++)energy+=(double)buf[i]*buf[i];
                    double rms=n>0?Math.sqrt(energy/n):0;
                    boolean loud=n>0 && rms>Math.max(180,noise*2.8);
                    if(!voiced&&!loud)noise=noise*.95+Math.min(rms,300)*.05;
                    if(loud&&!voiced){System.arraycopy(preroll,0,samples,0,preUsed);used=preUsed;preUsed=0;}
                    if(loud){voiced=true;silent=0;}else silent+=n;
                    if(voiced || loud){
                        for(int i=0;i<n;i++){
                            samples[used++]=buf[i]/32768f;
                            if(used==samples.length){submit(samples,used,to,id,false);used=0;samples=new float[320000];}
                        }
                    }else if(n>0){int keep=Math.min(preUsed,preroll.length-n);if(keep>0)System.arraycopy(preroll,preUsed-keep,preroll,0,keep);for(int i=0;i<n;i++)preroll[keep+i]=buf[i]/32768f;preUsed=keep+n;}
                    if(voiced && silent>=19200){if(used>=3200)submit(samples,used,to,id,true);used=0;voiced=false;preUsed=0;samples=new float[320000];}
                }
                if(voiced && used>=3200)submit(samples,used,to,id,true);
                decoder.execute(()->reply(to,STATUS,id,"已停止，草稿可编辑后确认"));
            } catch(Throwable e){reply(to,ERROR,id,"录音失败："+e.getMessage());}
            finally{recording=false;mic=null;if(r!=null){try{r.stop();}catch(Exception ignored){}r.release();}main.post(()->{capture=null;if(nextClient!=null){client=nextClient;session=nextSession;nextClient=null;start();}});}
        },"winintel-microphone");
        capture.start();
    }
    private final java.util.concurrent.atomic.AtomicInteger pending=new java.util.concurrent.atomic.AtomicInteger();
    private void submit(float[] samples,int used,Messenger to,int id,boolean sentenceEnd){
        if(pending.incrementAndGet()>3){pending.decrementAndGet();reply(to,ERROR,id,"语音处理较慢，请暂停后分句录音");if(id==session)recording=false;return;}
        float[] audio=Arrays.copyOf(samples,used);
        decoder.execute(()->{
            try{
                if(id!=session)return;
                reply(to,STATUS,id,"正在离线识别…");prepare();
                String text=SpeechPunctuation.apply(recognizer.decode(audio),sentenceEnd);
                if(!text.isEmpty())reply(to,TEXT,id,text);
            }catch(Throwable e){reply(to,ERROR,id,"离线识别失败："+e.getMessage());}
            finally{Arrays.fill(audio,0);pending.decrementAndGet();}
        });
    }
    @Override public void onDestroy(){
        nextClient=null;recording=false;AudioRecord r=mic;if(r!=null)try{r.stop();}catch(Exception ignored){}
        decoder.execute(()->{if(recognizer!=null){recognizer.close();recognizer=null;}});
        decoder.shutdown();super.onDestroy();
    }
}
