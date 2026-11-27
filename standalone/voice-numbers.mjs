// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
const digits={零:0,〇:0,一:1,二:2,两:2,三:3,四:4,五:5,六:6,七:7,八:8,九:9};
const digit='零〇一二两三四五六七八九',nonzero='一二两三四五六七八九';
const smallPattern=new RegExp(`^(?:[${nonzero}]千)?(?:零?[${nonzero}]百)?(?:零?[${nonzero}]?十)?(?:零?[${nonzero}])?$`);
function integer(value){
  if(!value)return 0;
  if([...value].every(x=>x in digits))return Number([...value].map(x=>digits[x]).join(''));
  for(const [unit,scale] of [['亿',100000000],['万',10000]]){
    if(!value.includes(unit))continue;
    const parts=value.split(unit);if(parts.length!==2||!parts[0])return null;
    const high=integer(parts[0]),low=integer(parts[1].replace(/^零/,''));
    if(high===null||low===null||high<1||low>=scale)return null;
    const result=high*scale+low;return Number.isSafeInteger(result)?result:null;
  }
  if(!smallPattern.test(value))return null;
  let result=0,pending=0;
  for(const char of value){if(char in digits)pending=digits[char];else{result+=(pending||1)*({十:10,百:100,千:1000}[char]);pending=0;}}
  return result+pending;
}
function numeral(value){
  const [whole,fraction]=value.split('点'),number=integer(whole);
  return number===null?null:String(number)+(fraction===undefined?'':'.'+[...fraction].map(x=>digits[x]).join(''));
}
const span=new RegExp(`[${digit}十百千万亿]+(?:点[${digit}]+)?`,'gu');
const quantity=/^\s*(?:[GMT]B?|GHz|MHz|bits?|bytes?|千瓦|瓦|伏|安培|毫秒|秒|分钟|小时|元|块钱|张|台|个|颗|倍|%|％)(?=$|[\s\p{P}\p{Script=Han}])/iu;
const boundary='(?=$|[\\s\\p{P}\\p{Script=Han}])';
function technicalTerms(text){
  // Join only known terms: never collapse arbitrary English words or letters.
  text=text.replace(new RegExp(`\\b(?:s\\s*x\\s*m|x\\s*s\\s*m)\\s*([2345二三四五])${boundary}`,'giu'),
    (_,version)=>'SXM'+(digits[version]??version));
  text=text.replace(new RegExp(`\\bn\\s*v\\s*l\\s*i\\s*n\\s*k${boundary}`,'giu'),'NVLink');
  // "envy" is a measured ASR spelling of the spoken letters N V.
  text=text.replace(new RegExp(`\\benvy\\s+link${boundary}`,'giu'),'NVLink');
  text=text.replace(new RegExp(`\\bw\\s*i\\s*n\\s*i\\s*n\\s*t\\s*e\\s*l(?:\\s*(?:点|\\.|dot)\\s*|\\s+)c\\s*o\\s*m${boundary}`,'giu'),'winintel.com');
  // A bit rate and a byte rate are different units. Preserve their casing.
  text=text.replace(new RegExp(`(\\d)\\s*(?:g\\s*b\\s*p\\s*s|g\\s*bit\\s*\\/\\s*s|gigabps|gigabbs|gigabits?\\s*(?:per\\s*second|\\/\\s*s))${boundary}`,'gu'),'$1Gbps');
  text=text.replace(new RegExp(`(\\d)\\s*(?:G\\s*b\\s*p\\s*s|G\\s*bit\\s*\\/\\s*s)${boundary}`,'gu'),'$1Gbps');
  text=text.replace(new RegExp(`(\\d)\\s*(?:G\\s*B\\s*p\\s*s|G\\s*B\\s*\\/\\s*s)${boundary}`,'gu'),'$1GBps');
  text=text.replace(new RegExp(`(\\d)\\s*(bits?|bytes?)${boundary}`,'giu'),
    (_,number,unit)=>number+' '+unit.toLowerCase());
  return text;
}
export function normalizeVoiceNumbers(input){
  let text=String(input??'').replace(/[０-９]/g,x=>String(x.charCodeAt(0)-0xff10));
  // Split the known model before converting an adjoining spoken capacity.
  text=text.replace(new RegExp(`\\bV\\s*一百\\s*(三十二|十六)\\s*(GB|G)${boundary}`,'giu'),
    (_,capacity,unit)=>'V100 '+numeral(capacity)+unit.toUpperCase());
  // Known 100-series models plus common capacities; no arbitrary model guessing.
  const merged={V132:'V100 32',V116:'V100 16',A180:'A100 80',A140:'A100 40',H180:'H100 80'};
  const splitMerged=value=>value.replace(new RegExp(`\\b([VAH])\\s*(132|116|180|140)\\s*(GB|G)${boundary}`,'giu'),
    (original,letter,number,unit)=>merged[letter.toUpperCase()+number]?merged[letter.toUpperCase()+number]+unit.toUpperCase():original);
  text=splitMerged(text);
  text=text.replace(span,(value,offset,source)=>{
    const before=source.slice(0,offset),after=source.slice(offset+value.length);
    if(value==='一'&&/(?:另|上|下|这|那)$/.test(before))return value;
    if((value==='千万'&&/^(?:不要|别|不能|不可|注意|记得|小心)/.test(after))||value==='万一')return value;
    if(!(value.length>1||value==='十'||quantity.test(after)||/[A-Za-z]\s*$/.test(before)))return value;
    return numeral(value)??value;
  });
  text=text.replace(/\b([vVaAhHbBrRlL])\s+(\d+(?:\.\d+)?)/g,(_,letter,number)=>letter.toUpperCase()+number);
  text=text.replace(/\b([vahbrl])(\d+)(?=$|[\s\p{P}\p{Script=Han}]|[GMT])/giu,(_,letter,number)=>letter.toUpperCase()+number);
  text=text.replace(/(\d)\s*([GMT])\s+B(?=$|[\s\p{P}\p{Script=Han}])/giu,(_,number,unit)=>number+unit.toUpperCase()+'B');
  text=text.replace(/(\d)\s*(GHz|MHz|GB|TB|MB|G|T|M)(?=$|[\s\p{P}\p{Script=Han}])/giu,(_,number,unit)=>number+({ghz:'GHz',mhz:'MHz'}[unit.toLowerCase()]||unit.toUpperCase()));
  text=splitMerged(text);
  // Model and capacity are two separate fields, even if ASR concatenates them.
  text=text.replace(new RegExp(`\\b(V100|A100|H100|H200|A800|RTX\\s*\\d{4})\\s*[,，:：]?\\s*(\\d+(?:\\.\\d+)?)(GB|G)${boundary}`,'giu'),
    (_,model,capacity,unit)=>model.toUpperCase()+' '+capacity+unit.toUpperCase());
  return technicalTerms(text);
}
