package com.winintel.ime;
import android.Manifest;
import android.content.*;
import android.content.pm.PackageManager;
import android.inputmethodservice.InputMethodService;
import android.os.*;
import android.text.InputType;
import android.view.*;
import android.view.inputmethod.*;
import android.widget.*;
import java.util.*;
import java.util.concurrent.*;

public final class ImeService extends InputMethodService {
    private final ExecutorService lookup=Executors.newSingleThreadExecutor();
    private final Handler ui=new Handler(Looper.getMainLooper());
    private Dictionary dictionary;
    private VoiceClient voice;
    private LinearLayout root,candidates,keys;
    private TextView composition,status;
    private EditText draft;
    private Button micButton;
    private String code="";
    private boolean english,numbers,password,editing,recording,shift;
    private volatile int generation;
    private int page;
    private List<String> results=new ArrayList<>();
    @Override public void onCreate(){
        super.onCreate();voice=new VoiceClient(this,this::voiceEvent);
        lookup.execute(()->{try{dictionary=new Dictionary(Assets.copy(this,"dictionary.bin"));ui.post(()->{if(status!=null)status.setText("离线词库已就绪");query();});}catch(Exception e){ui.post(()->{if(status!=null)status.setText("词库加载失败："+e.getMessage());});}});
    }
    @Override public View onCreateInputView(){
        root=Ui.column(this);root.setBackgroundColor(Ui.BG);root.setPadding(Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,8));
        root.setOnApplyWindowInsetsListener((v,insets)->{v.setPadding(Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,8)+Math.max(Ui.dp(this,24),insets.getSystemWindowInsetBottom()));return insets;});
        LinearLayout tools=new LinearLayout(this);
        add(tools,Ui.button(this,"中 / EN",v->{flush();english=!english;editing=false;renderKeys();}),1,38);
        micButton=Ui.button(this,"语音",v->toggleVoice());add(tools,micButton,1,38);
        add(tools,Ui.button(this,"切换",v->{flush();((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).showInputMethodPicker();}),1,38);
        add(tools,Ui.button(this,"收起",v->requestHideSelf(0)),1,38);root.addView(tools);
        composition=Ui.text(this,"",15);composition.setVisibility(View.GONE);root.addView(composition);
        HorizontalScrollView scroll=new HorizontalScrollView(this);scroll.setHorizontalScrollBarEnabled(false);
        candidates=new LinearLayout(this);scroll.setTag("candidates-scroll");scroll.setVisibility(View.GONE);scroll.addView(candidates);root.addView(scroll,new LinearLayout.LayoutParams(-1,Ui.dp(this,44)));
        status=Ui.text(this,"数萌 AI · 全离线",12);root.addView(status);
        draft=new EditText(this);draft.setTextSize(17);draft.setTextColor(Ui.INK);draft.setHint("语音草稿");draft.setSingleLine(false);draft.setMaxLines(3);draft.setShowSoftInputOnFocus(false);draft.setVisibility(View.GONE);root.addView(draft);
        LinearLayout actions=new LinearLayout(this);actions.setTag("draft-actions");actions.setVisibility(View.GONE);
        add(actions,Ui.button(this,"编辑草稿",v->{flush();editing=!editing;status.setText(editing?"键盘正在编辑语音草稿":"键盘正在输入应用");}),1,36);
        add(actions,Ui.button(this,"清除",v->{voice.cancel();recording=false;micButton.setText("语音");draft.setText("");}),1,36);
        add(actions,Ui.button(this,"确认上屏",v->{voice.cancel();recording=false;micButton.setText("语音");String s=draft.getText().toString();InputConnection ic=getCurrentInputConnection();if(ic!=null&&!s.isEmpty()){ic.commitText(s,1);draft.setText("");editing=false;} }),1,36);root.addView(actions);
        keys=Ui.column(this);root.addView(keys);renderKeys();return root;
    }
    private void add(LinearLayout row,View view,float weight,int height){
        LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(0,Ui.dp(this,height),weight);p.setMargins(Ui.dp(this,2),Ui.dp(this,3),Ui.dp(this,2),Ui.dp(this,3));row.addView(view,p);
    }
    private void renderKeys(){
        if(keys==null)return;keys.removeAllViews();
        String[] rows=numbers?new String[]{"1234567890","@#￥%&*+-()","，。？！：；/\"'"}:new String[]{"qwertyuiop","asdfghjkl","zxcvbnm"};
        for(String letters:rows){LinearLayout row=new LinearLayout(this);if(letters.equals("zxcvbnm"))add(row,Ui.button(this,shift?"⇧":"↑",v->{shift=!shift;renderKeys();}),1,46);for(char ch:letters.toCharArray()){String s=shift && (english||password)?String.valueOf(ch).toUpperCase(Locale.ROOT):String.valueOf(ch);add(row,Ui.button(this,s,v->letter(s)),1,46);}keys.addView(row);}
        LinearLayout bottom=new LinearLayout(this);
        add(bottom,Ui.button(this,numbers?"ABC":"123",v->{flush();numbers=!numbers;renderKeys();}),1.3f,46);
        Button punctuation=Ui.button(this,english?".":"，",v->{flush();commit(english?".":"，");});punctuation.setOnLongClickListener(v->{flush();commit(english?",":"。");return true;});add(bottom,punctuation,0.8f,46);
        add(bottom,Ui.button(this,english?"Space":"空格",v->{if(!code.isEmpty())choose(results.isEmpty()?code:results.get(0));else commit(" ");}),2.8f,46);
        Button back=Ui.button(this,"⌫",v->backspace());back.setOnLongClickListener(v->{code="";generation++;query();return true;});add(bottom,back,1.2f,46);
        add(bottom,Ui.button(this,"↵",v->{if(!code.isEmpty())flush();else if(editing)commit("\n");else{InputConnection ic=getCurrentInputConnection();EditorInfo e=getCurrentInputEditorInfo();int action=e==null?0:e.imeOptions&EditorInfo.IME_MASK_ACTION;if(ic!=null){if(action!=EditorInfo.IME_ACTION_NONE && action!=EditorInfo.IME_ACTION_UNSPECIFIED)ic.performEditorAction(action);else ic.commitText("\n",1);}}}),1.2f,46);keys.addView(bottom);
    }
    private void letter(String s){
        if(english || numbers || password){commit(s);return;}
        if(code.length()<48){code+=s;page=0;query();}
    }
    private void commit(String s){
        if(editing){int pos=Math.max(0,draft.getSelectionStart());draft.getText().insert(pos,s);}
        else{InputConnection ic=getCurrentInputConnection();if(ic!=null)ic.commitText(s,1);}
    }
    private void backspace(){
        if(!code.isEmpty()){code=code.substring(0,code.length()-1);query();}
        else if(editing){int p=draft.getSelectionStart();if(p>0)draft.getText().delete(draft.getText().toString().offsetByCodePoints(p,-1),p);}
        else{InputConnection ic=getCurrentInputConnection();if(ic!=null)ic.deleteSurroundingTextInCodePoints(1,0);}
    }
    private void flush(){if(!code.isEmpty())choose(code);}
    private void choose(String text){
        if(!password&&!code.isEmpty()&&!text.equals(code)){
            android.content.SharedPreferences prefs=getSharedPreferences("choices",0);
            if(prefs.getAll().size()>10000)prefs.edit().clear().apply();
            prefs.edit().putString(code,text).apply();
        }
        commit(text);code="";page=0;query();
    }
    private void query(){
        if(composition==null)return;
        composition.setVisibility(code.isEmpty()?View.GONE:View.VISIBLE);root.findViewWithTag("candidates-scroll").setVisibility(code.isEmpty()?View.GONE:View.VISIBLE);
        composition.setText(code);final String request=code;final int id=++generation;results=new ArrayList<>();candidates.removeAllViews();
        if(request.isEmpty()||dictionary==null)return;
        lookup.execute(()->{if(id!=generation)return;List<String> list=dictionary.query(request);String prior=password?null:getSharedPreferences("choices",0).getString(request,null);if(prior!=null){list.remove(prior);list.add(0,prior);}ui.post(()->{if(id!=generation || candidates==null)return;results=list;page=0;showCandidates();});});
    }
    private void showCandidates(){
        candidates.removeAllViews();int start=page*9;
        for(int i=start;i<Math.min(results.size(),start+9);i++){String text=results.get(i);Button b=Ui.button(this,text,v->choose(text));b.setPadding(Ui.dp(this,12),0,Ui.dp(this,12),0);candidates.addView(b,new LinearLayout.LayoutParams(-2,-1));}
        if(page>0)candidates.addView(Ui.button(this,"‹",v->{page--;showCandidates();}));
        if(start+9<results.size())candidates.addView(Ui.button(this,"›",v->{page++;showCandidates();}));
    }
    private void toggleVoice(){
        if(password){status.setText("密码输入框不启用语音");return;}
        if(checkSelfPermission(Manifest.permission.RECORD_AUDIO)!=PackageManager.PERMISSION_GRANTED){startActivity(new Intent(this,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));status.setText("请在数萌首页授权麦克风");return;}
        if(recording){voice.stop();recording=false;micButton.setText("语音");}
        else{draft.setVisibility(View.VISIBLE);root.findViewWithTag("draft-actions").setVisibility(View.VISIBLE);editing=false;recording=true;micButton.setText("停止");voice.start();}
    }
    private void voiceEvent(int type,String text){
        if(status==null)return;
        if(type==VoiceService.TEXT){String command=text.replaceAll("[\\s，。！？,.!?]","");if(command.equals("清除")||command.equals("清空"))draft.setText("");else if(command.equals("撤销")){String old=draft.getText().toString();int p=old.lastIndexOf('\n');draft.setText(p<0?"":old.substring(0,p));}else{if(draft.length()>0)draft.append("\n");draft.append(text);}}
        else{status.setText(text);if(type==VoiceService.ERROR){voice.stop();recording=false;micButton.setText("语音");}}
    }
    @Override public void onStartInput(EditorInfo info,boolean restarting){
        super.onStartInput(info,restarting);voice.cancel();recording=false;code="";generation++;editing=false;
        int type=info.inputType,variation=type&InputType.TYPE_MASK_VARIATION;
        password=((type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_TEXT && (variation==InputType.TYPE_TEXT_VARIATION_PASSWORD || variation==InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD || variation==InputType.TYPE_TEXT_VARIATION_WEB_PASSWORD)) || ((type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_NUMBER && variation==InputType.TYPE_NUMBER_VARIATION_PASSWORD);
        numbers=(type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_NUMBER || (type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_PHONE;
        if(draft!=null){draft.setText("");draft.setVisibility(View.GONE);root.findViewWithTag("draft-actions").setVisibility(View.GONE);micButton.setText("语音");}
        query();renderKeys();
    }
    @Override public void onFinishInputView(boolean finishingInput){voice.cancel();recording=false;editing=false;super.onFinishInputView(finishingInput);}
    @Override public void onFinishInput(){voice.cancel();code="";generation++;super.onFinishInput();}
    @Override public boolean onEvaluateFullscreenMode(){return false;}
    @Override public void onDestroy(){voice.close();lookup.shutdownNow();super.onDestroy();}
}
