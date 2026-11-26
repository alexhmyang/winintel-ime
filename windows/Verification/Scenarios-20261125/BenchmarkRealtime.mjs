import {Worker} from 'node:worker_threads';
import {readFileSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {pathToFileURL} from 'node:url';
import {createRequire} from 'node:module';
import {performance} from 'node:perf_hooks';
import {spawn} from 'node:child_process';
import {createInterface} from 'node:readline';
const root=process.env.WININTEL_BENCH_ROOT||'F:/Temp/winintel-ime-model-upgrade',assets=join(root,'meeting-build/payload');
const sherpa=createRequire(join(assets,'standalone/voice-host.mjs'))('sherpa-onnx-node');
// Match the desktop architecture: the two native ONNX runtimes need separate processes.
const translationProcess=spawn(process.execPath,[join(assets,'standalone/translation-host.mjs')],{stdio:['pipe','pipe','pipe']});
translationProcess.stderr.on('data',()=>{});
const responses=new Map();let requestId=0;
createInterface({input:translationProcess.stdout}).on('line',line=>{
  const response=JSON.parse(line),pending=responses.get(response.id);
  if(!pending)return;responses.delete(response.id);
  if(response.error)pending.reject(Error(response.error));else pending.resolve(response);
});
function translate(text,direction){
  return new Promise((resolve,reject)=>{
    const id=++requestId;responses.set(id,{resolve,reject});
    translationProcess.stdin.write(JSON.stringify({id,text,direction})+'\n');
  });
}
translationProcess.on('exit',code=>{for(const promise of responses.values())promise.reject(Error('Translation exited: '+code));responses.clear();});
await translate('你好。','zh-en');await translate('Hello.','en-zh');
const fixtures=JSON.parse(readFileSync(join(root,'scenario-audio/fixtures.json'),'utf8').replace(/^\uFEFF/,''));
const worker=new Worker(pathToFileURL(join(assets,'standalone/voice-worker.mjs')));
await new Promise((resolve,reject)=>{worker.on('error',reject);worker.on('message',m=>{if(m.type==='status'&&m.ready)resolve();});});
const rows=[];
for(const fixture of fixtures.filter(s=>['0-zh','0-en','4-zh','4-en','8-zh','8-en','18-zh','18-en'].includes(s.id))){
  const wave=sherpa.readWave(fixture.file),id=fixture.id,direction=fixture.lang==='zh'?'zh-en':'en-zh';
  const start=performance.now();let firstPartialMs=null,lastFinalMs=0,finishSentMs=0,firstTranslationMs=null;
  const translated=[],finals=[];let chain=Promise.resolve();
  await new Promise((resolve,reject)=>{
    const timeout=setTimeout(()=>reject(Error('Realtime test timeout')),90000);
    async function listener(m){
      if(m.id!==id)return;
      if(m.type==='partial'&&m.text&&firstPartialMs===null)firstPartialMs=performance.now()-start;
      if(m.type==='started'){
        for(let i=0;i<wave.samples.length;i+=1600){
          worker.postMessage({id,type:'audio',samples:wave.samples.slice(i,i+1600).buffer});
          const due=start+(i+1600)/16;
          const delay=due-performance.now();if(delay>0)await new Promise(r=>setTimeout(r,delay));
        }
        finishSentMs=performance.now()-start;worker.postMessage({id,type:'finish'});
      }
      if(m.type==='error'){clearTimeout(timeout);worker.off('message',listener);reject(Error(m.error));}
      if(m.type==='final'){
        lastFinalMs=performance.now()-start;
        if(m.text){
          finals.push({text:m.text,start:m.start,end:m.end,receivedMs:lastFinalMs});
          chain=chain.then(async()=>{
            const result=await translate(m.text,direction);
            const arrived=performance.now()-start;
            if(firstTranslationMs===null)firstTranslationMs=arrived;
            translated.push({text:result.text,arrivedMs:arrived,ms:result.ms});
          });
        }
        if(m.finished){
          await chain;clearTimeout(timeout);worker.off('message',listener);
          const end=performance.now()-start;
          rows.push({id,group:fixture.group,source:fixture.text,seconds:wave.samples.length/wave.sampleRate,feedWallMs:finishSentMs,
            firstPartialMs,firstTranslationMs,finalRecognitionLagMs:Math.max(0,lastFinalMs-finishSentMs),
            finalTranslationLagMs:Math.max(0,end-finishSentMs),finals,translated});
          console.log(`${id} first=${firstPartialMs?.toFixed(0)}ms ASRtail=${rows.at(-1).finalRecognitionLagMs.toFixed(0)}ms translationTail=${rows.at(-1).finalTranslationLagMs.toFixed(0)}ms`);
          resolve();
        }
      }
    }
    worker.on('message',listener);worker.postMessage({id,type:'start'});
  });
}
await worker.terminate();
translationProcess.kill();
writeFileSync(join(root,'scenario-realtime.json'),JSON.stringify({rows,memory:process.memoryUsage()},null,2));
