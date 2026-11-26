import assert from 'node:assert/strict';
import {writeFileSync} from 'node:fs';
import {normalizeVoiceNumbers} from '../../standalone/voice-numbers.mjs';
const cases=[
 ['英伟达 v 一百 三十二 g','英伟达 V100 32G'],
 ['V一百三十二g','V100 32G'],
 ['v132g','V100 32G'],
 ['V132GB，另一张 V100 16G','V100 32GB，另一张 V100 16G'],
 ['v10032g','V100 32G'],
 ['v100   32 g','V100 32G'],
 ['V100 132G','V100 132G'],
 ['V100132G','V100 132G'],
 ['V一百十六GB','V100 16GB'],
 ['英伟达 v 一百，三十二 g','英伟达 V100 32G'],
 ['A 一百，八十 g','A100 80G'],
 ['a10080GB h10080G h200141GB','A100 80GB H100 80G H200 141GB'],
 ['一百三十二g，三十二g','132G，32G'],
 ['两张 V 一百，每张三十二 GB','2张 V100，每张32GB'],
 ['三点二 GHz，六十四 g 内存','3.2GHz，64G 内存'],
 ['一百二十八 GB，二百五十六 GB','128GB，256GB'],
 ['一千零二十四 MB，一万零三十二','1024MB，10032'],
 ['十六张卡，八台服务器，二零二六','16张卡，8台服务器，2026'],
 ['三十二个，一百三十二个','32个，132个'],
 ['一起测试，一般情况，一切正常，一心一意','一起测试，一般情况，一切正常，一心一意'],
 ['千万不要关机，万一断电，千方百计，一五一十','千万不要关机，万一断电，千方百计，一五一十'],
 ['请用 vLLM 部署 DeepSeek，检查 GPU','请用 vLLM 部署 DeepSeek，检查 GPU'],
 ['输入法指令，把三十二G改成六十四G','输入法指令，把32G改成64G'],
 ['３２ gb','32GB'],
 ['三十二 GPU','32 GPU'],
 ['V99932G','V99932G'],
 ['V10032 G b','V100 32GB'],
 ['三十二 g b','32GB'],
 ['A180G H180GB A140G V116GB','A100 80G H100 80GB A100 40G V100 16GB'],
 ['A一百八十G','A100 80G'],
 ['V100 32G s x m 二','V100 32G SXM2'],
 ['x s m 2，s x m 三，SXM4 sxm5','SXM2，SXM3，SXM4 SXM5'],
 ['V10032g SXM2，V132G SXM3','V100 32G SXM2，V100 32G SXM3'],
 ['n v link n v link','NVLink NVLink'],
 ['n v l i n k，nvlink','NVLink，NVLink'],
 ['我们的网站叫 w i n intel 点 com','我们的网站叫 winintel.com'],
 ['WININTEL.COM，w i n i n t e l dot c o m','winintel.com，winintel.com'],
 ['五十六 gigabbs，56 gigabps，56 Gbit/s','56Gbps，56Gbps，56Gbps'],
 ['56 gbps，56 Gbps，56Gbps，56 GBps，56 GB/s','56Gbps，56Gbps，56Gbps，56GBps，56GBps'],
 ['八 bits，8bits，八 bytes，8bytes','8 bits，8 bits，8 bytes，8 bytes'],
 ['s x m words，n v linkage，w i n intel 点 company','s x m words，n v linkage，w i n intel 点 company'],
 ['V100 32G ch TIME2M 32计 32G 巴比特 bar beats','V100 32G ch TIME2M 32计 32G 巴比特 bar beats'],
 ['NVLink envy link','NVLink NVLink'],
 ['我们的网站叫 w i n intel com','我们的网站叫 winintel.com'],
 ['winintel company，envy linkage，winintelcom','winintel company，envy linkage，winintelcom'],
];
let report='| 原始识别文字 | 规范化结果 | 验证 |\n|---|---|---|\n';
for(const [input,expected] of cases){
 const actual=normalizeVoiceNumbers(input);assert.equal(actual,expected,input);
 assert.equal(normalizeVoiceNumbers(actual),actual,'idempotence');
 report+=`| ${input} | ${actual} | 通过 |\n`;
}
if(process.argv[2])writeFileSync(process.argv[2],report);
console.log(`${cases.length} cases passed: model/capacity boundaries, numeric formatting, ordinary words, idempotence`);
