import {createRequire} from 'node:module';
import {Worker} from 'node:worker_threads';
import {readFileSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {performance} from 'node:perf_hooks';
import {pathToFileURL} from 'node:url';
const root=process.env.WININTEL_BENCH_ROOT||'F:/Temp/winintel-ime-model-upgrade';
const assets=join(root,'meeting-build/payload');
const sherpa=createRequire(join(assets,'standalone/voice-host.mjs'))('sherpa-onnx-node');
const {normalizeVoiceNumbers}=await import(pathToFileURL(join(assets,'standalone/voice-numbers.mjs')));
const normalize=text=>normalizeVoiceNumbers(text).toLowerCase().replace(/[\s\p{P}]/gu,'');
export function distance(a,b){
  let row=Array.from({length:b.length+1},(_,i)=>i);
  for(let i=0;i<a.length;i++){
    const next=[i+1];
    for(let j=0;j<b.length;j++)next.push(Math.min(next[j]+1,row[j+1]+1,row[j]+(a[i]===b[j]?0:1)));
    row=next;
  }
  return row[b.length];
}
const fixtures=JSON.parse(readFileSync(join(root,'scenario-audio/fixtures.json'),'utf8').replace(/^\uFEFF/,''));
const samples=fixtures.map(f=>({...f,condition:'干净',wave:sherpa.readWave(f.file)}));
let seed=20261125;
for(const index of [0,1,8,9,16,17,24,25,32,33,36,37,40,41,46,47]){
  const s=samples[index],audio=s.wave.samples;
  const rms=Math.sqrt(audio.reduce((sum,v)=>sum+v*v,0)/audio.length);
  const amplitude=rms/Math.pow(10,12/20)*Math.sqrt(3);
  const noisy=Float32Array.from(audio,v=>{
    seed=(Math.imul(1664525,seed)+1013904223)>>>0;
    return Math.max(-1,Math.min(1,v+(seed/4294967296*2-1)*amplitude));
  });
  samples.push({...s,id:s.id+'-noise',condition:'12dB白噪声',wave:{samples:noisy,sampleRate:16000}});
}
const name=process.argv[2]||'sense';
let recognizer,worker,loadMs;
if(name==='stream'){
  const start=performance.now();
  worker=new Worker(pathToFileURL(join(assets,'standalone/voice-worker.mjs')));
  await new Promise((resolve,reject)=>{
    const timer=setTimeout(()=>reject(Error('Streaming load timeout')),30000);
    worker.on('error',reject);
    worker.on('message',m=>{if(m.type==='status'){if(m.ready){clearTimeout(timer);resolve();}else if(m.error){clearTimeout(timer);reject(Error(m.error));}}});
  });
  loadMs=performance.now()-start;
}else{
  const config=name==='qwen'?{
    numThreads:4,provider:'cpu',qwen3Asr:{
      convFrontend:join(root,'sherpa-onnx-qwen3-asr-0.6B-int8-2026-03-25/conv_frontend.onnx'),
      encoder:join(root,'sherpa-onnx-qwen3-asr-0.6B-int8-2026-03-25/encoder.int8.onnx'),
      decoder:join(root,'sherpa-onnx-qwen3-asr-0.6B-int8-2026-03-25/decoder.int8.onnx'),
      tokenizer:join(root,'sherpa-onnx-qwen3-asr-0.6B-int8-2026-03-25/tokenizer'),maxNewTokens:256
    },tokens:''
  }:{numThreads:2,provider:'cpu',senseVoice:{model:join(assets,'models/sensevoice/model.int8.onnx'),language:'auto',useInverseTextNormalization:true},tokens:join(assets,'models/sensevoice/tokens.txt')};
  const start=performance.now();
  recognizer=new sherpa.OfflineRecognizer({featConfig:{sampleRate:16000,featureDim:80},modelConfig:config});
  loadMs=performance.now()-start;
}
let next=0;
async function recognize(sample){
  const start=performance.now();
  if(recognizer){
    const stream=recognizer.createStream();stream.acceptWaveform(sample.wave);recognizer.decode(stream);
    return {raw:recognizer.getResult(stream).text.replace(/<\|[^|]*\|>/g,'').trim(),ms:performance.now()-start};
  }
  const id=String(++next),finals=[],partials=[];
  return await new Promise((resolve,reject)=>{
    const timer=setTimeout(()=>{worker.off('message',listener);reject(Error('Streaming timeout'));},60000);
    function listener(m){
      if(m.id!==id)return;
      if(m.type==='started'){
        for(let i=0;i<sample.wave.samples.length;i+=1600)worker.postMessage({id,type:'audio',samples:sample.wave.samples.slice(i,i+1600).buffer});
        worker.postMessage({id,type:'finish'});
      }
      if(m.type==='partial'&&m.text)partials.push({text:m.text,ms:performance.now()-start});
      if(m.type==='error'){clearTimeout(timer);worker.off('message',listener);reject(Error(m.error));}
      if(m.type==='final'){
        finals.push({text:m.text,start:m.start,end:m.end});
        if(m.finished){clearTimeout(timer);worker.off('message',listener);resolve({raw:finals.map(s=>s.text).filter(Boolean).join(' '),ms:performance.now()-start,firstPartialMs:partials[0]?.ms,finals});}
      }
    }
    worker.on('message',listener);worker.postMessage({id,type:'start'});
  });
}
await recognize(samples[0]); // warm-up excluded
const rows=[];
for(const sample of samples){
  const runs=[];
  for(let repeat=0;repeat<2;repeat++)runs.push(await recognize(sample));
  const formatted=normalizeVoiceNumbers(runs[0].raw);
  const expected=normalize(sample.text),hypothesis=normalize(formatted);
  const wordClean=s=>normalizeVoiceNumbers(s).toLowerCase().replace(/\p{P}/gu,' ').trim().split(/\s+/);
  const ms=runs.map(r=>r.ms),seconds=sample.wave.samples.length/sample.wave.sampleRate;
  rows.push({id:sample.id,group:sample.group,lang:sample.lang,condition:sample.condition,expected:sample.text,raw:runs[0].raw,formatted,errors:distance(expected,hypothesis),chars:expected.length,
    wordErrors:sample.lang==='en'?distance(wordClean(sample.text),wordClean(formatted)):null,words:sample.lang==='en'?wordClean(sample.text).length:null,
    seconds,ms:ms.reduce((a,b)=>a+b)/ms.length,runs:ms,rtf:ms.reduce((a,b)=>a+b)/ms.length/1000/seconds,
    firstPartialMs:runs[0].firstPartialMs,finals:runs[0].finals,consistent:runs[0].raw===runs[1].raw});
  console.log(`${name} ${sample.id} ${formatted} ${rows.at(-1).ms.toFixed(0)}ms`);
}
if(worker)await worker.terminate();
writeFileSync(join(root,`scenario-asr-${name}.json`),JSON.stringify({name,loadMs,rows,memory:process.memoryUsage()},null,2));
