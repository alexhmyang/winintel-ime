import { parentPort,Worker } from 'node:worker_threads';
import { createRequire } from 'node:module';
import { readdirSync } from 'node:fs';
import { join, resolve } from 'node:path';
import {normalizeVoiceNumbers} from './voice-numbers.mjs';
const dir=resolve(import.meta.dirname,'..','models','voice');
let recognizer;
const sessions=new Map();
const pending=new Map();
let nextRequest=0,refinerReady=false;
const refiner=new Worker(new URL('./voice-refiner.mjs',import.meta.url));
refiner.on('message',m=>{
  if(m.type==='status'){
    refinerReady=m.ready;
    parentPort.postMessage({type:'status',ready:!!recognizer&&m.ready,error:m.error});
    return;
  }
  const request=pending.get(m.request);if(!request)return;
  pending.delete(m.request);
  const {id,session,segment,finished,start,end}=request;
  session.inflight--;
  if(sessions.get(id)!==session)return;
  if(m.error)parentPort.postMessage({id,type:'warning',text:'整句复核失败，保留实时结果'});
  parentPort.postMessage({id,type:'final',segment,text:m.text,finished,start,end});
  if(finished)sessions.delete(id);
});
refiner.on('error',e=>{
  refinerReady=false;parentPort.postMessage({type:'status',ready:false,error:e.message});
  for(const [request,m] of pending){
    if(sessions.get(m.id)===m.session)parentPort.postMessage({id:m.id,type:'error',error:'整句复核进程异常，请重新启动数萌语音'});
    pending.delete(request);
  }
});
try {
  const files=readdirSync(dir);
  const find=p=>join(dir,files.find(f=>f.startsWith(p)&&f.endsWith('.onnx')&&f.includes('int8')));
  const sherpa=createRequire(import.meta.url)('sherpa-onnx-node');
  recognizer=new sherpa.OnlineRecognizer({
    featConfig:{sampleRate:16000,featureDim:80},
    modelConfig:{transducer:{encoder:find('encoder'),decoder:find('decoder'),joiner:find('joiner')},tokens:join(dir,'tokens.txt'),numThreads:2,provider:'cpu',modelType:'zipformer',debug:0},
    decodingMethod:'modified_beam_search',maxActivePaths:4,hotwordsFile:join(dir,'hotwords.txt'),hotwordsScore:1.5,
    enableEndpoint:true,rule1MinTrailingSilence:2.4,rule2MinTrailingSilence:0.9,rule3MinUtteranceLength:20
  });
  if(refinerReady)parentPort.postMessage({type:'status',ready:true});
} catch(e) {parentPort.postMessage({type:'status',ready:false,error:e.message});}
parentPort.on('message',m=>{
  const {id,type}=m;
  try {
    if(!recognizer)throw Error('离线语音模型未就绪');
    if(type==='start'){if(sessions.size>=2)throw Error('已有语音会话');sessions.set(id,{stream:recognizer.createStream(),text:'',segment:1,chunks:[],length:0,inflight:0,finishing:false});parentPort.postMessage({id,type:'started'});return;}
    if(type==='close'){sessions.delete(id);return;}
    const session=sessions.get(id);if(!session)throw Error('语音会话未启动');
    if(type==='audio'||type==='finish'){
      if(session.finishing)return;
      const samples=type==='finish'?new Float32Array(12800):new Float32Array(m.samples);
      if(type==='audio'){
        if(!samples.length||samples.length>16384||!samples.every(Number.isFinite))throw Error('无效音频');
        session.chunks.push(samples);session.length+=samples.length;
      }
      session.stream.acceptWaveform({samples,sampleRate:16000});
      if(type==='finish')session.stream.inputFinished();
      while(recognizer.isReady(session.stream))recognizer.decode(session.stream);
      const text=normalizeVoiceNumbers(recognizer.getResult(session.stream).text.trim());
      const endpoint=type==='finish'||recognizer.isEndpoint(session.stream)||session.length>=320000;
      if(text!==session.text||endpoint){parentPort.postMessage({id,type:'partial',segment:session.segment,text});session.text=text;}
      if(endpoint){
        if(session.inflight>=4)throw Error('整句复核处理速度不足，请停止录音');
        const pcm=new Float32Array(session.length);let offset=0;
        for(const chunk of session.chunks){pcm.set(chunk,offset);offset+=chunk.length;}
        const request=++nextRequest;session.inflight++;
        const start=(session.elapsed||0)/16000,end=start+session.length/16000;
        session.elapsed=(session.elapsed||0)+session.length;
        pending.set(request,{id,session,segment:session.segment,finished:type==='finish',start,end});
        refiner.postMessage({request,samples:pcm.buffer,fallback:text},[pcm.buffer]);
        session.segment++;session.chunks=[];session.length=0;recognizer.reset(session.stream);session.text='';
      }
      if(type==='finish')session.finishing=true;
    }
  } catch(e){parentPort.postMessage({id,type:'error',error:e.message});}
});
