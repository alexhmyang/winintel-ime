import { readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { resolve, join } from 'node:path';
const root = resolve(import.meta.dirname, '..');
const rows = new Map();
function add(code, text, score) {
  let list = rows.get(code);
  if (!list) rows.set(code, list = []);
  const found = list.find(r => r.text === text);
  if (found) found.score = Math.max(found.score, score);
  else list.push({ text, score });
  list.sort((a,b) => b.score-a.score);
  list.length = Math.min(list.length, 9);
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
    add(code,text,score);
    const abbr = spelling.trim().split(/\s+/).map(s=>s[0]).join('');
    if (abbr.length>1 && abbr !== code) add(abbr,text,score-4);
  }
}
for (const file of readdirSync(join(root,'data','rime-ice')).filter(f=>f.endsWith('.dict.yaml'))) load(join(root,'data','rime-ice',file));
load(join(root,'data','starter.tsv'),12);
const strings = [], entries = [];
let offset = 0;
function str(s) { const buf=Buffer.from(s+'\0'); const at=offset; strings.push(buf); offset+=buf.length; return at; }
for (const code of [...rows.keys()].sort()) {
  const at=str(code);
  for (const row of rows.get(code)) entries.push([at,str(row.text),row.score]);
}
const header=Buffer.alloc(16);
header.write('WIME0002'); header.writeUInt32LE(entries.length,8); header.writeUInt32LE(16+entries.length*12,12);
const index=Buffer.alloc(entries.length*12);
entries.forEach((r,i)=>{index.writeUInt32LE(r[0],i*12);index.writeUInt32LE(r[1],i*12+4);index.writeFloatLE(r[2],i*12+8);});
writeFileSync(process.argv[2],Buffer.concat([header,index,...strings]));
console.log(`Own dictionary: ${rows.size} codes, ${entries.length} entries, ${16+index.length+offset} bytes`);
