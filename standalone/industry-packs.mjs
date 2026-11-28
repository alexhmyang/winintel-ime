// 数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later
import {readFileSync,writeFileSync} from 'node:fs';
import {join,resolve} from 'node:path';
export const domains={
 programming:{label:'编程与软件',match:/程序|编程|代码|算法|函数|指针|线程|数据库|编译|软件|接口|服务器|协议|中间件|路由|栈|异步|缓存|调试|变量|序列化/},
 ai:{label:'AI与算力',match:/人工智能|机器学习|深度学习|神经网络|模型|梯度|张量|量化|推理|显存|算力|并行|向量|嵌入|训练|解码|吞吐/},
 medical:{label:'医疗健康',match:/诊断|临床|病|药|医|血|肝|肾|心脏|心电|治疗|护理|康复|手术|处方|抗体|疫苗/},
 finance:{label:'金融财会',match:/财务|财产|会计|审计|资产|负债|账|帐|利润|税|现金|基金|债券|股票|利率|贷款|银行|期货|估值|保险/},
 law:{label:'法律政务',match:/法律|法院|法官|律师|诉讼|仲裁|合同|协议|民法|刑法|原告|被告|举证|侵权|专利|商标|著作权|行政|司法/},
 education:{label:'教育科研',match:/教育|教学|课程|课堂|学科|学生|教师|教案|备课|作业|考试|试卷|学习|素养|定理|方程|积分|导数|概率/},
 manufacturing:{label:'工业制造',match:/制造|工艺|加工|机床|数控|焊|模具|公差|装配|零件|机械|钢|热处理|材料|生产|供应链|质量|物料/},
 ecommerce:{label:'电商营销',match:/电商|商品|订单|库存|销售|售后|客服|物流|营销|推广|优惠|退货|复购|点击率|转化率|客单价|直播|零售|批发/}
};
export const englishTokens={
 programming:`Python Java JavaScript TypeScript Rust Go Kotlin Swift C C++ C# .NET PHP Ruby SQL HTML CSS React ReactDOM Vue Angular Svelte Next.js Nuxt.js Node.js Express FastAPI Django Flask SpringBoot Laravel Rails Docker Kubernetes Git GitHub GitLab VSCode IntelliJIDEA PyCharm AndroidStudio PostgreSQL MySQL SQLite MongoDB Redis Elasticsearch Kafka RabbitMQ Nginx Apache Linux Ubuntu Windows macOS PowerShell Bash npm pnpm yarn pip conda venv uv HTTP HTTPS TCP UDP IPv4 IPv6 DNS REST GraphQL WebSocket OAuth JWT JSON YAML TOML XML UTF-8 ASCII Unicode API SDK CLI GUI IDE CI/CD RPC ORM CRUD ACID CAP SOLID DRY KISS MVC MVVM async await def class import from return yield lambda with try except finally raise pass None True False public private protected static final const let var interface enum implements extends function export default undefined null true false console.log JSON.parse JSON.stringify Array.from Object.keys Promise.all Promise.resolve setTimeout setInterval useState useEffect useMemo useCallback useRef createContext getUserById getUserName userId userName user_id user_name accessToken refreshToken access_token refresh_token requestId request_id responseData response_data camelCase snake_case std::vector std::string std::unordered_map std::unique_ptr std::shared_ptr int64_t uint64_t size_t len range enumerate zip isinstance issubclass print sorted open map filter sum list dict tuple set str int float bool bytes pathlib asyncio dataclass pytest unittest assertEqual assertTrue assertFalse assertRaises StringBuilder ConcurrentHashMap ArrayList HashMap IOException NullPointerException IndexError KeyError ValueError TypeError RuntimeError FileNotFoundError ConnectionError TimeoutError NullReferenceException IEnumerable Task CancellationToken ConfigureAwait IDisposable Console.WriteLine System.Text.Json LINQ NumPy Pandas SciPy Matplotlib requests httpx aiohttp pydantic SQLAlchemy Alembic Celery BeautifulSoup Selenium Playwright Jest Vitest Mocha Cypress JUnit Maven Gradle CMake Ninja GCC Clang MSVC Makefile Cargo rustfmt clippy rustup rustc gofmt goroutine channel defer recover struct trait impl match Option Result Some Ok Err Vec HashSet Arc Mutex RwLock Future Send Sync virtual override noexcept constexpr nullptr template typedef namespace include sizeof malloc calloc realloc free memcpy memset printf fprintf snprintf pthread_create git status git commit git push git pull git diff git switch git merge git rebase git stash git log docker compose docker build docker run kubectl apply kubectl get kubectl logs npm install npm run pnpm install pip install python -m`,
 ai:`PyTorch TensorFlow JAX ONNX ONNXRuntime vLLM SGLang llama.cpp llama-cpp Transformers HuggingFace Tokenizer SentencePiece CUDA cuDNN cuBLAS NCCL NVLink InfiniBand OpenMP oneDNN AVX2 AVX512 BF16 FP16 FP32 FP8 INT8 INT4 GPTQ AWQ GGUF RAG LoRA QLoRA MoE MTP MLA GQA MHA RoPE RMSNorm LayerNorm Softmax FlashAttention PagedAttention DeepSeek Qwen Llama Mistral Gemma BERT GPT SenseVoice Whisper sherpa-onnx FAISS Milvus Qdrant Chroma TensorRT torch.compile torch.no_grad torch.inference_mode torch.Tensor torch.nn torch.optim torch.utils.data DataLoader Dataset Adam AdamW SGD CrossEntropyLoss backward requires_grad state_dict load_state_dict safetensors einsum bmm matmul unsqueeze squeeze reshape contiguous detach multiprocessing thread_pool KVCache GPU CPU NPU TP PP DP EP TTFT TPOT tokens/s batch_size learning_rate max_tokens top_p top_k temperature`,
 finance:`EBITDA EBIT DCF NPV IRR WACC ROE ROA EPS PE PB NAV CAGR CAPEX OPEX IFRS GAAP VAT GST P&L`,
 medical:`MRI CT ECG EEG ICU BMI HbA1c CRP SpO2`,
 law:`GDPR NDA UDRP WIPO`,
 education:`STEM STEAM PBL PhD DOI`,
 manufacturing:`CAD CAM CNC PLC MES ERP BOM ISO9001 FMEA SPC OEE`,
 ecommerce:`SKU SPU GMV ROI ROAS CTR CVR SEO SEM CRM B2B B2C DTC`
};
// Phrases containing spaces are explicit commands, not automatic shell execution.
const commandTokens=['git status','git commit','git push','git pull','git diff','git switch','git merge','git rebase','git stash','git log','docker compose','docker build','docker run','kubectl apply','kubectl get','kubectl logs','npm install','npm run','pnpm install','pip install','python -m'];
export function publicWords(root){
 const all=new Map();
 for(const name of ['base.dict.yaml','ext.dict.yaml','others.dict.yaml']){
  let input;try{input=readFileSync(join(root,'data/rime-ice',name),'utf8');}catch{continue;}
  let active=false;
  for(const row of input.split(/\r?\n/)){
   if(!active){if(row.trim()==='...')active=true;continue;}
   if(!row||row.startsWith('#'))continue;
   const [text,spelling,freq]=row.split('\t');
   if(!text||!/^[\u3400-\u9fff]{2,16}$/.test(text)||!spelling||!/^[a-z ]+$/.test(spelling))continue;
   const score=Math.log2(Math.max(1,Number(freq)||100));
   const key=text+'\t'+spelling;if(!all.has(key)||all.get(key).frequency<score)all.set(key,{text,spelling,frequency:score});
  }
 }
 return [...all.values()];
}
export function buildIndustryPacks(root){
 const dictionaries={},rows=[];
 const seeds=readFileSync(join(root,'data/industry-seeds.tsv'),'utf8').split(/\r?\n/).filter(s=>s&&!s.startsWith('#')).map(s=>s.split('\t'));
 const publicEntries=publicWords(root);
 for(const [id,config] of Object.entries(domains)){
  const entries=new Map();
  const selected=publicEntries.filter(w=>config.match.test(w.text)).sort((a,b)=>b.frequency-a.frequency||a.text.localeCompare(b.text)).slice(0,1200);
  for(const w of selected)entries.set(w.text,{...w,weight:120+Math.min(20,w.frequency)});
  for(const [profile,text,spelling] of seeds)if(profile===id)entries.set(text,{text,spelling,weight:160});
  for(const w of entries.values()){
   const code=w.spelling.replaceAll(' ',''),abbr=w.spelling.split(' ').map(s=>s[0]).join('');
   rows.push([id,'C',code,w.text,w.weight.toFixed(3)]);
   if(abbr.length>1&&abbr!==code)rows.push([id,'C',abbr,w.text,(w.weight-4).toFixed(3)]);
  }
  const tokens=new Set(englishTokens[id].split(/\s+/));
  if(id==='programming'){for(const part of ['git','status','commit','push','pull','diff','switch','merge','rebase','stash','log','docker','compose','build','run','kubectl','apply','get','logs','npm','install','pnpm','pip','python','-m'])tokens.delete(part);for(const cmd of commandTokens)tokens.add(cmd);for(const base of ['git','docker','kubectl','npm','pnpm','pip','python'])tokens.add(base);}
  for(const token of tokens)rows.push([id,'E',token.toLowerCase(),token,180]);
  dictionaries[id]={label:config.label,chinese:entries.size,english:tokens.size};
 }
 rows.sort((a,b)=>a[0].localeCompare(b[0])||a[1].localeCompare(b[1])||a[2].localeCompare(b[2])||a[3].localeCompare(b[3]));
 return {rows,dictionaries};
}
if(process.argv[1]&&resolve(process.argv[1])===resolve(import.meta.filename)){
 const output=process.argv[2];if(!output)throw new Error('Pass payload directory');
 const {rows,dictionaries}=buildIndustryPacks(resolve(import.meta.dirname,'..'));
 writeFileSync(join(output,'industry-words.tsv'),rows.map(r=>r.join('\t')).join('\n')+'\n');
 writeFileSync(join(output,'industry-stats.json'),JSON.stringify(dictionaries,null,2)+'\n');
 console.log(JSON.stringify(dictionaries));
}
