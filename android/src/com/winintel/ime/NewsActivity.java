// SPDX-License-Identifier: GPL-3.0-or-later
package com.winintel.ime;
import android.app.Activity;
import android.content.*;
import android.os.*;
import android.util.AtomicFile;
import android.view.*;
import android.widget.*;
import org.json.*;
import java.io.*;
import java.util.concurrent.*;

public final class NewsActivity extends Activity {
    private final ExecutorService worker=Executors.newSingleThreadExecutor();
    private final Handler ui=new Handler(Looper.getMainLooper());
    private SharedPreferences prefs;
    private LinearLayout list;
    private TextView status;
    private EditText endpoint;
    private CheckBox enabled,fallback;
    private JSONArray items=new JSONArray();
    private boolean busy,closed;
    private AtomicFile cache;
    private final Runnable refreshTimer=new Runnable(){public void run(){refresh(false);ui.postDelayed(this,300000);}};
    @Override public void onCreate(Bundle state){
        super.onCreate(state);prefs=getSharedPreferences("news",0);
        cache=new AtomicFile(new File(getFilesDir(),"news-cache.json"));
        getWindow().getDecorView().setOnApplyWindowInsetsListener((v,insets)->{v.setPadding(insets.getSystemWindowInsetLeft(),insets.getSystemWindowInsetTop(),insets.getSystemWindowInsetRight(),insets.getSystemWindowInsetBottom());return insets;});
        ScrollView scroll=new ScrollView(this);LinearLayout root=Ui.column(this);root.setPadding(16,16,16,16);root.setBackgroundColor(Ui.BG);scroll.addView(root);setContentView(scroll);
        root.addView(Ui.text(this,"实时新闻与双语热词",25));
        root.addView(Ui.text(this,"免费源：IT之家 / NPR。开启后在此页面每 5 分钟刷新，关闭页面即停止定时请求。输入、音频和选词记录不会上传。",14));
        enabled=new CheckBox(this);enabled.setText("启用实时新闻（默认关闭）");enabled.setChecked(prefs.getBoolean("enabled",false));root.addView(enabled);
        endpoint=new EditText(this);endpoint.setSingleLine(true);endpoint.setInputType(android.text.InputType.TYPE_CLASS_TEXT|android.text.InputType.TYPE_TEXT_VARIATION_URI);
        endpoint.setHint("自有 HTTPS 接口，例如 https://你的域名/v1/news");endpoint.setText(prefs.getString("endpoint",""));root.addView(endpoint);
        fallback=new CheckBox(this);fallback.setText("自有接口失败时回退免费源");fallback.setChecked(prefs.getBoolean("fallback",true));root.addView(fallback);
        Button save=Ui.button(this,"保存并刷新",v->{
            String address=endpoint.getText().toString().trim();
            if(!address.isEmpty()&&!NewsFeed.validUrl(address)){status.setText("自有接口必须使用 HTTPS");return;}
            prefs.edit().putBoolean("enabled",enabled.isChecked()).putString("endpoint",address).putBoolean("fallback",fallback.isChecked()).apply();refresh(true);
        });root.addView(save,new LinearLayout.LayoutParams(-1,Ui.dp(this,48)));
        status=Ui.text(this,"离线缓存",14);root.addView(status);list=Ui.column(this);root.addView(list);
        try{JSONObject saved=new JSONObject(new String(cache.readFully(),"UTF-8"));items=NewsFeed.parseJson(saved.toString());status.setText("缓存下载时间："+saved.optString("downloaded_at"));}catch(Exception ignored){}
        showItems();
        try{
            StringBuilder hot=new StringBuilder("本地精选热词 · 2026-11-26\n");
            try(BufferedReader reader=new BufferedReader(new InputStreamReader(getAssets().open("hot-chinese.tsv"),"UTF-8"))){String line;while((line=reader.readLine())!=null)hot.append(line.split("\t")[1]).append("  ");}
            hot.append("\nDeepSeek · NVLink · InfiniBand · vLLM · agentic · multimodal\n英文补全与中文热词无需联网。");
            root.addView(Ui.text(this,hot.toString(),15));
        }catch(Exception ignored){}
    }
    private void showItems(){
        list.removeAllViews();
        if(items.length()==0)list.addView(Ui.text(this,"暂无缓存。开启新闻后点击保存并刷新。",15));
        for(int i=0;i<items.length();i++){
            JSONObject item=items.optJSONObject(i);if(item==null)continue;
            TextView row=Ui.text(this,item.optString("title")+"\n"+item.optString("source")+" · "+item.optString("published_at"),16);
            row.setOnClickListener(v->{String url=item.optString("url");if(NewsFeed.validUrl(url))try{startActivity(new Intent(Intent.ACTION_VIEW,android.net.Uri.parse(url)));}catch(Exception e){status.setText("没有可用浏览器");}});
            list.addView(row);
        }
    }
    private void refresh(boolean force){
        if(closed||busy)return;
        if(!prefs.getBoolean("enabled",false)){status.setText("新闻已关闭 · 本地词库和缓存仍可使用");return;}
        if(!force&&System.currentTimeMillis()-prefs.getLong("downloaded",0)<300000)return;
        busy=true;status.setText("正在获取新闻…");
        final String address=prefs.getString("endpoint","");final boolean allowFallback=prefs.getBoolean("fallback",true);
        worker.execute(()->{
            try{
                JSONArray rows;StringBuilder message=new StringBuilder();
                if(address.isEmpty()){message.append("免费实时源");rows=NewsFeed.freeSources(message);}
                else try{rows=NewsFeed.parseJson(NewsFeed.download(address));message.append("自有新闻接口");}
                catch(Exception e){if(!allowFallback)throw e;message.append("自有接口失败，已回退免费源");rows=NewsFeed.freeSources(message);}
                if(closed)return;
                String date=java.time.Instant.now().toString();
                JSONObject pack=new JSONObject().put("schema",1).put("items",rows).put("downloaded_at",date);
                FileOutputStream out=null;
                try{out=cache.startWrite();out.write(pack.toString().getBytes("UTF-8"));cache.finishWrite(out);}catch(Exception e){if(out!=null)cache.failWrite(out);throw e;}
                prefs.edit().putLong("downloaded",System.currentTimeMillis()).apply();
                if(rows.length()>0&&java.time.Instant.parse(rows.getJSONObject(0).getString("published_at")).isBefore(java.time.Instant.now().minusSeconds(172800)))message.append(" · 来源超过 48 小时无新条目");
                final String detail=message+" · "+rows.length()+" 条\n下载时间："+date;final JSONArray fresh=rows;
                ui.post(()->{if(!closed){items=fresh;status.setText(detail);showItems();}});
            }catch(Exception e){ui.post(()->{if(!closed)status.setText("刷新失败，保留缓存："+e.getMessage());});}
            finally{ui.post(()->busy=false);}
        });
    }
    @Override protected void onResume(){super.onResume();ui.post(refreshTimer);}
    @Override protected void onPause(){ui.removeCallbacks(refreshTimer);super.onPause();}
    @Override protected void onDestroy(){closed=true;ui.removeCallbacksAndMessages(null);worker.shutdownNow();super.onDestroy();}
}
