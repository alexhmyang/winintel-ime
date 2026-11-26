// SPDX-License-Identifier: GPL-3.0-or-later
import {normalizeVoiceNumbers} from './voice-numbers.mjs';
const terms={
  deepseek:['DeepSeek','DeepSeek'],vllm:['vLLM','vLLM'],windows:['Windows','Windows'],
  nvlink:['NVLink','NVLink'],infiniband:['InfiniBand','InfiniBand'],cpu:['CPU','CPU'],gpu:['GPU','GPU'],
  'api key':['API key','API 密钥'],api:['API','API'],microphone:['microphone','麦克风'],
  benchmark:['benchmark','基准测试'],latency:['latency','延迟'],throughput:['throughput','吞吐量'],
  offline:['offline','离线'],audio:['audio','音频'],'winintel.com':['winintel.com','winintel.com'],
  'winintel ai input method':['winintel AI input method','数萌 AI 输入法'],
  'winintel ai':['winintel AI','数萌 AI'],
  '显存':['VRAM','显存'],'显卡':['graphics card','显卡'],
  '并发性能':['concurrent performance','并发性能'],
  'concurrent performance':['concurrent performance','并发性能'],
  'graphics cards':['graphics cards','显卡'],'graphics card':['graphics card','显卡'],
  'vram':['VRAM','显存'],'memory':['memory','内存']
};
for(const [i,day] of ['Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday'].entries()){
  const chinese='下周'+'一二三四五六日'[i];
  terms[chinese]=['next '+day,chinese];
  terms['next '+day.toLowerCase()]=['next '+day,chinese];
}
const known=Object.keys(terms).sort((a,b)=>b.length-a.length).map(s=>s.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')).join('|');
const pattern=new RegExp(`数萌\\s*AI(?:\\s*输入法)?|(?<![A-Za-z0-9])(?:${known}|C\\s*P\\s*U|G\\s*P\\s*U|SXM[2345]|[VAH]\\d{3})(?![A-Za-z0-9])|(?:[$¥€£]\\s*[+-]?\\d[\\d,]*(?:\\.\\d+)?|[+-]?\\d[\\d,]*(?:\\.\\d+)?\\s*(?:人民币|美元|欧元|英镑|元|yuan|USD|RMB|CNY|dollars?))(?![A-Za-z])|[+-]?\\d+(?:\\.\\d+)?\\s*(?:Gbps|GBps|GHz|MHz|GB|TB|MB|G)(?![A-Za-z])`,'giu');
function target(value,direction){
  if(/^数萌/.test(value))return direction==='zh-en'?(value.includes('输入法')?'winintel AI input method':'winintel AI'):value;
  const term=terms[value.toLowerCase()];
  if(term)return term[direction==='zh-en'?0:1];
  if(/^[CG]\s*P\s*U$/i.test(value))return value.replace(/\s/g,'').toUpperCase();
  if(/^[VAH]\d{3}$/i.test(value)||/^SXM[2345]$/i.test(value))return value.toUpperCase();
  const currency=value.match(/([$¥€£])?\s*([+-]?\d[\d,]*(?:\.\d+)?)\s*(人民币|美元|欧元|英镑|元|yuan|USD|RMB|CNY|dollars?)?$/i);
  if(currency){
    const number=currency[2].replace(/,/g,''),unit=(currency[1]||currency[3]||'').toLowerCase();
    const names=unit==='$'||/美元|usd|dollar/.test(unit)?['US dollars','美元']:
      unit==='€'||unit==='欧元'?['euros','欧元']:unit==='£'||unit==='英镑'?['British pounds','英镑']:['yuan','元'];
    return number+(direction==='zh-en'?' '+names[0]:names[1]);
  }
  return normalizeVoiceNumbers(value);
}
export function protect(text,direction){
  text=normalizeVoiceNumbers(text);
  const hardwareMemory=/graphics\s+cards?|\bGPU\b|\b[VAH]\d{3}\b|\bVRAM\b/i.test(text)?'显存':/\bCPU\b|server|\bRAM\b/i.test(text)?'内存':'';
  const slots=[...text.matchAll(pattern)].filter(match=>match[0].toLowerCase()!=='memory'||hardwareMemory).map(match=>({
    value:match[0],target:match[0].toLowerCase()==='memory'&&direction==='en-zh'?hardwareMemory:target(match[0],direction),offset:match.index
  }));
  return {text,slots};
}
// Common translations are equivalent spellings, not reasons to split a sentence.
export function canonicalize(text,direction,source){
  if(direction==='en-zh'){
    text=text.replace(/脱机/g,'离线').replace(/API\s*键/gi,'API 密钥')
      .replace(/图形卡|图形显卡|视频卡/g,'显卡').replace(/同时性能/g,'并发性能')
      .replace(/延时/g,'延迟');
    if(/graphics\s+cards?|\bGPU\b|\b[VAH]\d{3}\b|\bVRAM\b/i.test(source))text=text.replace(/内存/g,'显存');
    text=text.replace(/(\d+(?:\.\d+)?)千兆位\/秒/g,'$1Gbps');
  }
  return text;
}
