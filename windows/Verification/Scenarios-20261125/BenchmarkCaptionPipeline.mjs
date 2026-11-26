import {readFileSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
import {pathToFileURL} from 'node:url';
import {performance} from 'node:perf_hooks';
const root=process.env.WININTEL_BENCH_ROOT||'F:/Temp/winintel-ime-model-upgrade';
const {translate}=await import(pathToFileURL(join(root,'meeting-build/payload/standalone/translation-engine.mjs')));
const asr=JSON.parse(readFileSync(join(root,'scenario-asr-stream.json'),'utf8'));
const references=JSON.parse(readFileSync(join(root,'scenarios.json'),'utf8'));
const rows=[];
for(const row of asr.rows.filter(s=>s.condition==='干净'&&['0-zh','0-en','4-zh','5-en','8-zh','9-en','12-zh','13-en','16-zh','17-en','18-zh','19-en','20-zh','21-en','22-zh','23-en'].includes(s.id))){
  const direction=row.lang==='zh'?'zh-en':'en-zh',targets=[],times=[];
  for(const segment of row.finals.filter(s=>s.text)){
    const start=performance.now();
    const result=await translate(segment.text,direction);
    targets.push(result.text);times.push(performance.now()-start);
  }
  rows.push({id:row.id,group:row.group,source:row.expected,recognized:row.formatted,
    translation:targets.join(direction==='zh-en'?' ':''),translations:targets,
    reference:references[Number(row.id.split('-')[0])][row.lang==='zh'?'en':'zh'],
    audioSeconds:row.seconds,recognitionComputeMs:row.ms,translationWallMs:times.reduce((a,b)=>a+b,0),
    segments:row.finals,estimatedComputeMs:row.ms+times.reduce((a,b)=>a+b,0)});
  console.log(`${row.id} ${rows.at(-1).translation}`);
}
writeFileSync(join(root,'scenario-caption-pipeline.json'),JSON.stringify({rows},null,2));
