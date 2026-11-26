import {readFileSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {pathToFileURL} from 'node:url';
import {performance} from 'node:perf_hooks';
const root=process.env.WININTEL_BENCH_ROOT||'F:/Temp/winintel-ime-model-upgrade';
const {translate}=await import(pathToFileURL(join(root,'meeting-build/payload/standalone/translation-engine.mjs')));
const {normalizeVoiceNumbers}=await import(pathToFileURL(join(root,'meeting-build/payload/standalone/voice-numbers.mjs')));
const scenarios=JSON.parse(readFileSync(join(root,'scenarios.json'),'utf8'));
function grams(s,n){
  s=s.toLowerCase().replace(/\s/g,'');const map=new Map();
  for(let i=0;i+n<=s.length;i++){const gram=s.slice(i,i+n);map.set(gram,(map.get(gram)||0)+1);}
  return map;
}
function chrf(reference,hypothesis){
  let p=0,r=0,count=0;
  for(let n=1;n<=6;n++){
    const a=grams(reference,n),b=grams(hypothesis,n);
    if(!a.size||!b.size)continue;
    let matches=0;for(const [gram,value] of a)matches+=Math.min(value,b.get(gram)||0);
    p+=matches/[...b.values()].reduce((x,y)=>x+y,0);r+=matches/[...a.values()].reduce((x,y)=>x+y,0);count++;
  }
  p/=count;r/=count;return p+r?5*p*r/(4*p+r)*100:0;
}
const rows=[];
for(const [i,sample] of scenarios.entries()){
  for(const direction of ['zh-en','en-zh']){
    const source=direction==='zh-en'?sample.zh:sample.en,reference=direction==='zh-en'?sample.en:sample.zh;
    const runs=[],total=[];
    let first;
    for(let repeat=0;repeat<3;repeat++){
      const start=performance.now(),result=await translate(source,direction);
      if(!first)first=result;
      runs.push(result.ms);total.push(performance.now()-start);
    }
    rows.push({id:i+'-'+direction,group:sample.group,direction,source,reference,output:first.text,raw:first.raw,chrf:chrf(normalizeVoiceNumbers(reference),first.text),firstLoadMs:first.loadMs,
      firstInferenceMs:runs[0],warmMs:(runs[1]+runs[2])/2,runs,total});
    console.log(`${i} ${direction} ${first.text} ${(runs[1]+runs[2])/2|0}ms`);
  }
}
writeFileSync(join(root,'scenario-translation.json'),JSON.stringify({rows,memory:process.memoryUsage()},null,2));
