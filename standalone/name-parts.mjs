// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 数萌AI服务器 (winintel) / Alex YANG
// Common surname and given-name characters, authored here; no personal data.
export function buildNameParts(readings) {
  const surnames='王李张刘陈杨黄赵吴周徐孙马朱胡郭何林高罗郑梁谢宋唐许韩冯邓曹彭曾肖田董潘袁蔡蒋余于杜叶程魏苏吕丁任沈姚卢姜崔钟谭陆汪范金石廖贾夏韦傅方白邹孟熊秦邱江尹薛闫段雷侯龙史陶黎贺顾毛郝龚邵万钱严覃武戴莫孔向汤常温康施文牛樊葛邢安齐易乔伍庞颜倪庄聂章鲁岳翟殷詹申欧耿关兰焦俞左柳甘祝包宁尚符舒阮柯纪梅童凌毕单季裴席成苗谷盛曲翁战骆蓝路游辛靳管柴蒙鲍华喻祁蒲房滕屈饶解艾尤阳时穆农司卓古吉车简连芦麦褚娄窦喇戚岑景党宫费卜冷查井边沙邬仇米池应臧';
  // Ordering within an initial is a modest prior, never a claim about a person.
  const given='明敏梅美民铭梦茗曼萌沐慕满妙浩华慧海宏洪辉恒红昊航汉惠涵豪晖弘欢焕皓珩瀚衡和虹鸿怀含晗卉明伟文维威薇炜玮蔚婉雯温武闻万微唯巍卫望宛青强清庆勤琪琦启倩秋泉群茜谦乾沁晴祺乔桥全权奇琴芹巧千其楠宁娜南凝念年妮霓诺宁俊军静杰健佳嘉家建君晶锦景金洁婕菁京江瑾进剑骏娟娇敬久继吉靖今皎津谨智志子梓泽正振哲忠卓真珍中紫兆知昭章政舟竹珠芝洲宗震仲桢祖展湛琢之质博彬斌波柏冰兵碧蓓白宝邦奔滨北百邦博瑜宇雨玉云永勇燕艳颖莹怡一依伊亦奕易毅逸懿妍岩言炎洋阳扬杨尧瑶耀越悦月岳远源媛元圆园韵允芸英盈樱岳雅亚音银翊熠烨岳安爱艾傲昂奥岸阿琳林丽莉立力利黎磊蕾雷乐良亮梁玲凌灵露璐路鹿兰岚蓝龙隆伦柳莲连练临朗澜朗礼律令榕荣容蓉仁瑞锐睿然燃润若如茹冉融任柔儒日韧冉松思斯诗世士书舒淑树生胜圣盛少绍珊山善尚硕双顺素苏莎森霜声石水时升殊爽帅实守述曙深天田婷庭廷通彤桐童涛桃腾泰太添拓同唐甜禹越昕欣新鑫心信馨星兴杏晓小笑筱秀旭雪学轩萱宣玄贤宪显夏霞仙先祥湘香翔向想象雄雄锡溪希熙曦习夕惜羲修勋迅寻浔行许需孙赛森松东栋冬丹淡道德迪狄笛定丁鼎敦大达大登典黛岱朵端杜多朵方芳峰锋枫丰风凤菲飞斐凡帆繁梵福富芬奋馥赋非霏方芬芬光广国桂贵冠刚钢罡高歌革耕根格公功观莞果郭固高川春纯超朝成诚承城程晨辰宸昌畅长常楚初慈聪灿彩才采翠淳澄橙崇冲充存策创苍冲辰聪超伟慧明';
  const overrides={曾:'zeng',单:'shan',解:'xie',区:'ou',仇:'qiu',查:'zha',翟:'zhai',覃:'qin',乐:'le',尉:'wei',朴:'piao',褚:'chu',任:'ren'};
  const output=[];
  function add(role,text,code,rank){if(code)output.push([role,text,code,rank].join('\t'));}
  for(const [role,source] of [['S',surnames],['G',given]]) {
    const seen=new Set(),ranks=new Map();
    for(const text of source) {
      if(seen.has(text))continue;seen.add(text);
      const list=readings.get(text);if(!list)continue;
      const code=(role==='S'&&overrides[text])||[...list].sort((a,b)=>b[1]-a[1])[0][0];
      const initial=code[0],rank=ranks.get(initial)||0;ranks.set(initial,rank+1);
      add(role,text,code,rank);
    }
  }
  for(const [text,code] of [['欧阳','ou yang'],['司马','si ma'],['上官','shang guan'],['诸葛','zhu ge'],['夏侯','xia hou'],['东方','dong fang'],['皇甫','huang fu'],['尉迟','yu chi'],['公孙','gong sun'],['慕容','mu rong'],['司徒','si tu'],['令狐','ling hu'],['南宫','nan gong']])add('S',text,code,0);
  return output.join('\n')+'\n';
}
