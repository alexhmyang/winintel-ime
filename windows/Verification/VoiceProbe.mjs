// SPDX-License-Identifier: GPL-3.0-or-later
import { Worker } from 'node:worker_threads';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
const root=resolve(process.argv[2]);
const worker=new Worker(join(root,'standalone','voice-worker.mjs'));
const cases=['0.wav','1.wav','2.wav'];
const results=[];
let current=0,started=0,combined='';
const deadline=setTimeout(()=>{console.error('Voice verification timeout');worker.terminate();process.exitCode=1;},45000);
function send(){
  const wav=readFileSync(join(root,'models','voice','test_wavs',cases[current]));
  let offset=12,pcm,rate=0;
  while(offset+8<=wav.length){const size=wav.readUInt32LE(offset+4),type=wav.toString('ascii',offset,offset+4);if(type==='fmt ')rate=wav.readUInt32LE(offset+12);if(type==='data')pcm=wav.subarray(offset+8,offset+8+size);offset+=8+size+(size%2);}
  if(!pcm||rate!==16000)throw Error('Expected 16 kHz fixture');
  const samples=new Float32Array(pcm.length/2);for(let i=0;i<samples.length;i++)samples[i]=pcm.readInt16LE(i*2)/32768;
  started=performance.now();combined='';
  for(let i=0;i<samples.length;i+=1600)worker.postMessage({id:'fixture',type:'audio',samples:samples.slice(i,i+1600).buffer});
  worker.postMessage({id:'fixture',type:'finish'});
}
worker.on('message',m=>{
  if(m.type==='status'){if(!m.ready)throw Error(m.error);worker.postMessage({id:'fixture',type:'start'});}
  else if(m.type==='started')send();
  else if(m.type==='error')throw Error(m.error);
  else if(m.type==='final'){
    combined+=(combined?' ':'')+m.text;
    if(m.finished){
      const elapsed=performance.now()-started;results.push({file:cases[current],text:combined,ms:elapsed});
      if(++current<cases.length)worker.postMessage({id:'fixture',type:'start'});
      else{clearTimeout(deadline);worker.terminate();const table='| 音频 | 识别文本 | 解码耗时 ms | 结果 |\n|---|---|---:|---|\n'+results.map(r=>`| ${r.file} | ${r.text.replace(/\|/g,' ')} | ${r.ms.toFixed(0)} | ${r.text.trim()?'通过':'失败'} |`).join('\n')+'\n';writeFileSync(process.argv[3],table);console.log(table);if(results.some(r=>!r.text.trim()))process.exitCode=1;}
    }
  }
});
