// 数萌AI服务器 (winintel) / Alex YANG - compositional, offline technical lexicon.
export function domainLexicon() {
  const groups = [
    [[['向量','xiang liang'],['关系型','guan xi xing'],['分布式','fen bu shi'],['时序','shi xu'],['内存','nei cun'],['图','tu']], [['数据库','shu ju ku']]],
    [[['向量','xiang liang'],['全文','quan wen'],['语义','yu yi'],['混合','hun he'],['稀疏','xi shu'],['多模态','duo mo tai']], [['检索','jian suo'],['索引','suo yin'],['搜索','sou suo']]],
    [[['推理','tui li'],['训练','xun lian'],['解码','jie ma'],['预填充','yu tian chong']], [['加速','jia su'],['吞吐量','tun tu liang'],['延迟','yan chi'],['性能','xing neng'],['服务','fu wu'],['引擎','yin qing']]],
    [[['动态','dong tai'],['静态','jing tai'],['权重','quan zhong'],['激活','ji huo'],['整数','zheng shu'],['低比特','di bi te']], [['量化','liang hua']]],
    [[['张量','zhang liang'],['流水线','liu shui xian'],['数据','shu ju'],['专家','zhuan jia'],['序列','xu lie']], [['并行','bing xing'],['并行计算','bing xing ji suan']]],
    [[['注意力','zhu yi li'],['混合专家','hun he zhuan jia'],['语言','yu yan'],['视觉','shi jue'],['多模态','duo mo tai']], [['模型','mo xing'],['机制','ji zhi'],['网络','wang luo']]],
    [[['离线','li xian'],['实时','shi shi'],['本地','ben di'],['流式','liu shi'],['端侧','duan ce']], [['语音','yu yin'],['输入','shu ru'],['语音识别','yu yin shi bie'],['语音合成','yu yin he cheng'],['翻译','fan yi'],['转写','zhuan xie']]],
    [[['显存','xian cun'],['内存','nei cun'],['网络','wang luo'],['存储','cun chu']], [['带宽','dai kuan'],['容量','rong liang'],['延迟','yan chi'],['瓶颈','ping jing']]],
    [[['请求','qing qiu'],['连接','lian jie'],['线程','xian cheng'],['进程','jin cheng'],['任务','ren wu']], [['调度','diao du'],['队列','dui lie'],['并发','bing fa'],['管理','guan li']]],
    [[['模型','mo xing'],['系统','xi tong'],['应用','ying yong'],['服务','fu wu']], [['部署','bu shu'],['监控','jian kong'],['配置','pei zhi'],['更新','geng xin'],['优化','you hua']]],
    [[['数据','shu ju'],['模型','mo xing'],['梯度','ti du'],['张量','zhang liang']], [['同步','tong bu'],['传输','chuan shu'],['压缩','ya suo'],['缓存','huan cun']]],
    [[['随机','sui ji'],['顺序','shun xu'],['批量','pi liang'],['异步','yi bu']], [['读取','du qu'],['写入','xie ru'],['处理','chu li']]],
  ];
  const rows=[];
  for(const [left,right] of groups)for(const [a,ac] of left)for(const [b,bc] of right)rows.push([a+b,ac+' '+bc,10000]);
  for(const [text,code] of [['向量','xiang liang'],['嵌入','qian ru'],['调大','tiao da'],['调小','tiao xiao'],['调高','tiao gao'],['调低','tiao di'],['调亮','tiao liang'],['调暗','tiao an']])rows.push([text,code,6000]);
  return rows;
}
