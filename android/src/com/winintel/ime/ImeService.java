// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
// Project: winintel AI Input Method - https://github.com/alexhmyang/winintel-ime
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
    private EnglishDictionary englishDictionary;
    private NineKeyDictionary nineDictionary;
    private PersonalLexicon personal;
    private String editorPackage;
    private int editorField;
    private int editorType;
    private VoiceClient voice;
    private LinearLayout root,candidates,keys;
    private TextView composition,status;
    private EditText draft;
    private Button micButton,languageButton;
    private LinearLayout editorTools;
    private TextView cursorPreview;
    private boolean selecting;
    private int selectionAnchor=-1;
    private Magnifier magnifier;
    private String code="";
    private boolean english,numbers,password,editing,recording,shift;
    private boolean nine;
    private boolean symbols;
    private String symbolCategory="常用";
    private final List<String> voiceHistory=new ArrayList<>();
    private volatile int generation;
    private int page;
    private List<String> results=new ArrayList<>();
    @Override public void onCreate(){
        super.onCreate();voice=new VoiceClient(this,this::voiceEvent);nine=getSharedPreferences("layout",0).getBoolean("nine",true);
        lookup.execute(()->{try{dictionary=new Dictionary(Assets.copy(this,"dictionary-015.bin","dictionary.bin"));dictionary.loadHotwords(getAssets().open("hot-chinese.tsv"));englishDictionary=new EnglishDictionary(getAssets().open("english-words.tsv"));personal=new PersonalLexicon(new java.io.File(getFilesDir(),"personal-words.tsv"),getAssets().open("character-readings.tsv"));ui.post(()->{if(status!=null)status.setText("离线中英文词库已就绪");query();});}catch(Exception e){ui.post(()->{if(status!=null)status.setText("词库加载失败："+e.getMessage());});}});
    }
    @Override public View onCreateInputView(){
        root=Ui.column(this);root.setBackgroundColor(Ui.BG);root.setPadding(Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,8));
        root.setOnApplyWindowInsetsListener((v,insets)->{v.setPadding(Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,8)+Math.max(Ui.dp(this,24),insets.getSystemWindowInsetBottom()));return insets;});
        LinearLayout tools=new LinearLayout(this);
        languageButton=Ui.button(this,"中 / EN",v->{flush();english=!english;renderKeys();});add(tools,languageButton,1,38);
        micButton=Ui.button(this,"语音",v->toggleVoice());add(tools,micButton,1,38);
        add(tools,Ui.button(this,"布局",v->showLayouts()),1,38);
        add(tools,Ui.button(this,"工具",v->{PopupMenu menu=new PopupMenu(this,v);menu.getMenu().add("切换输入法");menu.getMenu().add("每日新闻");menu.getMenu().add("收起键盘");menu.setOnMenuItemClickListener(item->{flush();if(item.getTitle().equals("切换输入法"))((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).showInputMethodPicker();else if(item.getTitle().equals("收起键盘"))requestHideSelf(0);else startActivity(new Intent(this,NewsActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));return true;});menu.show();}),1,38);
        composition=Ui.text(this,"",18);composition.setVisibility(View.GONE);root.addView(composition);
        HorizontalScrollView scroll=new HorizontalScrollView(this);scroll.setHorizontalScrollBarEnabled(false);
        candidates=new LinearLayout(this);candidates.setBackgroundColor(Ui.GREEN);scroll.setTag("candidates-scroll");scroll.setVisibility(View.GONE);scroll.addView(candidates);root.addView(scroll,new LinearLayout.LayoutParams(-1,Ui.dp(this,52)));
        status=Ui.text(this,"数萌 AI · 九宫格 / 全键盘 · 本机处理",12);root.addView(status);
        draft=new EditText(this);draft.setTextSize(17);draft.setTextColor(Ui.INK);draft.setHint("语音草稿");draft.setSingleLine(false);draft.setMaxLines(3);draft.setShowSoftInputOnFocus(false);draft.setVisibility(View.GONE);root.addView(draft);
        draft.setOnTouchListener((v,e)->{if(e.getAction()==MotionEvent.ACTION_DOWN){flush();editing=true;if(recording){voice.cancel();recording=false;micButton.setText("语音");micButton.setSelected(false);}}return false;});
        LinearLayout actions=new LinearLayout(this);actions.setTag("draft-actions");actions.setVisibility(View.GONE);
        add(actions,Ui.button(this,"清除",v->{voice.cancel();recording=false;micButton.setText("语音");micButton.setSelected(false);draft.setText("");}),1,36);
        add(actions,Ui.button(this,"确认上屏",v->{flush();voice.cancel();recording=false;micButton.setText("语音");micButton.setSelected(false);String s=draft.getText().toString();InputConnection ic=getCurrentInputConnection();if(ic!=null&&!s.isEmpty()){ic.finishComposingText();ic.commitText(s,1);learnText(s);draft.setText("");editing=false;draft.clearFocus();} }),1,36);root.addView(actions);
        editorTools=Ui.column(this);
        cursorPreview=Ui.text(this,"",19);cursorPreview.setSingleLine(true);cursorPreview.setVisibility(View.GONE);editorTools.addView(cursorPreview);
        LinearLayout navigation=new LinearLayout(this);
        add(navigation,function("←",v->moveCursor(-1)),1,44);
        add(navigation,function("选择",v->{selecting=!selecting;v.setSelected(selecting);selectionAnchor=-1;status.setVisibility(View.VISIBLE);status.setText(selecting?"用左右键扩展选区":"用左右键移动光标");}),1,44);
        add(navigation,function("→",v->moveCursor(1)),1,44);
        for(String label:new String[]{"全选","复制","剪切","粘贴"})add(navigation,function(label,v->editAction(label)),1,44);
        editorTools.addView(navigation);root.addView(editorTools);
        keys=Ui.column(this);root.addView(keys);root.addView(tools);renderKeys();return root;
    }
    private void add(LinearLayout row,View view,float weight,int height){
        row.setBaselineAligned(false);
        LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(0,Ui.dp(this,height),weight);p.setMargins(Ui.dp(this,2),Ui.dp(this,3),Ui.dp(this,2),Ui.dp(this,3));row.addView(view,p);
    }
    private void renderKeys(){
        if(keys==null)return;keys.removeAllViews();if(languageButton!=null)languageButton.setSelected(english);if(micButton!=null)micButton.setSelected(recording);
        if(symbols)renderSymbols();
        else if(numbers)renderNumbers();
        else if(nine&&!english&&!password)renderNine();
        else{
            String[] rows={"qwertyuiop","asdfghjkl","zxcvbnm"};
            for(int index=0;index<rows.length;index++){
                LinearLayout row=new LinearLayout(this);
                if(index==1)add(row,new View(this),0.5f,keyHeight());
                if(index==2)add(row,key(shift?"⇧":"↑",v->{shift=!shift;renderKeys();}),1.5f,keyHeight());
                for(char ch:rows[index].toCharArray()){String s=shift&&(english||password)?String.valueOf(ch).toUpperCase(Locale.ROOT):String.valueOf(ch);add(row,key(s,v->letter(s)),1,keyHeight());}
                if(index==1)add(row,new View(this),0.5f,keyHeight());
                if(index==0)add(row,deleteKey(),1.3f,keyHeight());
                if(index==2)add(row,function("→",v->moveCursor(1)),1.5f,keyHeight());
                keys.addView(row);
            }
        }
        LinearLayout bottom=new LinearLayout(this);
        add(bottom,function(numbers||symbols?"返回":"符号",v->{flush();if(numbers||symbols){numbers=false;symbols=false;}else symbols=true;renderKeys();}),1.2f,keyHeight());
        add(bottom,function("123",v->{flush();symbols=false;numbers=!numbers;renderKeys();}),1,keyHeight());
        Button punctuation=key(english?".":"，",v->{commitPunctuation(english?".":"，");});punctuation.setOnLongClickListener(v->{commitPunctuation(english?",":"。");return true;});add(bottom,punctuation,0.8f,keyHeight());
        Button space=key(english?"Space":(nine?"九宫格 · 空格":"全键盘 · 空格"),v->{if(english){flush();commit(" ");}else if(!code.isEmpty())choose(results.isEmpty()?code:results.get(0));else commit(" ");});
        space.setOnLongClickListener(v->{showEditor();return true;});
        space.setOnTouchListener(new View.OnTouchListener(){
            float lastX,startX;boolean dragging;
            public boolean onTouch(View v,MotionEvent e){
                if(e.getAction()==MotionEvent.ACTION_DOWN){lastX=startX=e.getX();dragging=false;}
                else if(e.getAction()==MotionEvent.ACTION_MOVE){
                    float step=Ui.dp(ImeService.this,14);
                    if(!dragging&&Math.abs(e.getX()-startX)>Ui.dp(ImeService.this,24)){v.cancelLongPress();flush();dragging=true;editorTools.setVisibility(View.VISIBLE);}
                    if(dragging){int count=(int)((e.getX()-lastX)/step);if(count!=0){for(int i=0;i<Math.abs(count);i++)moveCursor(count>0?1:-1);lastX+=count*step;}showMagnifier();return true;}
                }else if(e.getAction()==MotionEvent.ACTION_UP||e.getAction()==MotionEvent.ACTION_CANCEL){if(magnifier!=null)magnifier.dismiss();if(dragging){v.setPressed(false);return true;}}
                return false;
            }
        });add(bottom,space,2.8f,keyHeight());
        add(bottom,function(enterLabel(),v->enter()),1.3f,keyHeight());keys.addView(bottom);
    }
    private int keyHeight(){return getResources().getConfiguration().orientation==android.content.res.Configuration.ORIENTATION_LANDSCAPE?42:56;}
    private Button key(String label,View.OnClickListener action){Button b=Ui.button(this,label,action);b.setTextSize(label.length()==1?24:label.contains("\n")?20:17);b.setGravity(Gravity.CENTER);b.setIncludeFontPadding(false);b.setHapticFeedbackEnabled(true);b.setSelected((label.equals("⇧")&&shift)||(label.equals("123")&&numbers)||(symbols&&label.equals(symbolCategory))||(label.equals("中/英")&&english));return b;}
    private Button function(String label,View.OnClickListener action){Button b=key(label,action);Ui.colors(b,Ui.GREEN);return b;}
    private Button deleteKey(){
        Button b=key("⌫",v->backspace());
        final Runnable repeat=new Runnable(){public void run(){backspace();ui.postDelayed(this,65);}};
        b.setOnTouchListener((v,e)->{if(e.getAction()==MotionEvent.ACTION_DOWN)ui.postDelayed(repeat,400);if(e.getAction()==MotionEvent.ACTION_UP||e.getAction()==MotionEvent.ACTION_CANCEL)ui.removeCallbacks(repeat);return false;});return b;
    }
    private void showLayouts(){
        View anchor=keys;PopupMenu menu=new PopupMenu(this,anchor);menu.getMenu().add("拼音九宫格");menu.getMenu().add("拼音全键盘");menu.getMenu().add("英文全键盘");
        menu.setOnMenuItemClickListener(item->{flush();String label=item.getTitle().toString();english=label.startsWith("英文");if(!english){nine=label.equals("拼音九宫格");getSharedPreferences("layout",0).edit().putBoolean("nine",nine).apply();}numbers=false;symbols=false;shift=false;query();renderKeys();return true;});menu.show();
    }
    private void renderNine(){
        String[][] labels={{"，","1\n分词","2\nABC","3\nDEF","⌫"},{"。","4\nGHI","5\nJKL","6\nMNO","←"},{"？","7\nPQRS","8\nTUV","9\nWXYZ","→"}};
        for(String[] line:labels){LinearLayout row=new LinearLayout(this);for(int i=0;i<line.length;i++){
            final String label=line[i];Button b;
            if(label.equals("⌫"))b=deleteKey();
            else if(label.equals("←")||label.equals("→"))b=function(label,v->moveCursor(label.equals("←")?-1:1));
            else if(label.equals("中/英"))b=function(label,v->{flush();english=!english;renderKeys();});
            else if(label.startsWith("1"))b=key(label,v->{if(!code.isEmpty()){code+="'";query();}});
            else if(label.contains("\n"))b=key(label,v->letter(label.substring(0,1)));
            else b=key(label,v->commitPunctuation(label));
            add(row,b,i==0||i==4?0.65f:1.55f,keyHeight());
        }keys.addView(row);}
    }
    private void renderNumbers(){
        String[][] rows={{"+","1","2","3","⌫"},{"-","4","5","6","."},{"%","7","8","9","/"},{"(",")","0","00","@"}};
        for(String[] line:rows){LinearLayout row=new LinearLayout(this);for(String s:line)add(row,s.equals("⌫")?deleteKey():key(s,v->{flush();commit(s);}),1,keyHeight());keys.addView(row);}
    }
    private void renderSymbols(){
        LinearLayout tabs=new LinearLayout(this);for(String category:new String[]{"常用","中文","英文","数学"})add(tabs,function(category,v->{symbolCategory=category;renderKeys();}),1,34);add(tabs,deleteKey(),1,34);keys.addView(tabs);
        String[][] sets=symbolCategory.equals("中文")?new String[][]{{"，","。","？","！","、","；"},{"：","“","”","‘","’","…"},{"（","）","《","》","【","】"}}:symbolCategory.equals("英文")?new String[][]{{",",".","?","!",";",":"},{"'","\"","(",")","[","]"},{"@","#","_","/","\\","&"}}:symbolCategory.equals("数学")?new String[][]{{"+","−","×","÷","=","≠"},{"<",">","≤","≥","±","%"},{"℃","°","㎡","³","∞","√"}}:new String[][]{{"，","。","？","！","、","："},{"@","#","/","-","（","）"},{"😊","👍","❤️","🎉","🙏","￥"}};
        for(int n=0;n<sets.length;n++){LinearLayout row=new LinearLayout(this);for(String s:sets[n])add(row,key(s,v->commitPunctuation(s)),1,keyHeight());keys.addView(row);}
    }
    private void commitPunctuation(String s){if(!code.isEmpty()&&!english)choose(results.isEmpty()?code:results.get(0));else flush();commit(s);}
    private String enterLabel(){return "换行";}
    private void enter(){
        if(!code.isEmpty()){choose(results.isEmpty()?code:results.get(0));return;}if(editing){commit("\n");return;}
        commit("\n");
    }
    private void letter(String s){
        if(numbers || symbols || password){flush();commit(s);return;}
        if(code.length()<48){code+=s;page=0;query();}
    }
    private void commit(String s){
        if(editing){int start=Math.max(0,draft.getSelectionStart()),end=Math.max(0,draft.getSelectionEnd());int pos=Math.min(start,end);draft.getText().replace(pos,Math.max(start,end),s);draft.setSelection(pos+s.length());}
        else{InputConnection ic=getCurrentInputConnection();if(ic!=null){
            String before="",after="";boolean correction=false;
            if(!password&&!english){CharSequence b=ic.getTextBeforeCursor(24,0),selected=ic.getSelectedText(0),a=ic.getTextAfterCursor(24,0);before=b==null?"":b.toString();after=a==null?"":a.toString();correction=selected!=null&&selected.length()>0;}
            ic.commitText(s,1);
            if(!password&&!english)learnText(chineseSuffix(before)+s+(correction?chinesePrefix(after):""));
        }}
    }
    private static String chineseSuffix(String s){int i=s.length();while(i>0&&s.charAt(i-1)>=0x3400&&s.charAt(i-1)<=0x9fff)i--;return s.substring(i);}
    private static String chinesePrefix(String s){int i=0;while(i<s.length()&&s.charAt(i)>=0x3400&&s.charAt(i)<=0x9fff)i++;return s.substring(0,i);}
    private void learnText(String text){
        if(password||english)return;
        for(String phrase:text.split("[^\\u3400-\\u9fff]+"))if(phrase.length()>=2&&phrase.length()<=24)learnWord("",phrase);
    }
    private void learnWord(String typed,String text){lookup.execute(()->{try{if(personal!=null)personal.learn(typed,text);}catch(java.io.IOException e){ui.post(()->{if(status!=null)status.setText("本机记词失败："+e.getMessage());});}});}
    private void backspace(){
        if(!code.isEmpty()){code=code.substring(0,code.length()-1);if(code.isEmpty())clearEnglishComposition();query();}
        else if(editing){int p=Math.max(0,draft.getSelectionStart()),end=Math.max(0,draft.getSelectionEnd());if(p!=end){int low=Math.min(p,end);draft.getText().delete(low,Math.max(p,end));draft.setSelection(low);}else if(p>0){int low=draft.getText().toString().offsetByCodePoints(p,-1);draft.getText().delete(low,p);draft.setSelection(low);}}
        else{InputConnection ic=getCurrentInputConnection();if(ic!=null){CharSequence selected=ic.getSelectedText(0);if(selected!=null&&selected.length()>0)ic.commitText("",1);else ic.deleteSurroundingTextInCodePoints(1,0);}}
    }
    private void flush(){if(!code.isEmpty())choose(!english&&!results.isEmpty()?results.get(0):code);}
    private void clearEnglishComposition(){if(english&&!editing&&!password){InputConnection ic=getCurrentInputConnection();if(ic!=null){ic.setComposingText("",1);ic.finishComposingText();}}}
    private void choose(String text){
        if(!password&&!english&&!code.isEmpty()&&!text.equals(code)){
            android.content.SharedPreferences prefs=getSharedPreferences("choices",0);
            prefs.edit().putString((nine?"t9:":"")+code,text).apply();
            learnWord((nine?"t9:":"")+code,text);
        }
        commit(text);code="";page=0;query();
    }
    private void query(){
        if(english&&!password&&!editing){InputConnection ic=getCurrentInputConnection();if(ic!=null){if(code.isEmpty())ic.finishComposingText();else ic.setComposingText(code,1);}}
        if(composition==null)return;
        if(status!=null&&!recording)status.setVisibility(View.GONE);
        composition.setVisibility(code.isEmpty()?View.GONE:View.VISIBLE);root.findViewWithTag("candidates-scroll").setVisibility(code.isEmpty()?View.GONE:View.VISIBLE);
        composition.setText(code);final String request=code;final int id=++generation;results=new ArrayList<>();candidates.removeAllViews();
        if(request.isEmpty()||dictionary==null)return;
        final boolean en=english,t9=nine&&!english&&!password;
        lookup.execute(()->{if(id!=generation)return;List<String> list;
            if(t9){try{if(nineDictionary==null){nineDictionary=new NineKeyDictionary(Assets.copy(this,"dictionary-015.bin","dictionary.bin"));nineDictionary.loadHotwords(getAssets().open("hot-chinese.tsv"));}list=new ArrayList<>();
                String[] parts=request.split("'",-1);String prefix="";for(int i=0;i<parts.length;i++){if(parts[i].isEmpty()){if(i==parts.length-1&&!prefix.isEmpty())list.add(prefix);continue;}List<String> part=nineDictionary.query(parts[i]);if(i<parts.length-1){if(!part.isEmpty())prefix+=part.get(0);}else for(String s:part)list.add(prefix+s);}
            }catch(Exception e){list=new ArrayList<>();}}
            else list=en?(englishDictionary==null?new ArrayList<>():englishDictionary.query(request)):dictionary.query(request);
            String prior=password||en?null:getSharedPreferences("choices",0).getString((t9?"t9:":"")+request,null);if(prior!=null){list.remove(prior);list.add(0,prior);}
            if(!password&&!en&&personal!=null)list=personal.rank((t9?"t9:":"")+request.replace("'",""),list);
            final List<String> found=list;ui.post(()->{if(id!=generation || candidates==null)return;results=found;page=0;showCandidates();});});
    }
    private void showCandidates(){
        candidates.removeAllViews();int start=page*9;
        for(int i=start;i<Math.min(results.size(),start+9);i++){String text=results.get(i);Button b=Ui.button(this,text,v->choose(text));b.setTextSize(22);b.setSelected(true);b.setPadding(Ui.dp(this,16),0,Ui.dp(this,16),0);if(i==start){b.setTypeface(null,android.graphics.Typeface.BOLD);b.setTextSize(24);}candidates.addView(b,new LinearLayout.LayoutParams(-2,-1));}
        if(page>0)candidates.addView(Ui.button(this,"‹",v->{page--;showCandidates();}));
        if(start+9<results.size())candidates.addView(Ui.button(this,"›",v->{page++;showCandidates();}));
    }
    private void toggleVoice(){
        status.setVisibility(View.VISIBLE);
        if(password){status.setText("密码输入框不启用语音");return;}
        if(checkSelfPermission(Manifest.permission.RECORD_AUDIO)!=PackageManager.PERMISSION_GRANTED){startActivity(new Intent(this,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));status.setText("请在数萌首页授权麦克风");return;}
        if(recording){voice.stop();recording=false;micButton.setText("语音");micButton.setSelected(false);}
        else{flush();draft.clearFocus();draft.setVisibility(View.VISIBLE);root.findViewWithTag("draft-actions").setVisibility(View.VISIBLE);editing=false;recording=true;micButton.setText("停止");micButton.setSelected(true);voice.start();}
    }
    private void voiceEvent(int type,String text){
        if(status==null)return;
        if(type==VoiceService.TEXT){String command=text.replaceAll("[\\s，。！？,.!?]","");if(command.equals("清除")||command.equals("清空")){voiceHistory.add(draft.getText().toString());draft.setText("");}else if(command.equals("撤销")){if(!voiceHistory.isEmpty())draft.setText(voiceHistory.remove(voiceHistory.size()-1));}else{voiceHistory.add(draft.getText().toString());if(voiceHistory.size()>100)voiceHistory.remove(0);int start=draft.getSelectionStart(),end=draft.getSelectionEnd();String previous=draft.getText().toString();String joined=SpeechPunctuation.join(previous,text);draft.getText().append(joined.substring(previous.length()));if(editing)draft.setSelection(Math.max(0,start),Math.max(0,end));else draft.setSelection(draft.length());}}
        else{status.setVisibility(View.VISIBLE);status.setText(text);if(type==VoiceService.ERROR){voice.stop();recording=false;micButton.setText("语音");micButton.setSelected(false);}}
    }
    @Override public void onStartInput(EditorInfo info,boolean restarting){
        super.onStartInput(info,restarting);
        int type=info.inputType,variation=type&InputType.TYPE_MASK_VARIATION;
        boolean secure=((type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_TEXT && (variation==InputType.TYPE_TEXT_VARIATION_PASSWORD || variation==InputType.TYPE_TEXT_VARIATION_VISIBLE_PASSWORD || variation==InputType.TYPE_TEXT_VARIATION_WEB_PASSWORD)) || ((type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_NUMBER && variation==InputType.TYPE_NUMBER_VARIATION_PASSWORD);
        boolean numeric=(type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_NUMBER || (type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_PHONE;
        if(restarting&&Objects.equals(editorPackage,info.packageName)&&editorField==info.fieldId&&password==secure&&editorType==type){query();return;}
        editorPackage=info.packageName;editorField=info.fieldId;editorType=type;
        if(voice!=null)voice.cancel();recording=false;code="";generation++;editing=false;symbols=false;voiceHistory.clear();password=secure;
        numbers=(type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_NUMBER || (type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_PHONE;
        selecting=false;selectionAnchor=-1;
        if(draft!=null){draft.setText("");draft.setVisibility(View.GONE);root.findViewWithTag("draft-actions").setVisibility(View.GONE);micButton.setText("语音");}
        query();renderKeys();
    }
    @Override public void onFinishInputView(boolean finishingInput){if(voice!=null)voice.cancel();recording=false;if(finishingInput)editing=false;super.onFinishInputView(finishingInput);}
    @Override public void onFinishInput(){voice.cancel();InputConnection ic=getCurrentInputConnection();if(english&&ic!=null)ic.finishComposingText();code="";generation++;super.onFinishInput();}
    @Override public boolean onEvaluateFullscreenMode(){return false;}
    @Override public void onUpdateSelection(int oldStart,int oldEnd,int newStart,int newEnd,int composingStart,int composingEnd){
        super.onUpdateSelection(oldStart,oldEnd,newStart,newEnd,composingStart,composingEnd);
        if(english&&!editing&&!code.isEmpty()&&(newStart!=oldStart||newEnd!=oldEnd)&&(newStart!=composingEnd||newEnd!=composingEnd)){
            code="";page=0;InputConnection ic=getCurrentInputConnection();if(ic!=null)ic.finishComposingText();query();
        }
        if(cursorPreview!=null&&cursorPreview.getVisibility()==View.VISIBLE)updateCursorPreview();
    }
    private void showEditor(){flush();selecting=false;selectionAnchor=-1;editorTools.setVisibility(editorTools.getVisibility()==View.VISIBLE?View.GONE:View.VISIBLE);updateCursorPreview();}
    private ExtractedText editorText(){InputConnection ic=getCurrentInputConnection();return ic==null?null:ic.getExtractedText(new ExtractedTextRequest(),0);}
    private void moveCursor(int direction){
        flush();
        String text;int start,end,offset=0;
        if(editing){text=draft.getText().toString();start=draft.getSelectionStart();end=draft.getSelectionEnd();}
        else{ExtractedText e=editorText();if(e==null||e.text==null)return;text=e.text.toString();start=e.selectionStart;end=e.selectionEnd;offset=e.startOffset;}
        start=Math.max(0,Math.min(start,text.length()));end=Math.max(0,Math.min(end,text.length()));
        if(selecting&&selectionAnchor<0)selectionAnchor=start;
        int pos;
        if(!selecting&&start!=end)pos=direction<0?Math.min(start,end):Math.max(start,end);
        else{pos=end;int next=pos+direction;if(next>=0&&next<=text.length())pos=text.offsetByCodePoints(pos,direction);}
        int anchor=selecting?selectionAnchor:pos;
        if(editing)draft.setSelection(anchor,pos);else{InputConnection ic=getCurrentInputConnection();if(ic!=null)ic.setSelection(offset+anchor,offset+pos);}
        updateCursorPreview();
    }
    private void updateCursorPreview(){
        if(cursorPreview==null)return;cursorPreview.setVisibility(View.VISIBLE);CharSequence text;int pos;
        if(password){cursorPreview.setText("密码框 · 左右键移动光标");return;}
        if(editing){text=draft.getText();pos=draft.getSelectionEnd();}
        else{ExtractedText e=editorText();if(e==null||e.text==null){cursorPreview.setText("左右键移动 · 长按空格进入编辑");return;}text=e.text;pos=e.selectionEnd;}
        pos=Math.max(0,Math.min(pos,text.length()));int left=Math.max(0,pos-12),right=Math.min(text.length(),pos+12);
        if(left>0&&Character.isLowSurrogate(text.charAt(left)))left--;if(right<text.length()&&Character.isLowSurrogate(text.charAt(right)))right++;
        cursorPreview.setText(text.subSequence(left,pos)+"│"+text.subSequence(pos,right));
    }
    private void showMagnifier(){
        if(Build.VERSION.SDK_INT<28||password||cursorPreview.getWidth()==0||cursorPreview.getLayout()==null)return;
        if(magnifier==null)magnifier=new Magnifier(cursorPreview);
        int marker=cursorPreview.getText().toString().indexOf('│');if(marker<0)return;
        float x=cursorPreview.getPaddingLeft()+cursorPreview.getLayout().getPrimaryHorizontal(marker)-cursorPreview.getScrollX();
        magnifier.show(Math.max(0,Math.min(x,cursorPreview.getWidth())),cursorPreview.getHeight()/2f);
    }
    private void editAction(String label){
        flush();selecting=false;selectionAnchor=-1;
        int id=label.equals("全选")?android.R.id.selectAll:label.equals("复制")?android.R.id.copy:label.equals("剪切")?android.R.id.cut:android.R.id.paste;
        if(editing)draft.onTextContextMenuItem(id);else{InputConnection ic=getCurrentInputConnection();if(ic!=null)ic.performContextMenuAction(id);}
        updateCursorPreview();
    }
    @Override public void onDestroy(){voice.close();lookup.shutdownNow();super.onDestroy();}
}
