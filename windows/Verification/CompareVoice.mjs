import {createRequire} from 'node:module';
import {readFileSync,writeFileSync,readdirSync} from 'node:fs';
import {join,resolve} from 'node:path';
const [assets,fixtures,report]=process.argv.slice(2);
const sherpa=createRequire(join(resolve(assets),'standalone','voice-host.mjs'))('sherpa-onnx-node');
const dir=join(assets,'models','voice'),files=readdirSync(dir);
const find=p=>join(dir,files.find(f=>f.startsWith(p)&&f.endsWith('.onnx')&&f.includes('int8')));
const online=new sherpa.OnlineRecognizer({
  featConfig:{sampleRate:16000,featureDim:80},
  modelConfig:{transducer:{encoder:find('encoder'),decoder:find('decoder'),joiner:find('joiner')},tokens:join(dir,'tokens.txt'),numThreads:2,provider:'cpu',modelType:'zipformer'},
  decodingMethod:'greedy_search',enableEndpoint:false
});
const sense=new sherpa.OfflineRecognizer({featConfig:{sampleRate:16000,featureDim:80},
  modelConfig:{senseVoice:{model:join(assets,'models','sensevoice','model.int8.onnx'),language:'auto',useInverseTextNormalization:true},tokens:join(assets,'models','sensevoice','tokens.txt'),numThreads:2,provider:'cpu'}});
const expected=JSON.parse(readFileSync(join(fixtures,'fixtures.json'),'utf8').replace(/^\uFEFF/,''));
const norm=s=>s.toLowerCase().replace(/[\s\p{P}]/gu,'');
function distance(a,b){let row=Array.from({length:b.length+1},(_,i)=>i);for(let i=1;i<=a.length;i++){const next=[i];for(let j=1;j<=b.length;j++)next[j]=Math.min(next[j-1]+1,row[j]+1,row[j-1]+(a[i-1]!==b[j-1]));row=next;}return row[b.length];}
let output='Windows Huihui 合成语音，不代表真人或噪声环境准确率。\n\n| 预期文字 | 0.3.2 实时模型 | 0.4 整句复核 | 旧/新字符错误数 | 整句复核耗时 |\n|---|---|---|---|---|\n';
for(let i=0;i<expected.length;i++){
  const wav=sherpa.readWave(join(fixtures,`tts-${i}.wav`));
  const stream=online.createStream();stream.acceptWaveform(wav);stream.acceptWaveform({sampleRate:16000,samples:new Float32Array(12800)});stream.inputFinished();
  while(online.isReady(stream))online.decode(stream);
  const old=online.getResult(stream).text.trim(),start=performance.now();
  const s=sense.createStream();s.acceptWaveform(wav);sense.decode(s);
  const text=sense.getResult(s).text.trim(),elapsed=performance.now()-start;
  output+=`| ${expected[i]} | ${old} | ${text} | ${distance(norm(expected[i]),norm(old))} / ${distance(norm(expected[i]),norm(text))} | ${elapsed.toFixed(0)} ms |\n`;
}
writeFileSync(report,output);console.log(output);
