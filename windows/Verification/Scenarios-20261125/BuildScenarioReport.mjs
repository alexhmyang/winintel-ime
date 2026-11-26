import {readFileSync,writeFileSync} from 'node:fs';
import {join} from 'node:path';
const root=process.env.WININTEL_BENCH_ROOT||'F:/Temp/winintel-ime-model-upgrade',output=process.env.WININTEL_REPORT_OUTPUT||'C:/Users/86187/Documents/Trae/default';
const read=file=>JSON.parse(readFileSync(join(root,file),'utf8'));
const asr=['sense','qwen','stream'].map(name=>read('scenario-asr-'+name+'.json'));
const translation=read('scenario-translation.json'),pipeline=read('scenario-caption-pipeline.json');
const baseline=read('scenario-translation-baseline.json'),realtime=read('scenario-realtime.json');
const review=read('scenario-semantic-review.json');
const names={sense:'SenseVoice INT8（2线程）',qwen:'Qwen3-ASR 0.6B INT8（4线程）',stream:'Zipformer实时 + SenseVoice复核（2+2线程）'};
const cell=s=>String(s??'').replace(/\|/g,'\\|').replace(/[\r\n]+/g,' ');
const sum=(rows,key)=>rows.reduce((n,r)=>n+(r[key]||0),0);
const avg=(rows,key)=>sum(rows,key)/rows.length;
const percentile=(data,p)=>[...data].sort((a,b)=>a-b)[Math.min(data.length-1,Math.ceil(data.length*p)-1)];
const cer=rows=>100*sum(rows,'errors')/sum(rows,'chars');
const wer=rows=>100*sum(rows,'wordErrors')/sum(rows,'words');
let text='# 数萌 AI 输入法：翻译、字幕与会议场景实测\n\n';
text+='测试机器：Windows，Intel Core i7-13700KF，16 核 / 24 线程，约 40 GiB 内存。全部 CPU、本机离线运行，无录音或文本上传。\n\n';
text+='| 项目 | 覆盖范围与口径 |\n|---|---|\n';
text+='| 语音样本 | 24 组中英内容，48 条干净音频，另加 16 条 12dB 白噪声音频，共 64 条；每模型每条运行 2 次，另有不计分预热 |\n';
text+=`| 音频总时长 | ${sum(asr[0].rows,'seconds').toFixed(1)} 秒；本机 Windows Huihui / Zira 合成音频，16kHz 单声道 |\n`;
text+='| 翻译样本 | 48 条双向文本，每条运行 3 次；暖机时间使用第 2、3 次平均 |\n';
text+='| 字幕串联 | 16 条实际流式复核结果逐段送入翻译，保留识别错误传播，和纯文本翻译分开评价 |\n';
text+='| 准确率 | 中文 CER：字符编辑距离 / 参考字符；英文 WER：词编辑距离 / 参考词。数字使用相同本地规范化规则，CER 忽略大小写、空格和标点 |\n';
text+='| 翻译质量 | 单参考 chrF2（字符 1–6 gram、β=2）衡量参考相似度，参考和译文均应用数字规范化；并非“正确率”，合理同义译法可能降低 chrF |\n';
text+='| 语义评审 | 助手逐条对照原文核查主旨、实体、数值单位、否定、日期和动作关系；完整=主要事实及关系全部保留，部分=主旨可辨但有事实/关系遗漏或错译，失败=主要意思错误。此为本组单人评审通过率，非独立人工盲评 |\n';
text+='| 时间 | 推理耗时与首次模型加载分列；流式音频以最快速度输入，测计算速度，不代表真人从开口到看到文字的墙钟延迟 |\n';
text+='| 适用范围 | 合成音频覆盖内容和受控白噪声；不包含真人口音、多人重叠、回声或剪映同录音对测，不能外推真人准确率 |\n\n';
text+='| 模型 | 首次加载 ms | 中文 CER | 英文 WER | 完全匹配 | 平均计算 ms | P50 ms | P95 ms | 总体 RTF |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|\n';
for(const r of asr){
  const zh=r.rows.filter(x=>x.lang==='zh'),en=r.rows.filter(x=>x.lang==='en');
  text+=`| ${names[r.name]} | ${r.loadMs.toFixed(0)} | ${cer(zh).toFixed(2)}% | ${wer(en).toFixed(2)}% | ${r.rows.filter(x=>x.errors===0).length}/64 | ${avg(r.rows,'ms').toFixed(0)} | ${percentile(r.rows.map(x=>x.ms),.5).toFixed(0)} | ${percentile(r.rows.map(x=>x.ms),.95).toFixed(0)} | ${(sum(r.rows,'ms')/1000/sum(r.rows,'seconds')).toFixed(3)} |\n`;
}
text+='\nRTF 为处理时间 / 音频时长，小于 1 表示计算能跟上录音；流式行包括分段和整句复核计算。不同模型线程配置已标明，不能将全部差异归因于权重。WER没有统一英文数词与阿拉伯数字，例如eight/8、nine thirty/930的表示差异也计为词错误；它是词形错误率，不能直接当成语义错误率。语音完全匹配按规范化字符判断，不包含标点恢复和语义核查。\n\n';
text+='| 场景 | 样本数 | SenseVoice 中文 CER / 英文 WER / 平均 ms | Qwen 中文 CER / 英文 WER / 平均 ms | 流式复核 中文 CER / 英文 WER / 平均 ms |\n|---|---:|---:|---:|---:|\n';
for(const group of [...new Set(asr[0].rows.map(x=>x.group))]){
  const rows=asr.map(r=>r.rows.filter(x=>x.group===group));
  text+=`| ${group} | ${rows[0].length} | ${rows.map(r=>`${cer(r.filter(x=>x.lang==='zh')).toFixed(2)}% / ${wer(r.filter(x=>x.lang==='en')).toFixed(2)}% / ${avg(r,'ms').toFixed(0)}`).join(' | ')} |\n`;
}
text+='\n| 音频条件 | SenseVoice 中文 CER / 英文 WER | Qwen 中文 CER / 英文 WER | 流式复核 中文 CER / 英文 WER |\n|---|---:|---:|---:|\n';
for(const condition of ['干净','12dB白噪声'])text+=`| ${condition} | ${asr.map(r=>`${cer(r.rows.filter(x=>x.condition===condition&&x.lang==='zh')).toFixed(2)}% / ${wer(r.rows.filter(x=>x.condition===condition&&x.lang==='en')).toFixed(2)}%`).join(' | ')} |\n`;
text+='\n| 翻译方向 | 样本数 | 首次模型加载 ms | 暖机平均 ms | 暖机 P50 ms | 暖机 P95 ms | 平均 chrF2 |\n|---|---:|---:|---:|---:|---:|---:|\n';
for(const direction of ['zh-en','en-zh']){
  const rows=translation.rows.filter(r=>r.direction===direction);
  text+=`| ${direction==='zh-en'?'中文 → 英文':'英文 → 简体中文'} | ${rows.length} | ${Math.max(...rows.map(r=>r.firstLoadMs)).toFixed(0)} | ${avg(rows,'warmMs').toFixed(0)} | ${percentile(rows.map(r=>r.warmMs),.5).toFixed(0)} | ${percentile(rows.map(r=>r.warmMs),.95).toFixed(0)} | ${avg(rows,'chrf').toFixed(2)} |\n`;
}
text+='\n翻译配置：OPUS-MT Marian，INT8，CPU 2 线程，贪心解码，单段最多 180 字符，英文到中文指定简体中文语言标签。其为小型专用神经翻译模型，并非聊天大模型。\n\n';
text+='| 语义评审范围 | 完整 | 部分 | 失败 | 完整通过率 |\n|---|---:|---:|---:|---:|\n';
for(const direction of ['zh-en','en-zh']){
  const rows=review.translation.filter(r=>r.id.endsWith(direction));
  const counts=['完整','部分','失败'].map(value=>rows.filter(r=>r.verdict===value).length);
  text+=`| ${direction} | ${counts.join(' | ')} | ${(100*counts[0]/rows.length).toFixed(1)}% |\n`;
}
const captionCounts=['完整','部分','失败'].map(value=>review.captions.filter(r=>r.verdict===value).length);
text+=`| 语音 → 双语字幕（16 条） | ${captionCounts.join(' | ')} | ${(100*captionCounts[0]/review.captions.length).toFixed(1)}% |\n`;
text+='\n上述字幕评审以原始场景内容为参考，识别错误和翻译错误均计入；分段译文按最终字幕顺序评审。\n\n';
text+='| 场景结论 | 实测证据与使用建议 |\n|---|---|\n';
text+='| 默认语音路线 | Qwen在本组中文CER上未优于SenseVoice，平均CPU计算时间明显更长；保留Zipformer实时预览与SenseVoice复核 |\n';
text+='| 日常与数字文本翻译 | 房间预订、邮箱、金额、温度和增长比例大多保留事实；中译英的问句位置限定、辣椒和会议参加动作仍有遗漏或错译 |\n';
text+='| 硬件中译英 | 8张V100显卡被译为8台服务器，即使型号和容量字符保留，也不能据此认定整句准确 |\n';
text+='| 实时字幕 | 中文问句可能因ASR缺标点被翻为陈述；英文日期、品牌和人名错误会传播到译文，端到端语义通过率低于直接文本翻译 |\n';
text+='| 术语保护 | 最终GPU否定、货币、负数和Gbps/GBps测试通过；分段回退会破坏句子衔接，需要改善整句翻译质量 |\n';
text+='| 会议记录 | 支持内存逐句记录、双语SRT和手动导出；决策待办为关键词摘录，重要负责人、日期和技术结论需校对 |\n\n';
text+='| 术语保护对照 | 原始平均 ms / chrF2 | 最终平均 ms / chrF2 |\n|---|---:|---:|\n';
for(const direction of ['zh-en','en-zh']){
  const original=baseline.rows.filter(r=>r.direction===direction),final=translation.rows.filter(r=>r.direction===direction);
  text+=`| ${direction} | ${avg(original,'warmMs').toFixed(0)} / ${avg(original,'chrf').toFixed(2)} | ${avg(final,'warmMs').toFixed(0)} / ${avg(final,'chrf').toFixed(2)} |\n`;
}
text+='\n最终版本增加本地品牌、技术词、货币和相对日期保护：优先采用包含关键事实的原译文，未保留时按术语边界分段翻译。chrF 提升不能等同于语义准确率，分段可能降低句子流畅度。\n\n';
text+='| 翻译场景 | 中译英平均 ms / chrF2 | 英译中平均 ms / chrF2 |\n|---|---:|---:|\n';
for(const group of [...new Set(translation.rows.map(x=>x.group))]){
  text+=`| ${group} | ${['zh-en','en-zh'].map(direction=>{const rows=translation.rows.filter(x=>x.group===group&&x.direction===direction);return `${avg(rows,'warmMs').toFixed(0)} / ${avg(rows,'chrf').toFixed(2)}`;}).join(' | ')} |\n`;
}
text+='\n| 字幕链路样本 | 音频秒 | 识别计算 ms | 翻译墙钟 ms | 串联计算估计 ms | 实际识别 | 实际译文 |\n|---|---:|---:|---:|---:|---|---|\n';
for(const r of pipeline.rows)text+=`| ${r.id} | ${r.audioSeconds.toFixed(2)} | ${r.recognitionComputeMs.toFixed(0)} | ${r.translationWallMs.toFixed(0)} | ${r.estimatedComputeMs.toFixed(0)} | ${cell(r.recognized)} | ${cell(r.translation)} |\n`;
text+='\n上表“串联计算估计”为分开测得的识别和翻译相加；不含真人说话时间及端点等待，不是完整麦克风实测延迟。首条各方向翻译包括新进程中的模型加载。当前字幕在停顿后进行复核和翻译，规则端点等待约 0.9 秒；原文实时预览在此之前显示。\n\n';
text+='| 真实节奏输入（100ms音频块） | 音频秒 | 首次原文预览 ms | 停止输入后复核完成 ms | 停止输入后译文完成 ms |\n|---|---:|---:|---:|---:|\n';
for(const r of realtime.rows)text+=`| ${r.id} | ${r.seconds.toFixed(2)} | ${r.firstPartialMs.toFixed(0)} | ${r.finalRecognitionLagMs.toFixed(0)} | ${r.finalTranslationLagMs.toFixed(0)} |\n`;
text+=`| 平均 | ${avg(realtime.rows,'seconds').toFixed(2)} | ${avg(realtime.rows,'firstPartialMs').toFixed(0)} | ${avg(realtime.rows,'finalRecognitionLagMs').toFixed(0)} | ${avg(realtime.rows,'finalTranslationLagMs').toFixed(0)} |\n`;
text+='\n真实节奏测试通过 Worker 接收按墙钟时间发送的 100ms 音频块，翻译在独立进程执行，两种语言预热后测试。尾部延迟从 finish 发出开始计时，模拟用户主动停止；没有模拟麦克风驱动和远程会议采集。等待自然停顿时还受端点规则影响。\n\n';
text+='| 字幕与会议功能检查 | 结果 |\n|---|---|\n| 空记录、按时间排序、毫秒时间戳、重叠修正、零时长修正、双语与换行、超过 1 小时 | 通过 |\n| 清空后旧回调失效、关闭隐藏、重新打开、实时预览、释放后的迟到 Render | 通过 |\n| 决策待办提取 | 保留原句，不生成虚构人名；为关键词摘录，非大模型摘要 |\n| 实际桌面翻译队列 | 中文UTF-8、元/美元、负数金额、DeepSeek/vLLM、GPU否定、Gbps/GBps 区分共6项通过 |\n\n';
text+='## 语音逐条结果\n\n| ID | 场景 / 条件 | 参考原文 | SenseVoice | Qwen | 流式复核 |\n|---|---|---|---|---|---|\n';
asr[0].rows.forEach((r,i)=>text+=`| ${r.id} | ${r.group} / ${r.condition} | ${cell(r.expected)} | ${asr.map(result=>cell(result.rows[i].formatted)).join(' | ')} |\n`);
text+='\n## 翻译逐条结果\n\n| ID | 场景 | 原文 | 参考译文 | 实际译文 | 暖机 ms | chrF2 | 语义评审 / 原因 |\n|---|---|---|---|---|---:|---:|---|\n';
for(const r of translation.rows){
  const assessment=review.translation.find(x=>x.id===r.id);
  if(!assessment)throw Error('Missing semantic assessment: '+r.id);
  text+=`| ${r.id} | ${r.group} | ${cell(r.source)} | ${cell(r.reference)} | ${cell(r.output)} | ${r.warmMs.toFixed(0)} | ${r.chrf.toFixed(2)} | ${assessment.verdict}：${cell(assessment.reason)} |\n`;
}
text+='\n## 字幕端到端语义评审\n\n| ID | 结论 | 依据 |\n|---|---|---|\n';
for(const r of review.captions)text+=`| ${r.id} | ${r.verdict} | ${cell(r.reason)} |\n`;
writeFileSync(join(output,'winintel-ime-0.5.0-场景准确率与速度实测.md'),text);
console.log('Report written: '+join(output,'winintel-ime-0.5.0-场景准确率与速度实测.md'));
