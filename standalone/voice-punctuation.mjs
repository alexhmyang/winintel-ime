// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
// Local conservative punctuation. Never rewrite existing model punctuation.
export function cleanVoice(input){
  let text=input.replace(/<\|[^>]*\|>/g,'').replace(/[\u3040-\u30ff\u31f0-\u31ff]+/g,'').trim();
  for(let i=0;i<3;i++){
    text=text.replace(/(^|[\s，。！？,!?])(?:呃+|嗯+|啊+)(?:[\s，。,.]+|$)/g,'$1').replace(/^(?:呃+|嗯+)[\s，,.]*/,'');
    text=text.replace(/(^|[\s，。！？,!?])(?:这个|那个)(?:[\s，,。.]+|(?=这个|那个)|$)/g,'$1');
  }
  text=text.replace(/。{2,}/g,'。').replace(/[，,]{2,}/g,'，').replace(/[，,]+(?=[。！？!?])/g,'').replace(/^[\s，,。]+/,'').trim();
  return text.replace(/[\p{P}\p{Z}\s]/gu,'')?text:'';
}
export function punctuateVoice(input,sentenceEnd=true){
  let text=cleanVoice(input);
  if(!text||/^(清除|清空|撤销)$/.test(text.replace(/[\s，。！？,.!?]/g,'')))return text;
  for(const [word,mark] of [['逗号','，'],['句号','。'],['问号','？'],['感叹号','！'],['分号','；'],['冒号','：'],['换行','\n'],['下一段','\n\n']])text=text.replace(new RegExp(word+'[，。,.]?','g'),mark);
  const chinese=/[\u3400-\u9fff]/.test(text);
  if(chinese)text=text.replace(/([\u3400-\u9fff]),/g,'$1，').replace(/([\u3400-\u9fff])\?/g,'$1？').replace(/([\u3400-\u9fff])!/g,'$1！').replace(/([，。！？；：])\s+(?=[\u3400-\u9fff])/g,'$1');
  if(!/[，。！？；：,.!?;:\n]$/.test(text)){
    const question=chinese?/(吗|么|呢|是不是|可不可以|能不能)$/.test(text)||/^(请问|为什么|怎么|哪里|谁|多少|什么时候)/.test(text):/^(what|why|how|when|where|who|can you|could you|do you|is it|are you)\b/i.test(text);
    text+=sentenceEnd?(chinese?(question?'？':'。'):(question?'?':'.')):(chinese?'，':',');
  }
  return text;
}
