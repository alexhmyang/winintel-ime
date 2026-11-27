// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
import {pipeline,env} from '@huggingface/transformers';
import {resolve} from 'node:path';
import {performance} from 'node:perf_hooks';
import {normalizeVoiceNumbers} from './voice-numbers.mjs';
import {protect,canonicalize} from './translation-glossary.mjs';
env.allowRemoteModels=false;
env.allowLocalModels=true;
env.useBrowserCache=false;
env.useFSCache=false;
const root=resolve(import.meta.dirname,'..','models','translation');
const translators=new Map();
export async function translate(text,direction='auto',useGlossary=true){
  text=String(text||'').trim();
  if(!text)return {text:'',ms:0,loadMs:0,direction};
  if(text.length>4000)throw Error('单次翻译最多 4000 字，请分段处理');
  if(direction==='auto')direction=/[\u3400-\u9fff]/.test(text)?'zh-en':'en-zh';
  if(!['zh-en','en-zh'].includes(direction))throw Error('请选择中译英或英译中');
  let loadMs=0;
  if(!translators.has(direction)){
    const start=performance.now();
    const translator=await pipeline('translation',resolve(root,'opus-mt-'+direction),{
      device:'cpu',dtype:'q8',session_options:{intraOpNumThreads:2,interOpNumThreads:1,executionMode:'sequential'}
    });
    translators.set(direction,translator);loadMs=performance.now()-start;
  }
  // Short chunks keep CPU latency and decoder memory bounded.
  const chunks=text.split(/(?<=[。！？!?])\s*|(?<=\.)\s+(?=[A-Z])|\n+/u).filter(s=>s.trim());
  const bounded=chunks.flatMap(s=>{
    const result=[];
    while(s.length>180){
      let cut=s.lastIndexOf(' ',180);
      if(cut<90)cut=180;
      result.push(s.slice(0,cut));s=s.slice(cut).trimStart();
    }
    if(s.trim())result.push(s);
    return result;
  }),output=[];
  const started=performance.now();let fallbacks=0;
  const run=async value=>{
    if(!value.trim())return '';
    if(/^[\s\p{P}\p{S}]+$/u.test(value))return direction==='zh-en'?value.replace(/[，。！？：、]/g,c=>({'，':',','。':'.','！':'!','？':'?','：':':','、':','}[c])):value;
    const input=(direction==='en-zh'?'>>cmn_Hans<< ':'')+value.trim();
    const result=await translators.get(direction)(input,{max_new_tokens:256,num_beams:1});
    return result[0].translation_text;
  };
  for(const chunk of bounded){
    const protectedText=useGlossary?protect(chunk,direction):{text:chunk,slots:[]};
    const plain=await run(protectedText.text);
    const canonical=canonicalize(normalizeVoiceNumbers(plain),direction,chunk).replace(/(?<=\d),(?=\d{3}\b)/g,'');
    const desired=new Map();
    for(const slot of protectedText.slots)desired.set(slot.target,(desired.get(slot.target)||0)+1);
    const patterns=[...desired].map(([term,count])=>{
      const escaped=term.replace(/[.*+?^${}()|[\]\\]/g,'\\$&').replace(/\s+/g,'\\s*');
      return {term,count,pattern:new RegExp(escaped,/(?:Gbps|GBps)$/.test(term)?'g':'gi')};
    });
    if(patterns.every(({pattern,count})=>(canonical.match(pattern)||[]).length>=count)){
      let result=canonical;
      for(const {pattern,term} of patterns)result=result.replace(pattern,()=>term);
      output.push(result);continue;
    }
    // Never silently lose a protected model number, currency or company name.
    fallbacks++;const pieces=[];let offset=0;
    for(const slot of protectedText.slots){
      const fragment=protectedText.text.slice(offset,slot.offset);
      // Translating isolated English function words can hallucinate entire sentences.
      // Keep their connective meaning without invoking a model on a one-word fragment.
      const connectors={of:'的',on:'上',in:'中',with:'配有',and:'和',the:'',a:'',an:'',using:'使用',to:'以'};
      const connector=direction==='en-zh'?connectors[fragment.trim().toLowerCase()]:undefined;
      pieces.push(connector===undefined?await run(fragment):connector,slot.target);
      offset=slot.offset+slot.value.length;
    }
    pieces.push(await run(protectedText.text.slice(offset)));
    output.push(pieces.join(direction==='zh-en'?' ':''));
  }
  const raw=output.join(direction==='zh-en'?' ':'').trim().replace(/ {2,}/g,' ').replace(/\s+([,.!?;:])/g,'$1');
  return {text:normalizeVoiceNumbers(raw),raw,ms:performance.now()-started,loadMs,direction,fallbacks};
}
