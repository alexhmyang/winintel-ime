package com.winintel.ime;
import android.content.*;
import android.os.*;
final class VoiceClient {
    interface Listener{void event(int type,String text);}
    private final Context context;
    private final Listener listener;
    private Messenger service;
    private int session=1;
    private boolean bound,wanted;
    private final Messenger replies;
    VoiceClient(Context c,Listener l){context=c;listener=l;replies=new Messenger(new Handler(Looper.getMainLooper(),m->{
        if(m.arg1==session)listener.event(m.what,m.getData().getString("text",""));return true;
    }));}
    private final ServiceConnection connection=new ServiceConnection(){
        public void onServiceConnected(ComponentName n,IBinder b){service=new Messenger(b);if(wanted)send(VoiceService.START);}
        public void onServiceDisconnected(ComponentName n){service=null;wanted=false;listener.event(VoiceService.ERROR,"语音进程已退出，请重新录音");}
    };
    void start(){session++;wanted=true;if(service!=null){send(VoiceService.START);return;}if(!bound)bound=context.bindService(new Intent(context,VoiceService.class),connection,Context.BIND_AUTO_CREATE);}
    void stop(){wanted=false;send(VoiceService.STOP);}
    void cancel(){stop();session++;}
    private void send(int type){if(service==null)return;Message m=Message.obtain(null,type,session,0);m.replyTo=replies;try{service.send(m);}catch(RemoteException e){listener.event(VoiceService.ERROR,"语音服务连接中断");}}
    void close(){cancel();if(bound){context.unbindService(connection);bound=false;}service=null;}
}
