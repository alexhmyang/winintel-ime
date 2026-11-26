package com.winintel.ime;
import android.Manifest;
import android.app.*;
import android.content.*;
import android.content.pm.PackageManager;
import android.os.Bundle;
import android.provider.Settings;
import android.view.*;
import android.view.inputmethod.InputMethodManager;
import android.widget.*;
import java.io.*;

public final class MainActivity extends Activity {
    private EditText notes;
    private TextView status;
    private Button record;
    private VoiceClient voice;
    private boolean recording;
    @Override public void onCreate(Bundle state){
        super.onCreate(state);
        getWindow().getDecorView().setOnApplyWindowInsetsListener((v,insets)->{v.setPadding(insets.getSystemWindowInsetLeft(),insets.getSystemWindowInsetTop(),insets.getSystemWindowInsetRight(),insets.getSystemWindowInsetBottom());return insets;});
        ScrollView scroll=new ScrollView(this);LinearLayout root=Ui.column(this);root.setPadding(Ui.dp(this,20),Ui.dp(this,20),Ui.dp(this,20),Ui.dp(this,24));root.setBackgroundColor(Ui.BG);scroll.addView(root);setContentView(scroll);
        root.addView(Ui.text(this,"数萌 AI 输入法",30));root.addView(Ui.text(this,"winintel AI  /  Android 0.1.2",14));
        root.addView(Ui.text(this,"本地拼音 · 英文补全 · 离线语音\n新闻由你开启；输入和录音只在本机处理。",17));
        button(root,"实时新闻与双语热词",v->startActivity(new Intent(this,NewsActivity.class)));
        button(root,"1  启用数萌输入法",v->startActivity(new Intent(Settings.ACTION_INPUT_METHOD_SETTINGS)));
        button(root,"2  切换到数萌输入法",v->((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).showInputMethodPicker());
        button(root,"3  授权麦克风",v->{if(checkSelfPermission(Manifest.permission.RECORD_AUDIO)==PackageManager.PERMISSION_GRANTED)status.setText("麦克风已授权");else requestPermissions(new String[]{Manifest.permission.RECORD_AUDIO},7);});
        root.addView(Ui.text(this,"输入体验",21));EditText test=new EditText(this);test.setHint("点这里，试试 nihao / shuru / v100");test.setMinLines(2);test.setTextSize(18);root.addView(test);
        root.addView(Ui.text(this,"离线语音笔记",21));root.addView(Ui.text(this,"停顿后追加文字，可直接修改。首次识别需要加载模型；请先分句体验。",14));
        status=Ui.text(this,"准备就绪",14);root.addView(status);
        record=Ui.button(this,"开始录音",v->toggle());root.addView(record,new LinearLayout.LayoutParams(-1,Ui.dp(this,48)));
        notes=new EditText(this);notes.setGravity(Gravity.TOP);notes.setMinLines(5);notes.setHint("文字笔记仅手动保存到本机");notes.setTextSize(18);root.addView(notes);
        button(root,"保存文字到本机",v->{Intent i=new Intent(Intent.ACTION_CREATE_DOCUMENT);i.addCategory(Intent.CATEGORY_OPENABLE);i.setType("text/plain");i.putExtra(Intent.EXTRA_TITLE,"数萌语音笔记.txt");startActivityForResult(i,8);});
        button(root,"清空笔记",v->new AlertDialog.Builder(this).setMessage("清空当前文字笔记？").setNegativeButton("取消",null).setPositiveButton("清空",(d,w)->{voice.cancel();recording=false;record.setText("开始录音");notes.setText("");}).show());
        root.addView(Ui.text(this,"支持全拼、简拼、选词学习和语音草稿。EN 输入 hel 后点击 hello 补全，Space 保留原词。离开输入框前可按 Space 提交英文。新闻采用免费源和自有接口，默认关闭。翻译与 SRT 导出由 Windows 版提供。",14));
        button(root,"开源与模型说明",v->new AlertDialog.Builder(this).setTitle("数萌 AI / winintel AI").setMessage("应用代码 GPL-3.0-or-later；词库来自 rime-ice；语音运行库 sherpa-onnx（Apache-2.0），模型 SenseVoiceSmall INT8（FunASR 模型协议 1.1）。完整许可随源码和 APK 发布。\n音频只在内存中处理，密码框不学习选词、不启用语音。").setPositiveButton("知道了",null).show());
        voice=new VoiceClient(this,(type,text)->{if(type==VoiceService.TEXT){if(notes.length()>0)notes.append("\n");notes.append(text);}else{status.setText(text);if(type==VoiceService.ERROR){voice.stop();recording=false;record.setText("开始录音");}}});
        if(state!=null)notes.setText(state.getString("notes",""));
    }
    private void button(LinearLayout root,String text,View.OnClickListener listener){Button b=Ui.button(this,text,listener);LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(-1,Ui.dp(this,48));p.setMargins(0,Ui.dp(this,8),0,Ui.dp(this,8));root.addView(b,p);}
    private void toggle(){
        if(checkSelfPermission(Manifest.permission.RECORD_AUDIO)!=PackageManager.PERMISSION_GRANTED){requestPermissions(new String[]{Manifest.permission.RECORD_AUDIO},7);return;}
        if(recording){voice.stop();recording=false;record.setText("开始录音");}else{recording=true;record.setText("停止录音");voice.start();}
    }
    @Override public void onRequestPermissionsResult(int request,String[] permissions,int[] grants){super.onRequestPermissionsResult(request,permissions,grants);status.setText(grants.length>0 && grants[0]==0?"麦克风已授权，可开始离线语音":"麦克风未授权，拼音输入仍可使用");}
    @Override protected void onActivityResult(int request,int result,Intent data){
        super.onActivityResult(request,result,data);
        if(request==8 && result==RESULT_OK && data!=null){try(OutputStream out=getContentResolver().openOutputStream(data.getData())){out.write(notes.getText().toString().getBytes("UTF-8"));status.setText("文字已保存");}catch(Exception e){status.setText("保存失败："+e.getMessage());}}
    }
    @Override protected void onSaveInstanceState(Bundle state){state.putString("notes",notes.getText().toString());super.onSaveInstanceState(state);}
    @Override protected void onStop(){voice.cancel();recording=false;record.setText("开始录音");super.onStop();}
    @Override protected void onDestroy(){voice.close();super.onDestroy();}
}
