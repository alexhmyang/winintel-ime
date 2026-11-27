package com.winintel.ime.check;
import android.app.Instrumentation;
import android.content.*;
import android.os.*;
import com.winintel.ime.VoiceService;
import java.util.concurrent.*;

public final class VoiceSwitchCheck extends Instrumentation {
    private final CountDownLatch ready=new CountDownLatch(1);
    private volatile String error="";
    private volatile Messenger service;
    private Messenger reply;
    private void send(int type,int session)throws RemoteException{
        Message m=Message.obtain(null,type,session,0);m.replyTo=reply;service.send(m);
    }
    @Override public void onCreate(Bundle args){super.onCreate(args);start();}
    @Override public void onStart(){
        Context context=getTargetContext();
        reply=new Messenger(new Handler(Looper.getMainLooper(),m->{
            if(m.arg1==502){
                if(m.what==VoiceService.ERROR){error=m.getData().getString("text","error");ready.countDown();}
                if(m.what==VoiceService.STATUS&&m.getData().getString("text","").startsWith("正在听"))ready.countDown();
            }
            return true;
        }));
        ServiceConnection connection=new ServiceConnection(){
            public void onServiceConnected(ComponentName name,IBinder binder){
                service=new Messenger(binder);
                try{send(VoiceService.START,501);send(VoiceService.STOP,501);send(VoiceService.START,502);}
                catch(Exception e){error=e.toString();ready.countDown();}
            }
            public void onServiceDisconnected(ComponentName name){error="service disconnected";ready.countDown();}
        };
        boolean bound=false;
        try{
            bound=context.bindService(new Intent(context,VoiceService.class),connection,Context.BIND_AUTO_CREATE);
            if(!bound||!ready.await(8,TimeUnit.SECONDS))error="new microphone session did not start";
        }catch(Exception e){error=e.toString();}
        finally{try{if(service!=null)send(VoiceService.STOP,502);}catch(Exception ignored){}if(bound)context.unbindService(connection);}
        Bundle result=new Bundle();result.putString("result",error.isEmpty()?"PASS":"FAIL: "+error);finish(error.isEmpty()?-1:0,result);
    }
}
