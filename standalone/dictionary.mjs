// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
import { readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { resolve, join, dirname } from 'node:path';
import { buildNameParts } from './name-parts.mjs';
const root = resolve(import.meta.dirname, '..');
const rows = new Map();
const readings = new Map();
function add(code, text, score, abbreviation = false) {
  let list = rows.get(code);
  if (!list) rows.set(code, list = new Map());
  list.set(text, Math.max(list.get(text) ?? -Infinity, score));
}
function load(file, boost = 0) {
  let active = !file.endsWith('.yaml');
  for (const line of readFileSync(file, 'utf8').split(/\r?\n/)) {
    if (!active) { if (line.trim() === '...') active = true; continue; }
    if (!line || line.startsWith('#')) continue;
    const [text, spelling, freq] = line.split('\t');
    if (!text || !spelling || text.length > 48) continue;
    const code = spelling.toLowerCase().replace(/ü/g,'v').replace(/[^a-z]/g,'');
    if (!code || code.length > 64) continue;
    const score = Math.log2(Math.max(1, Number(freq) || 100)) + boost;
    if (text.length === 1 && /^[a-z]+$/.test(code)) {
      let list = readings.get(text);
      if (!list) readings.set(text, list = new Map());
      list.set(code, Math.max(list.get(code) || 0, score));
    }
    add(code,text,score);
    const abbr = spelling.trim().split(/\s+/).map(s=>s[0]).join('');
    if (abbr.length>1 && abbr !== code) add(abbr,text,score-4,true);
  }
}
for (const file of readdirSync(join(root,'data','rime-ice')).filter(f=>f.endsWith('.dict.yaml'))) load(join(root,'data','rime-ice',file));
load(join(root,'data','starter.tsv'),12);
const strings = [], entries = [], offsets = new Map(), words = new Set();
let offset = 0;
function str(s) { if(offsets.has(s))return offsets.get(s); const buf=Buffer.from(s+'\0'); const at=offset; strings.push(buf); offset+=buf.length; offsets.set(s,at); return at; }
for (const code of [...rows.keys()].sort()) {
  const at=str(code);
  for (const [text,score] of [...rows.get(code)].sort((a,b)=>b[1]-a[1]||a[0].localeCompare(b[0],'en'))) {
    entries.push([at,str(text),score]); words.add(text);
  }
}
const header=Buffer.alloc(16);
header.write('WIME0002'); header.writeUInt32LE(entries.length,8); header.writeUInt32LE(16+entries.length*12,12);
const index=Buffer.alloc(entries.length*12);
entries.forEach((r,i)=>{index.writeUInt32LE(r[0],i*12);index.writeUInt32LE(r[1],i*12+4);index.writeFloatLE(r[2],i*12+8);});
writeFileSync(process.argv[2],Buffer.concat([header,index,...strings]));
writeFileSync(join(dirname(process.argv[2]),'character-readings.tsv'),
  [...readings].map(([text,list])=>text+'\t'+[...list].sort((a,b)=>b[1]-a[1]).map(r=>r[0]).join(' ')).join('\n')+'\n');
writeFileSync(join(dirname(process.argv[2]),'name-parts.tsv'),buildNameParts(readings));
writeFileSync(join(dirname(process.argv[2]),'dictionary-stats.json'),JSON.stringify({
  codes:rows.size, entries:entries.length, uniqueWords:words.size,
  multiCharacterWords:[...words].filter(w=>[...w].length>2).length,
  bytes:16+index.length+offset, truncatedCandidates:0
},null,2)+'\n');
console.log(`Own dictionary: ${rows.size} codes, ${entries.length} entries, ${16+index.length+offset} bytes`);
