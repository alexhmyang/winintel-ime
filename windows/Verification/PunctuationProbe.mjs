import assert from 'node:assert/strict';
import {writeFileSync} from 'node:fs';
import {punctuateVoice} from '../../standalone/voice-punctuation.mjs';
const fixtures=[
 ['中文句尾','今天开会',true,'今天开会。'],
 ['中文问句','可以吗',true,'可以吗？'],
 ['短段连接','继续处理',false,'继续处理，'],
 ['保留标点','今天开会。',true,'今天开会。'],
 ['口述标点','你好逗号明天见句号',true,'你好，明天见。'],
 ['口述换行','你好换行明天见',true,'你好\n明天见。'],
 ['英文疑问','How are you',true,'How are you?'],
 ['术语与小数','V100 32G NVLink 3.5',true,'V100 32G NVLink 3.5.'],
 ['空音频','',true,''],
 ['清除指令','清空。',true,'清空。'],
];
const rows=['| 检查 | 结果 |','|---|---|'];
for(const [name,input,end,expected] of fixtures){assert.equal(punctuateVoice(input,end),expected,name);rows.push(`| ${name} | 通过 |`);}
writeFileSync(process.argv[2],rows.join('\n')+'\n');console.log(rows.join('\n'));
