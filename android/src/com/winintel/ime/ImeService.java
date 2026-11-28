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
    private IndustryLexicon industry;
    private String editorPackage;
    private int editorField;
    private int editorType;
    private VoiceClient voice;
    private LinearLayout root,candidates,keys;
    private HorizontalScrollView candidateStrip;
    private Runnable deleteRepeat;
    private boolean deleteRepeating;
    private Button candidatePrevious,candidateNext;
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
    private static final int PAGE_SIZE=25;
    private int candidateEnd;
    private final List<Integer> previousPages=new ArrayList<>();
    private boolean expandedCandidates;
    private ScrollView candidatePanel;
    private LinearLayout candidateGrid;
    private Button candidateMore;
    private List<String> results=new ArrayList<>();
    @Override public void onCreate(){
        super.onCreate();voice=new VoiceClient(this,this::voiceEvent);nine=getSharedPreferences("layout",0).getBoolean("nine",true);
        lookup.execute(()->{try{java.io.File nameParts=Assets.copy(this,"lexicon-019/name-parts.tsv","name-parts.tsv");Assets.copy(this,"lexicon-019/character-readings.tsv","character-readings.tsv");dictionary=new Dictionary(Assets.copy(this,"lexicon-019/dictionary.bin","dictionary.bin"));dictionary.loadHotwords(getAssets().open("hot-chinese.tsv"));englishDictionary=new EnglishDictionary(getAssets().open("english-words.tsv"));industry=new IndustryLexicon(getAssets().open("industry-words.tsv"));personal=new PersonalLexicon(new java.io.File(getFilesDir(),"personal-words.tsv"),getAssets().open("character-readings.tsv"),nameParts);ui.post(()->{if(status!=null)status.setText("离线中英文与行业词库已就绪");query();});}catch(Exception e){ui.post(()->{if(status!=null)status.setText("词库加载失败："+e.getMessage());});}});
    }
    @Override public View onCreateInputView(){
        root=Ui.column(this);root.setBackgroundColor(Ui.BG);root.setPadding(Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,4),Ui.dp(this,8));
        root.setOnApplyWindowInsetsListener((v,insets)->{v.setPadding(Ui.dp(this,4),Ui.dp(this,2),Ui.dp(this,4),Math.max(Ui.dp(this,2),insets.getSystemWindowInsetBottom()));return insets;});
        LinearLayout tools=new LinearLayout(this);
        languageButton=Ui.button(this,"中 / EN",v->{flush();english=!english;renderKeys();});add(tools,languageButton,1,38);
        micButton=Ui.button(this,"语音",v->toggleVoice());add(tools,micButton,1,38);
        add(tools,Ui.button(this,"布局",v->showLayouts()),1,38);
        add(tools,Ui.button(this,"工具",v->{PopupMenu menu=new PopupMenu(this,v);menu.getMenu().add("光标与剪贴板");menu.getMenu().add("切换输入法");menu.getMenu().add("行业词库");menu.getMenu().add("每日新闻");menu.getMenu().add("收起键盘");menu.setOnMenuItemClickListener(item->{flush();if(item.getTitle().equals("光标与剪贴板"))showEditor();else if(item.getTitle().equals("切换输入法"))((InputMethodManager)getSystemService(INPUT_METHOD_SERVICE)).showInputMethodPicker();else if(item.getTitle().equals("收起键盘"))requestHideSelf(0);else if(item.getTitle().equals("行业词库"))startActivity(new Intent(this,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));else startActivity(new Intent(this,NewsActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));return true;});menu.show();}),1,38);
        composition=Ui.text(this,"",14);composition.setPadding(Ui.dp(this,8),0,Ui.dp(this,8),0);composition.setSingleLine(true);composition.setVisibility(View.GONE);root.addView(composition,new LinearLayout.LayoutParams(-1,Ui.dp(this,22)));
        candidates=new LinearLayout(this);candidates.setBackgroundColor(Ui.BG);candidates.setTag("candidate-list");
        LinearLayout candidateRow=new LinearLayout(this);candidateRow.setTag("candidates-scroll");candidateRow.setVisibility(View.GONE);
        candidateStrip=new HorizontalScrollView(this);candidateStrip.setFillViewport(false);
        candidateStrip.addView(candidates,new HorizontalScrollView.LayoutParams(-2,-1));
        candidateRow.addView(candidateStrip,new LinearLayout.LayoutParams(0,-1,1));
        candidatePrevious=Ui.button(this,"上页",v->{if(page>0){page=Math.max(0,page-PAGE_SIZE);showCandidates();}});
        candidateNext=Ui.button(this,"下页",v->{if(page+PAGE_SIZE<results.size()){page+=PAGE_SIZE;showCandidates();}});
        candidateMore=Ui.button(this,"展开",v->toggleCandidates());
        candidatePrevious.setContentDescription("上一页候选");candidateNext.setContentDescription("下一页候选");
        candidateRow.addView(candidatePrevious,new LinearLayout.LayoutParams(Ui.dp(this,48),-1));
        candidateRow.addView(candidateNext,new LinearLayout.LayoutParams(Ui.dp(this,48),-1));
        candidateRow.addView(candidateMore,new LinearLayout.LayoutParams(Ui.dp(this,48),-1));
        for(Button button:new Button[]{candidatePrevious,candidateNext,candidateMore})button.setTextSize(14);
        root.addView(candidateRow,new LinearLayout.LayoutParams(-1,Ui.dp(this,52)));
        status=Ui.text(this,"数萌 AI · 本机处理",12);status.setVisibility(View.GONE);root.addView(status);
        draft=new EditText(this);draft.setTextSize(17);draft.setTextColor(Ui.INK);draft.setHint("语音草稿");draft.setSingleLine(false);draft.setMaxLines(2);draft.setShowSoftInputOnFocus(false);draft.setVisibility(View.GONE);root.addView(draft,new LinearLayout.LayoutParams(-1,Ui.dp(this,56)));
        draft.setOnTouchListener((v,e)->{if(e.getAction()==MotionEvent.ACTION_DOWN){flush();editing=true;if(recording){voice.cancel();recording=false;micButton.setText("语音");micButton.setSelected(false);}}return false;});
        LinearLayout actions=new LinearLayout(this);actions.setTag("draft-actions");actions.setVisibility(View.GONE);
        add(actions,Ui.button(this,"清除",v->{voice.cancel();recording=false;micButton.setText("语音");micButton.setSelected(false);draft.setText("");}),1,36);
        add(actions,Ui.button(this,"确认上屏",v->{flush();voice.cancel();recording=false;micButton.setText("语音");micButton.setSelected(false);String s=draft.getText().toString();InputConnection ic=getCurrentInputConnection();if(ic!=null&&!s.isEmpty()){ic.finishComposingText();ic.commitText(s,1);learnText(s);draft.setText("");editing=false;draft.clearFocus();} }),1,36);root.addView(actions);
        editorTools=Ui.column(this);editorTools.setVisibility(View.GONE);
        cursorPreview=Ui.text(this,"",19);cursorPreview.setSingleLine(true);cursorPreview.setVisibility(View.GONE);editorTools.addView(cursorPreview);
        LinearLayout navigation=new LinearLayout(this);
        add(navigation,function("←",v->moveCursor(-1)),1,44);
        add(navigation,function("选择",v->{selecting=!selecting;v.setSelected(selecting);selectionAnchor=-1;status.setVisibility(View.VISIBLE);status.setText(selecting?"用左右键扩展选区":"用左右键移动光标");}),1,44);
        add(navigation,function("→",v->moveCursor(1)),1,44);
        for(String label:new String[]{"全选","复制","剪切","粘贴"})add(navigation,function(label,v->editAction(label)),1,44);
        editorTools.addView(navigation);root.addView(editorTools);
        keys=Ui.column(this);root.addView(keys);
        candidatePanel=new ScrollView(this);candidatePanel.setVisibility(View.GONE);candidateGrid=Ui.column(this);candidatePanel.addView(candidateGrid);root.addView(candidatePanel,new LinearLayout.LayoutParams(-1,Ui.dp(this,4*(keyHeight()+4))));
        root.addView(tools);renderKeys();return root;
    }
    private void add(LinearLayout row,View view,float weight,int height){
        row.setBaselineAligned(false);
        LinearLayout.LayoutParams p=new LinearLayout.LayoutParams(0,Ui.dp(this,height),weight);p.setMargins(Ui.dp(this,2),Ui.dp(this,2),Ui.dp(this,2),Ui.dp(this,2));row.addView(view,p);
    }
    private void renderKeys(){
        if(keys==null)return;stopDeleteRepeat();keys.removeAllViews();if(languageButton!=null)languageButton.setSelected(english);if(micButton!=null)micButton.setSelected(recording);
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
        if(!numbers)add(bottom,function("123",v->{flush();symbols=false;numbers=true;renderKeys();}),1,keyHeight());
        if(numbers)add(bottom,key("0",v->{flush();commit("0");}),1,keyHeight());
        else if(!nine||english||password||symbols){Button punctuation=key(english?".":"，",v->{commitPunctuation(english?".":"，");});punctuation.setOnLongClickListener(v->{commitPunctuation(english?",":"。");return true;});add(bottom,punctuation,0.8f,keyHeight());}
        Button space=key(english?"Space":"空格",v->{if(english){flush();commit(" ");}else if(!code.isEmpty())choose(results.isEmpty()?code:results.get(0));else commit(" ");});
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
        if(candidatePanel!=null)candidatePanel.getLayoutParams().height=Ui.dp(this,keyboardAreaHeight());
    }
    private int keyboardAreaHeight(){return (symbols?5:4)*(keyHeight()+4);}
    private int keyHeight(){
        android.content.res.Configuration c=getResources().getConfiguration();
        int rows=symbols?5:4;
        boolean landscape=c.orientation==android.content.res.Configuration.ORIENTATION_LANDSCAPE;
        int budget=Math.min(landscape?240:340,(int)(c.screenHeightDp*(landscape?0.55:0.43)));
        int reserved=42+74+16;
        if(editorTools!=null&&editorTools.getVisibility()==View.VISIBLE)reserved+=72;
        if(draft!=null&&draft.getVisibility()==View.VISIBLE)reserved+=96;
        if(status!=null&&status.getVisibility()==View.VISIBLE)reserved+=20;
        return Math.max(24,Math.min(landscape?34:48,(budget-reserved)/rows-4));
    }
    private Button key(String label,View.OnClickListener action){Button b=Ui.button(this,label,action);b.setTextSize(label.length()==1?24:label.contains("\n")?20:17);b.setGravity(Gravity.CENTER);b.setIncludeFontPadding(false);
        if(label.contains("\n")){b.setMaxLines(2);b.setAutoSizeTextTypeUniformWithConfiguration(8,20,1,android.util.TypedValue.COMPLEX_UNIT_SP);}
        b.setHapticFeedbackEnabled(true);b.setSelected((label.equals("⇧")&&shift)||(label.equals("123")&&numbers)||(symbols&&label.equals(symbolCategory))||(label.equals("中/英")&&english));return b;}
    private Button function(String label,View.OnClickListener action){Button b=key(label,action);Ui.colors(b,Ui.GREEN);return b;}
    private Button deleteKey(){
        Button b=key("⌫",v->backspace());
        final Runnable repeat=new Runnable(){public void run(){deleteRepeating=true;backspace();ui.postDelayed(this,65);}};
        b.setOnTouchListener((v,e)->{
            if(e.getAction()==MotionEvent.ACTION_DOWN){stopDeleteRepeat();deleteRepeat=repeat;deleteRepeating=false;ui.postDelayed(repeat,400);}
            if(e.getAction()==MotionEvent.ACTION_UP||e.getAction()==MotionEvent.ACTION_CANCEL){
                boolean repeated=deleteRepeating;stopDeleteRepeat();
                if(repeated||e.getAction()==MotionEvent.ACTION_CANCEL){v.setPressed(false);return true;}
            }
            return false;
        });return b;
    }
    private void stopDeleteRepeat(){if(deleteRepeat!=null)ui.removeCallbacks(deleteRepeat);deleteRepeat=null;deleteRepeating=false;}
    private void showLayouts(){
        View anchor=keys;PopupMenu menu=new PopupMenu(this,anchor);menu.getMenu().add("拼音九宫格");menu.getMenu().add("拼音全键盘");menu.getMenu().add("英文全键盘");
        menu.setOnMenuItemClickListener(item->{flush();String label=item.getTitle().toString();english=label.startsWith("英文");if(!english){nine=label.equals("拼音九宫格");getSharedPreferences("layout",0).edit().putBoolean("nine",nine).apply();}numbers=false;symbols=false;shift=false;query();renderKeys();return true;});menu.show();
    }
    private void renderNine(){
        String[][] labels={{"，","1\n分词","2\nABC","3\nDEF","⌫"},{"。","4\nGHI","5\nJKL","6\nMNO","选词"},{"？","7\nPQRS","8\nTUV","9\nWXYZ","收起"}};
        for(String[] line:labels){LinearLayout row=new LinearLayout(this);for(int i=0;i<line.length;i++){
            final String label=line[i];Button b;
            if(label.equals("⌫"))b=deleteKey();
            else if(label.equals("选词"))b=function(label,v->toggleCandidates());
            else if(label.equals("收起"))b=function(label,v->requestHideSelf(0));
            else if(label.equals("←")||label.equals("→"))b=function(label,v->moveCursor(label.equals("←")?-1:1));
            else if(label.equals("中/英"))b=function(label,v->{flush();english=!english;renderKeys();});
            else if(label.startsWith("1"))b=key(label,v->{if(!code.isEmpty()){code+="'";query();}});
            else if(label.contains("\n"))b=key(label,v->letter(label.substring(0,1)));
            else b=key(label,v->commitPunctuation(label));
            add(row,b,i==0||i==4?0.65f:1.55f,keyHeight());
        }keys.addView(row);}
    }
    private void renderNumbers(){
        String[][] rows={{"+","1","2","3","⌫"},{"-","4","5","6","."},{"%","7","8","9","/"}};
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
        if(code.length()<256){code+=s;page=0;query();}
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
        composition.setText(code);final String request=code;final int id=++generation;results=new ArrayList<>();page=0;previousPages.clear();candidateEnd=0;candidates.removeAllViews();
        if(request.isEmpty()){expandedCandidates=false;updateCandidatePanel();return;}
        candidates.addView(Ui.text(this,dictionary==null?"词库加载中…":"查找中…",14));
        if(dictionary==null)return;
        final boolean en=english,t9=nine&&!english&&!password;
        final String profile=getSharedPreferences("industry",0).getString("profile","general");
        lookup.execute(()->{if(id!=generation)return;List<String> list;
            if(t9){try{if(nineDictionary==null){try{Assets.copy(this,"lexicon-019/t9-index-v2.bin","t9-index-v2.bin");}catch(java.io.IOException ignored){}nineDictionary=new NineKeyDictionary(Assets.copy(this,"lexicon-019/dictionary.bin","dictionary.bin"));nineDictionary.loadHotwords(getAssets().open("hot-chinese.tsv"));}list=new ArrayList<>();
                String[] parts=request.split("'",-1);String prefix="";for(int i=0;i<parts.length;i++){if(parts[i].isEmpty()){if(i==parts.length-1&&!prefix.isEmpty())list.add(prefix);continue;}List<String> part=nineDictionary.query(parts[i]);if(i<parts.length-1){if(!part.isEmpty())prefix+=part.get(0);}else for(String s:part)list.add(prefix+s);}
            }catch(Exception e){list=new ArrayList<>();ui.post(()->{if(id==generation){status.setVisibility(View.VISIBLE);status.setText("候选加载失败，请重新打开键盘");}});}}
            else list=en?(englishDictionary==null?new ArrayList<>():englishDictionary.query(request)):dictionary.query(request);
            if(!password&&industry!=null)list=industry.rank(profile,request,list,en,t9);
            if(!password&&!en&&!t9&&dictionary!=null)list=dictionary.expandCandidates(request,list);
            String prior=password||en?null:getSharedPreferences("choices",0).getString((t9?"t9:":"")+request,null);if(prior!=null){list.remove(prior);list.add(0,prior);}
            if(!password&&!en&&personal!=null)list=personal.rank((t9?"t9:":"")+request.replace("'",""),list);
            final List<String> found=list;ui.post(()->{if(id!=generation || candidates==null)return;results=found;page=0;previousPages.clear();showCandidates();});});
    }
    private void showCandidates(){
        candidates.removeAllViews();
        candidateEnd=Math.min(results.size(),page+PAGE_SIZE);
        for(int i=page;i<candidateEnd;i++){
            String text=results.get(i);Button b=candidateButton(text,i);b.setSingleLine(true);b.setEllipsize(android.text.TextUtils.TruncateAt.END);
            int width=Math.max(Ui.dp(this,48),(int)Math.ceil(b.getPaint().measureText(text))+Ui.dp(this,24));
            width=Math.min(width,Ui.dp(this,240));candidates.addView(b,new LinearLayout.LayoutParams(width,-1));
        }
        candidateStrip.scrollTo(0,0);
        if(results.isEmpty())candidates.addView(Ui.text(this,"暂无候选，请检查分词",14));
        candidatePrevious.setEnabled(page>0);candidatePrevious.setAlpha(page>0?1f:0.35f);
        candidateNext.setEnabled(candidateEnd<results.size());candidateNext.setAlpha(candidateEnd<results.size()?1f:0.35f);
        candidateNext.setContentDescription("下一页候选，当前第"+(page+1)+"至"+candidateEnd+"项，共"+results.size()+"项");
        candidateMore.setContentDescription((expandedCandidates?"收起":"展开全部")+"候选，共"+results.size()+"项");
        updateCandidatePanel();
    }
    private Button candidateButton(String text,int index){
        Button b=Ui.button(this,text,v->choose(text));b.setTextSize(20);b.setSelected(index==0);b.setPadding(Ui.dp(this,12),0,Ui.dp(this,12),0);b.setContentDescription("候选"+(index+1)+"，"+text);
        if(index==0)b.setTypeface(null,android.graphics.Typeface.BOLD);
        return b;
    }
    private void toggleCandidates(){
        if(code.isEmpty()||results.isEmpty())return;expandedCandidates=!expandedCandidates;
        editorTools.setVisibility(View.GONE);renderKeys();updateCandidatePanel();
    }
    private void updateCandidatePanel(){
        if(candidatePanel==null)return;
        keys.setVisibility(expandedCandidates?View.GONE:View.VISIBLE);candidatePanel.setVisibility(expandedCandidates?View.VISIBLE:View.GONE);candidateMore.setText(expandedCandidates?"键盘":"25项");
        if(!expandedCandidates)return;
        candidateGrid.removeAllViews();
        TextView count=Ui.text(this,"第 "+(page/PAGE_SIZE+1)+" / "+((results.size()+PAGE_SIZE-1)/PAGE_SIZE)+" 页 · 每页25项 · 共"+results.size()+"项",12);count.setPadding(Ui.dp(this,8),0,0,0);candidateGrid.addView(count);
        appendCandidateBatch();
        candidatePanel.scrollTo(0,0);
    }
    private int expandedCount;
    private void appendCandidateBatch(){
        int end=Math.min(results.size(),page+PAGE_SIZE);
        int available=candidatePanel.getWidth();if(available<=0)available=root.getWidth();
        if(available<=0)available=Ui.dp(this,getResources().getConfiguration().screenWidthDp-8);
        LinearLayout row=null;int used=0;
        for(int i=page;i<end;i++){
            Button b=candidateButton(results.get(i),i);b.setTextSize(18);
            b.setSingleLine(false);b.setMaxLines(Integer.MAX_VALUE);
            int width=Math.min(available,Math.max(Ui.dp(this,64),(int)Math.ceil(b.getPaint().measureText(results.get(i)))+Ui.dp(this,28)));
            if(row==null||used+width>available){row=new LinearLayout(this);candidateGrid.addView(row);used=0;}
            b.setMinimumHeight(Ui.dp(this,48));
            row.addView(b,new LinearLayout.LayoutParams(width,-2));used+=width;
        }
    }
    private void toggleVoice(){
        status.setVisibility(View.VISIBLE);
        if(password){status.setText("密码输入框不启用语音");return;}
        if(checkSelfPermission(Manifest.permission.RECORD_AUDIO)!=PackageManager.PERMISSION_GRANTED){startActivity(new Intent(this,MainActivity.class).addFlags(Intent.FLAG_ACTIVITY_NEW_TASK));status.setText("请在数萌首页授权麦克风");return;}
        if(recording){voice.stop();recording=false;micButton.setText("语音");micButton.setSelected(false);}
        else{flush();draft.clearFocus();draft.setVisibility(View.VISIBLE);root.findViewWithTag("draft-actions").setVisibility(View.VISIBLE);editing=false;recording=true;micButton.setText("停止");micButton.setSelected(true);voice.start();}
        renderKeys();
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
        if(restarting&&Objects.equals(editorPackage,info.packageName)&&editorField==info.fieldId&&password==secure&&editorType==type)return;
        editorPackage=info.packageName;editorField=info.fieldId;editorType=type;
        if(voice!=null)voice.cancel();recording=false;code="";generation++;editing=false;symbols=false;voiceHistory.clear();password=secure;
        numbers=(type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_NUMBER || (type&InputType.TYPE_MASK_CLASS)==InputType.TYPE_CLASS_PHONE;
        selecting=false;selectionAnchor=-1;
        expandedCandidates=false;if(editorTools!=null)editorTools.setVisibility(View.GONE);
        if(draft!=null){draft.setText("");draft.setVisibility(View.GONE);root.findViewWithTag("draft-actions").setVisibility(View.GONE);micButton.setText("语音");}
        query();renderKeys();
    }
    @Override public void onFinishInputView(boolean finishingInput){stopDeleteRepeat();if(voice!=null)voice.cancel();recording=false;if(finishingInput)editing=false;super.onFinishInputView(finishingInput);}
    @Override public void onFinishInput(){voice.cancel();InputConnection ic=getCurrentInputConnection();if(english&&ic!=null)ic.finishComposingText();code="";generation++;super.onFinishInput();}
    @Override public boolean onEvaluateFullscreenMode(){return false;}
    @Override public void onUpdateSelection(int oldStart,int oldEnd,int newStart,int newEnd,int composingStart,int composingEnd){
        super.onUpdateSelection(oldStart,oldEnd,newStart,newEnd,composingStart,composingEnd);
        if(english&&!editing&&!code.isEmpty()&&(newStart!=oldStart||newEnd!=oldEnd)&&(newStart!=composingEnd||newEnd!=composingEnd)){
            code="";page=0;InputConnection ic=getCurrentInputConnection();if(ic!=null)ic.finishComposingText();query();
        }
        if(cursorPreview!=null&&cursorPreview.getVisibility()==View.VISIBLE)updateCursorPreview();
    }
    private void showEditor(){flush();selecting=false;selectionAnchor=-1;editorTools.setVisibility(editorTools.getVisibility()==View.VISIBLE?View.GONE:View.VISIBLE);updateCursorPreview();renderKeys();}
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
    @Override public void onDestroy(){stopDeleteRepeat();voice.close();lookup.shutdownNow();super.onDestroy();}
}
