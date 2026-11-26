import {Worker} from 'node:worker_threads';
import {createRequire} from 'node:module';
import {join,resolve} from 'node:path';
import {readFileSync,writeFileSync} from 'node:fs';
const [assets,fixtures,report,mode]=process.argv.slice(2);
const sherpa=createRequire(join(resolve(assets),'standalone','voice-host.mjs'))('sherpa-onnx-node');
const worker=new Worker(join(resolve(assets),'standalone','voice-worker.mjs'));
const timeout=setTimeout(()=>{console.error('worker timeout');worker.terminate();process.exitCode=1;},45000);
const expected=JSON.parse(readFileSync(join(fixtures,'fixtures.json'),'utf8').replace(/^\uFEFF/,''));
let index=0,id='',rows=[],finals=[],ids=new Set(),ready=false,observations=[];
worker.on('error',fail);
function fail(e){console.error(e);clearTimeout(timeout);worker.terminate();process.exitCode=1;}
function start(){id='case-'+index;finals=[];ids=new Set();worker.postMessage({id,type:'start'});}
worker.on('message',m=>{
  if(m.type==='status'){if(!m.ready)return fail(m.error);ready=true;start();return;}
  if(m.type==='error')return fail(m.error);
  if(m.id!==id)return;
  if(m.type==='started'){
    const wav=index<expected.length?sherpa.readWave(join(fixtures,`tts-${index}.wav`)):{samples:new Float32Array(16000),sampleRate:16000};
    // Audio is sent through the same 100 ms chunk path as microphone capture.
    const samples=wav.samples;
    for(let i=0;i<samples.length;i+=1600){const chunk=samples.slice(i,i+1600);worker.postMessage({id,type:'audio',samples:chunk.buffer},[chunk.buffer]);}
    // Force a real endpoint followed by finish: final text must not be duplicated.
    for(let i=0;i<15;i++)worker.postMessage({id,type:'audio',samples:new Float32Array(1600).buffer});
    worker.postMessage({id,type:'finish'});return;
  }
  if(m.type==='final'){
    if(ids.has(m.segment))return fail('Duplicate segment ID');ids.add(m.segment);
    if(m.text)finals.push(m.text);
    if(!m.finished)return;
    const text=finals.join(' '),clean=s=>s.toLowerCase().replace(/[\s\p{P}]/gu,'');
    const expectedText=expected[index]||'';
    if(mode==='--technical-fields')observations.push({expected:expectedText,segments:[...finals]});
    const hardware=s=>JSON.stringify(s.match(/\b(?:V100|A100|H100|H200|A800) \d+(?:GB|G)|\d+(?:GB|G|张)/g)||[]);
    const technical=s=>JSON.stringify(s.match(/\b(?:V100 \d+G|SXM[2345]|NVLink|winintel\.com|\d+Gbps|\d+ bits)\b/g)||[]);
    const ok=mode==='--technical-fields'?technical(text)===technical(expectedText):mode==='--hardware-fields'?hardware(text)===hardware(expectedText):clean(text)===clean(expectedText);
    rows.push(`| ${expectedText||'纯静音'} | ${text||'（空）'} | ${ok?'通过':'失败'} |`);
    if(!ok&&mode!=='--technical-fields')return fail(rows.join('\n'));
    index++;
    if(index<=expected.length)start();
    else{const output=(mode==='--technical-fields'?'本表记录拆字母合成语音的实际识别；不匹配保留完整输出，不代表真人准确率。草稿在合并分段后另做技术词规范化，见 Voice-acronym-draft.md。\n\n':mode==='--hardware-fields'?'本表检查型号、容量、数字及型号容量间空格；完整文字列保留其他识别误差，结果不代表全句准确率。\n\n':'')+'100 ms 音频分块 → 实时识别 → 停顿分段 → 整句复核 → 结束会话；全部为合成音频。\n\n| 预期 | 工作线程完整结果 | '+(mode==='--hardware-fields'?'数字字段验证':'结果')+' |\n|---|---|---|\n'+rows.join('\n')+'\n';writeFileSync(report,output);if(mode==='--technical-fields')writeFileSync(report+'.segments.json',JSON.stringify(observations));console.log(output);clearTimeout(timeout);worker.terminate();}
  }
});
