// SPDX-License-Identifier: GPL-3.0-or-later
import {readFile,writeFile} from 'node:fs/promises';
import {join,resolve} from 'node:path';
const root=resolve(import.meta.dirname,'..'), output=process.argv[2];
if(!output)throw new Error('Pass the payload directory');
const words=new Map();
for(const name of ['en.dict.yaml','en_ext.dict.yaml']){
  const input=await readFile(join(root,'data/rime-ice',name),'utf8');
  for(const row of input.split(/\r?\n/)){
    if(!row||row.startsWith('#'))continue;
    const [text,code]=row.trim().split(/\s+/);
    if(!code||!/^[a-zA-Z]+$/.test(code)||text.length>96)continue;
    const key=code.toLowerCase();if(!words.has(key))words.set(key,{text,weight:0});
  }
}
const common='hello world please thanks thank meeting project update version release download upload install offline online translation english computer customer confirm requirements server support hardware software network model memory performance'.split(' ');
for(const text of common)words.set(text,{text,weight:100});
const pack=JSON.parse(await readFile(join(root,'service/hotwords.json'),'utf8'));
for(const text of pack.english)words.set(text.toLowerCase().replace(/[^a-z]/g,''),{text,weight:200});
await writeFile(join(output,'english-words.tsv'),[...words].sort((a,b)=>a[0].localeCompare(b[0])).map(([key,w])=>`${key}\t${w.text}\t${w.weight}`).join('\n')+'\n');
await writeFile(join(output,'hot-chinese.tsv'),pack.chinese.map(w=>`${w.code.replace(/ /g,'')}\t${w.text}`).join('\n')+'\n');
await writeFile(join(output,'hotwords.json'),JSON.stringify(pack,null,2));
console.log(`English: ${words.size}; Chinese hot phrases: ${pack.chinese.length}`);
