// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
import {parentPort} from 'node:worker_threads';
import {createRequire} from 'node:module';
import {resolve,join} from 'node:path';
import {normalizeVoiceNumbers} from './voice-numbers.mjs';
import {punctuateVoice} from './voice-punctuation.mjs';
let recognizer;
try {
  const dir=resolve(import.meta.dirname,'..','models','sensevoice');
  const {OfflineRecognizer}=createRequire(import.meta.url)('sherpa-onnx-node');
  recognizer=new OfflineRecognizer({
    featConfig:{sampleRate:16000,featureDim:80},
    modelConfig:{senseVoice:{model:join(dir,'model.int8.onnx'),language:'auto',useInverseTextNormalization:true},
      tokens:join(dir,'tokens.txt'),numThreads:2,provider:'cpu',debug:0},
    decodingMethod:'greedy_search'
  });
  parentPort.postMessage({type:'status',ready:true});
}catch(e){parentPort.postMessage({type:'status',ready:false,error:e.message});}
parentPort.on('message',m=>{
  try {
    if(!recognizer)throw Error('整句复核模型未就绪');
    const samples=new Float32Array(m.samples);
    // Skip near-silence so the offline model cannot invent words from empty audio.
    let energy=0;for(const x of samples)energy+=x*x;
    let text=m.fallback;
    if(samples.length>=1600&&energy/samples.length>0.000003){
      const stream=recognizer.createStream();stream.acceptWaveform({samples,sampleRate:16000});
      recognizer.decode(stream);
      text=recognizer.getResult(stream).text.replace(/<\|[^|]*\|>/g,'').trim()||m.fallback;
      text=text.replace(/数萌\s*a\s*i\s*服务器/gi,'数萌AI服务器');
    }else text='';
    parentPort.postMessage({request:m.request,text:punctuateVoice(normalizeVoiceNumbers(text),m.sentenceEnd!==false)});
  }catch(e){parentPort.postMessage({request:m.request,text:punctuateVoice(normalizeVoiceNumbers(m.fallback),m.sentenceEnd!==false),error:e.message});}
});
