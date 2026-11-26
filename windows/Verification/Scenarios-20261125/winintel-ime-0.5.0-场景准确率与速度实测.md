# 数萌 AI 输入法：翻译、字幕与会议场景实测

测试机器：Windows，Intel Core i7-13700KF，16 核 / 24 线程，约 40 GiB 内存。全部 CPU、本机离线运行，无录音或文本上传。

| 项目 | 覆盖范围与口径 |
|---|---|
| 语音样本 | 24 组中英内容，48 条干净音频，另加 16 条 12dB 白噪声音频，共 64 条；每模型每条运行 2 次，另有不计分预热 |
| 音频总时长 | 347.6 秒；本机 Windows Huihui / Zira 合成音频，16kHz 单声道 |
| 翻译样本 | 48 条双向文本，每条运行 3 次；暖机时间使用第 2、3 次平均 |
| 字幕串联 | 16 条实际流式复核结果逐段送入翻译，保留识别错误传播，和纯文本翻译分开评价 |
| 准确率 | 中文 CER：字符编辑距离 / 参考字符；英文 WER：词编辑距离 / 参考词。数字使用相同本地规范化规则，CER 忽略大小写、空格和标点 |
| 翻译质量 | 单参考 chrF2（字符 1–6 gram、β=2）衡量参考相似度，参考和译文均应用数字规范化；并非“正确率”，合理同义译法可能降低 chrF |
| 语义评审 | 助手逐条对照原文核查主旨、实体、数值单位、否定、日期和动作关系；完整=主要事实及关系全部保留，部分=主旨可辨但有事实/关系遗漏或错译，失败=主要意思错误。此为本组单人评审通过率，非独立人工盲评 |
| 时间 | 推理耗时与首次模型加载分列；流式音频以最快速度输入，测计算速度，不代表真人从开口到看到文字的墙钟延迟 |
| 适用范围 | 合成音频覆盖内容和受控白噪声；不包含真人口音、多人重叠、回声或剪映同录音对测，不能外推真人准确率 |

| 模型 | 首次加载 ms | 中文 CER | 英文 WER | 完全匹配 | 平均计算 ms | P50 ms | P95 ms | 总体 RTF |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| SenseVoice INT8（2线程） | 945 | 3.45% | 6.91% | 49/64 | 481 | 460 | 914 | 0.089 |
| Qwen3-ASR 0.6B INT8（4线程） | 3093 | 3.45% | 9.12% | 50/64 | 2215 | 2025 | 3132 | 0.408 |
| Zipformer实时 + SenseVoice复核（2+2线程） | 929 | 3.60% | 7.46% | 47/64 | 531 | 470 | 1009 | 0.098 |

RTF 为处理时间 / 音频时长，小于 1 表示计算能跟上录音；流式行包括分段和整句复核计算。不同模型线程配置已标明，不能将全部差异归因于权重。WER没有统一英文数词与阿拉伯数字，例如eight/8、nine thirty/930的表示差异也计为词错误；它是词形错误率，不能直接当成语义错误率。语音完全匹配按规范化字符判断，不包含标点恢复和语义核查。

| 场景 | 样本数 | SenseVoice 中文 CER / 英文 WER / 平均 ms | Qwen 中文 CER / 英文 WER / 平均 ms | 流式复核 中文 CER / 英文 WER / 平均 ms |
|---|---:|---:|---:|---:|
| 日常交流 | 10 | 0.00% / 0.00% / 226 | 0.00% / 0.00% / 1898 | 0.00% / 0.00% / 526 |
| 会议与待办 | 10 | 0.00% / 1.92% / 415 | 0.00% / 1.92% / 1820 | 0.00% / 1.92% / 770 |
| 硬件与技术 | 10 | 5.31% / 8.62% / 596 | 3.54% / 27.59% / 2238 | 5.31% / 8.62% / 511 |
| 数字与交易 | 10 | 0.00% / 23.91% / 424 | 0.00% / 34.78% / 2110 | 0.00% / 23.91% / 384 |
| 否定与逻辑 | 6 | 0.00% / 0.00% / 450 | 0.00% / 0.00% / 1881 | 0.00% / 0.00% / 371 |
| 长句字幕 | 6 | 0.81% / 0.00% / 905 | 0.00% / 0.00% / 3707 | 0.81% / 0.00% / 765 |
| 中英混说 | 12 | 9.09% / 12.31% / 506 | 10.80% / 0.00% / 2300 | 9.66% / 15.38% / 438 |

| 音频条件 | SenseVoice 中文 CER / 英文 WER | Qwen 中文 CER / 英文 WER | 流式复核 中文 CER / 英文 WER |
|---|---:|---:|---:|
| 干净 | 2.81% / 5.07% | 3.01% / 6.88% | 3.01% / 5.80% |
| 12dB白噪声 | 5.36% / 12.79% | 4.76% / 16.28% | 5.36% / 12.79% |

| 翻译方向 | 样本数 | 首次模型加载 ms | 暖机平均 ms | 暖机 P50 ms | 暖机 P95 ms | 平均 chrF2 |
|---|---:|---:|---:|---:|---:|---:|
| 中文 → 英文 | 24 | 578 | 689 | 570 | 1293 | 63.26 |
| 英文 → 简体中文 | 24 | 581 | 538 | 420 | 939 | 44.36 |

翻译配置：OPUS-MT Marian，INT8，CPU 2 线程，贪心解码，单段最多 180 字符，英文到中文指定简体中文语言标签。其为小型专用神经翻译模型，并非聊天大模型。

| 语义评审范围 | 完整 | 部分 | 失败 | 完整通过率 |
|---|---:|---:|---:|---:|
| zh-en | 13 | 10 | 1 | 54.2% |
| en-zh | 19 | 5 | 0 | 79.2% |
| 语音 → 双语字幕（16 条） | 7 | 5 | 4 | 43.8% |

上述字幕评审以原始场景内容为参考，识别错误和翻译错误均计入；分段译文按最终字幕顺序评审。

| 场景结论 | 实测证据与使用建议 |
|---|---|
| 默认语音路线 | Qwen在本组中文CER上未优于SenseVoice，平均CPU计算时间明显更长；保留Zipformer实时预览与SenseVoice复核 |
| 日常与数字文本翻译 | 房间预订、邮箱、金额、温度和增长比例大多保留事实；中译英的问句位置限定、辣椒和会议参加动作仍有遗漏或错译 |
| 硬件中译英 | 8张V100显卡被译为8台服务器，即使型号和容量字符保留，也不能据此认定整句准确 |
| 实时字幕 | 中文问句可能因ASR缺标点被翻为陈述；英文日期、品牌和人名错误会传播到译文，端到端语义通过率低于直接文本翻译 |
| 术语保护 | 最终GPU否定、货币、负数和Gbps/GBps测试通过；分段回退会破坏句子衔接，需要改善整句翻译质量 |
| 会议记录 | 支持内存逐句记录、双语SRT和手动导出；决策待办为关键词摘录，重要负责人、日期和技术结论需校对 |

| 术语保护对照 | 原始平均 ms / chrF2 | 最终平均 ms / chrF2 |
|---|---:|---:|
| zh-en | 401 / 60.91 | 689 / 63.26 |
| en-zh | 360 / 40.09 | 538 / 44.36 |

最终版本增加本地品牌、技术词、货币和相对日期保护：优先采用包含关键事实的原译文，未保留时按术语边界分段翻译。chrF 提升不能等同于语义准确率，分段可能降低句子流畅度。

| 翻译场景 | 中译英平均 ms / chrF2 | 英译中平均 ms / chrF2 |
|---|---:|---:|
| 日常交流 | 309 / 67.15 | 380 / 23.34 |
| 会议与待办 | 552 / 57.52 | 348 / 62.86 |
| 硬件与技术 | 876 / 55.86 | 757 / 55.45 |
| 数字与交易 | 570 / 65.26 | 356 / 48.06 |
| 否定与逻辑 | 393 / 49.03 | 372 / 40.72 |
| 长句字幕 | 885 / 73.16 | 819 / 36.60 |
| 中英混说 | 1186 / 72.65 | 790 / 37.82 |

| 字幕链路样本 | 音频秒 | 识别计算 ms | 翻译墙钟 ms | 串联计算估计 ms | 实际识别 | 实际译文 |
|---|---:|---:|---:|---:|---|---|
| 0-zh | 3.90 | 314 | 795 | 1109 | 你好请问附近哪里有地铁站 | Hello, there's a subway station nearby. |
| 0-en | 3.48 | 291 | 746 | 1037 | hello where is the nearest subway station | 你好,最近的地铁站在哪里? |
| 4-zh | 3.56 | 657 | 366 | 1023 | 我们决定下周一发布新版本 | We've decided next Monday Publish New Version |
| 5-en | 5.63 | 1105 | 189 | 1294 | jihang wei is responsible for testing the deadline is friday afternoon | 周五下午由郑惠负责测试 |
| 8-zh | 6.24 | 627 | 508 | 1135 | 这台服务器有8张 V100显卡每张显存32GB | There are eight servers on this one. V100 Each card is visible. 32GB |
| 9-en | 6.78 | 576 | 454 | 1030 | connect the graphics cards using envlink the network bandwidth is 56Gbps | 使用 envlink 连接显卡, 网络带宽为 56Gbps |
| 12-zh | 5.05 | 400 | 721 | 1120 | 订单总金额是1250元包含运费 | The total amount of the order is... 1250 yuan Including freight |
| 13-en | 4.50 | 359 | 545 | 904 | please attend the meeting at 930 am on november 25 | 请在会议开始时间上午9时30分在会议开始之前出席。 |
| 16-zh | 4.71 | 377 | 317 | 695 | 不要删除原始文件只删除临时文件 | Do not delete original files only delete temporary files |
| 17-en | 4.00 | 322 | 281 | 603 | if the tests fail we cannot release the software | 如果测试失败, 我们无法释放软件 |
| 18-zh | 10.05 | 853 | 646 | 1498 | 为了保护用户隐私我们在本地处理全部音频并且只有用户点击导出后才会保存会议记录 | To protect user privacy, we process all audio locally and only store minutes after user clicks export |
| 19-en | 10.05 | 765 | 838 | 1603 | although the new recognition model is more accurate it runs more slowly on ordinary computers so we keep the fast model and then review complete sentences | 虽然新的识别模型更精确 它在普通电脑上运行的慢一些 所以我们保留快速模型 然后再审查完整句子 |
| 20-zh | 6.75 | 527 | 944 | 1471 | 请打开 windows 设置检查 microphone 权限然后重新启动 | Open it, please. Windows Set Check microphone Permissions then restart |
| 21-en | 7.08 | 452 | 854 | 1307 | this project is completely offline do not need an api key and does not upload audio | 这个项目是完全的离线不需要 API 密钥, 并且不上传音频 |
| 22-zh | 5.71 | 423 | 1356 | 1779 | 今天先做 benchmark 明天 在优化 latency 和 throughput | Let's do it today. benchmark Tomorrow. It's optimizing. latency and throughput |
| 23-en | 4.70 | 371 | 481 | 852 | openwinintelcom and download the wineintel ai input method | 打开网络并下载葡萄酒英特尔 Ai 输入方法 |

上表“串联计算估计”为分开测得的识别和翻译相加；不含真人说话时间及端点等待，不是完整麦克风实测延迟。首条各方向翻译包括新进程中的模型加载。当前字幕在停顿后进行复核和翻译，规则端点等待约 0.9 秒；原文实时预览在此之前显示。

| 真实节奏输入（100ms音频块） | 音频秒 | 首次原文预览 ms | 停止输入后复核完成 ms | 停止输入后译文完成 ms |
|---|---:|---:|---:|---:|
| 0-zh | 3.90 | 731 | 418 | 750 |
| 0-en | 3.48 | 432 | 401 | 676 |
| 4-zh | 3.56 | 440 | 388 | 1048 |
| 4-en | 3.42 | 434 | 351 | 597 |
| 8-zh | 6.24 | 737 | 581 | 1506 |
| 8-en | 6.16 | 443 | 599 | 1179 |
| 18-zh | 10.05 | 728 | 897 | 1519 |
| 18-en | 7.68 | 447 | 716 | 1400 |
| 平均 | 5.56 | 549 | 544 | 1084 |

真实节奏测试通过 Worker 接收按墙钟时间发送的 100ms 音频块，翻译在独立进程执行，两种语言预热后测试。尾部延迟从 finish 发出开始计时，模拟用户主动停止；没有模拟麦克风驱动和远程会议采集。等待自然停顿时还受端点规则影响。

| 字幕与会议功能检查 | 结果 |
|---|---|
| 空记录、按时间排序、毫秒时间戳、重叠修正、零时长修正、双语与换行、超过 1 小时 | 通过 |
| 清空后旧回调失效、关闭隐藏、重新打开、实时预览、释放后的迟到 Render | 通过 |
| 决策待办提取 | 保留原句，不生成虚构人名；为关键词摘录，非大模型摘要 |
| 实际桌面翻译队列 | 中文UTF-8、元/美元、负数金额、DeepSeek/vLLM、GPU否定、Gbps/GBps 区分共6项通过 |

## 语音逐条结果

| ID | 场景 / 条件 | 参考原文 | SenseVoice | Qwen | 流式复核 |
|---|---|---|---|---|---|
| 0-zh | 日常交流 / 干净 | 你好，请问附近哪里有地铁站？ | 你好请问附近哪里有地铁站 | 你好，请问附近哪里有地铁站？ | 你好请问附近哪里有地铁站 |
| 0-en | 日常交流 / 干净 | Hello, where is the nearest subway station? | hello where is the nearest subway station | Hello, where is the nearest subway station? | hello where is the nearest subway station |
| 1-zh | 日常交流 / 干净 | 我想订一间双人房，住三个晚上。 | 我想订一间双人房住3个晚上 | 我想订一间双人房，住3个晚上。 | 我想订一间双人房住3个晚上 |
| 1-en | 日常交流 / 干净 | I would like to book a double room for three nights. | i would like to book a double room for three nights | I would like to book a double room for three nights. | i would like to book a double room for three nights |
| 2-zh | 日常交流 / 干净 | 请把这份文件发到我的邮箱。 | 请把这份文件发到我的邮箱 | 请把这份文件发到我的邮箱。 | 请把这份文件发到我的邮箱 |
| 2-en | 日常交流 / 干净 | Please send this document to my email address. | please send this document to my email address | Please send this document to my email address. | please send this document to my email address |
| 3-zh | 日常交流 / 干净 | 这道菜不要放辣椒，我对花生过敏。 | 这道菜不要放辣椒我对花生过敏 | 这道菜不要放辣椒，我对花生过敏。 | 这道菜不要放辣椒我对花生过敏 |
| 3-en | 日常交流 / 干净 | Please do not add chili to this dish. I am allergic to peanuts. | please do not add chili to this dish i am allergic to peanuts | Please do not add chili to this dish. I am allergic to peanuts. | please do not add chili to this dish i am allergic to peanuts |
| 4-zh | 会议与待办 / 干净 | 我们决定下周一发布新版本。 | 我们决定下周一发布新版本 | 我们决定下周一发布新版本。 | 我们决定下周一发布新版本 |
| 4-en | 会议与待办 / 干净 | We decided to release the new version next Monday. | we decided to release the new version next monday | We decided to release the new version next Monday. | we decided to release the new version next monday |
| 5-zh | 会议与待办 / 干净 | 测试由张伟负责，截止时间是星期五下午。 | 测试由张伟负责截止时间是星期五下午 | 测试由张伟负责，截止时间是星期五下午。 | 测试由张伟负责截止时间是星期五下午 |
| 5-en | 会议与待办 / 干净 | Zhang Wei is responsible for testing. The deadline is Friday afternoon. | jihang wei is responsible for testing the deadline is friday afternoon | Jiang Wei is responsible for testing. The deadline is Friday afternoon. | jihang wei is responsible for testing the deadline is friday afternoon |
| 6-zh | 会议与待办 / 干净 | 今天的会议讨论预算、交付时间和质量问题。 | 今天的会议讨论预算交付时间和质量问题 | 今天的会议讨论预算交付时间和质量问题。 | 今天的会议讨论预算交付时间和质量问题 |
| 6-en | 会议与待办 / 干净 | Today's meeting covers the budget, delivery schedule, and quality issues. | today's meeting covers the budget delivery schedule and quality issues | Today's meeting covers the budget, delivery schedule, and quality issues. | today's meeting covers the budget delivery schedule and quality issues |
| 7-zh | 会议与待办 / 干净 | 请确认客户需求，然后更新项目计划。 | 请确认客户需求然后更新项目计划 | 请确认客户需求，然后更新项目计划。 | 请确认客户需求然后更新项目计划 |
| 7-en | 会议与待办 / 干净 | Please confirm the customer's requirements and then update the project plan. | please confirm the customer's requirements and then update the project plan | Please confirm the customer's requirements and then update the project plan. | please confirm the customer's requirements and then update the project plan |
| 8-zh | 硬件与技术 / 干净 | 这台服务器有八张 V100 显卡，每张显存三十二 GB。 | 这台服务器有8张 V100显卡每张显存32GB | 这台服务器有八张V100显卡，每张显存32GB。 | 这台服务器有8张 V100显卡每张显存32GB |
| 8-en | 硬件与技术 / 干净 | This server has eight V100 graphics cards, each with 32 GB of memory. | this server has 8 V100 graphics cards each with 32GB of memory | This server has eight V one zero zero graphics cards, each with thirty two GB of memory. | this server has 8 V100 graphics cards each with 32GB of memory |
| 9-zh | 硬件与技术 / 干净 | 使用 NVLink 连接显卡，网络带宽是五十六 Gbps。 | 使用 m v link 连接显卡网络带宽是56GBps | 使用NVLink连接显卡，网络带宽是56Gbps。 | 使用 m v link 连接显卡网络带宽是56GBps |
| 9-en | 硬件与技术 / 干净 | Connect the graphics cards using NVLink. The network bandwidth is 56 Gbps. | connect the graphics cards using envlink the network bandwidth is 56Gbps | Connect the graphics cards using NVLink. The network bandwidth is 56Gbps. | connect the graphics cards using envlink the network bandwidth is 56Gbps |
| 10-zh | 硬件与技术 / 干净 | 我们在 CPU 上运行离线语音识别，不使用 GPU。 | 我们在 c p u 上运行离线语音识别不使用 g p u | 我们在C P U上运行离线语音识别，不使用G P U。 | 我们在 c p u 上运行离线语音识别不使用 g p u |
| 10-en | 硬件与技术 / 干净 | We run offline speech recognition on the CPU without using a GPU. | we run offline speech recognition on the cpu without using a gpu | We run offline speech recognition on the CPU without using a GPU. | we run offline speech recognition on the cpu without using a gpu |
| 11-zh | 硬件与技术 / 干净 | 部署 DeepSeek 模型后，使用 vLLM 测试并发性能。 | 部署 deep c 模型后使用 v l l m 测试并发性能 | 部署DeepSeek模型后，使用VLM测试并发性能。 | 部署 deep c 模型后使用 v l l m 测试并发性能 |
| 11-en | 硬件与技术 / 干净 | After deploying the DeepSeek model, use vLLM to test concurrent performance. | after deploying the deepse model use vlllm to test concurrent performance | After deploying the deepseek model, use VLM to test concurrent performance. | after deploying the deepse model use vlllm to test concurrent performance |
| 12-zh | 数字与交易 / 干净 | 订单总金额是一千二百五十元，包含运费。 | 订单总金额是1250元包含运费 | 订单总金额是1250元，包含运费。 | 订单总金额是1250元包含运费 |
| 12-en | 数字与交易 / 干净 | The total order amount is 1250 yuan, including shipping. | the total order amount is 1250 yuan including shipping | The total order amount is one thousand two hundred and fifty yuan, including shipping. | the total order amount is 1250 yuan including shipping |
| 13-zh | 数字与交易 / 干净 | 请在十一月二十五日上午九点半参加会议。 | 请在11月25日上午九点半参加会议 | 请在11月25日上午九点半参加会议。 | 请在11月25日上午九点半参加会议 |
| 13-en | 数字与交易 / 干净 | Please attend the meeting at 9:30 AM on November 25. | please attend the meeting at 930 am on november 25 | Please attend the meeting at 9:30 a.m. on November 25th. | please attend the meeting at 930 am on november 25 |
| 14-zh | 数字与交易 / 干净 | 温度从二十五度上升到三十八度。 | 温度从25度上升到38度 | 温度从25度上升到38度。 | 温度从25度上升到38度 |
| 14-en | 数字与交易 / 干净 | The temperature rose from 25 degrees to 38 degrees. | the temperature rose from 25 degrees to 38 degrees | The temperature rose from 25 degrees to 38 degrees. | the temperature rose from 25 degrees to 38 degrees |
| 15-zh | 数字与交易 / 干净 | 这个季度收入增长了百分之十二点五。 | 这个季度收入增长了百分之12.5 | 这个季度收入增长了百分之12.5。 | 这个季度收入增长了百分之12.5 |
| 15-en | 数字与交易 / 干净 | Revenue increased by 12.5 percent this quarter. | revenue increased by 125 this quarter | Revenue increased by 12.5 this quarter. | revenue increased by 125 this quarter |
| 16-zh | 否定与逻辑 / 干净 | 不要删除原始文件，只删除临时文件。 | 不要删除原始文件只删除临时文件 | 不要删除原始文件，只删除临时文件。 | 不要删除原始文件只删除临时文件 |
| 16-en | 否定与逻辑 / 干净 | Do not delete the original files. Only delete the temporary files. | do not delete the original files only delete the temporary files | Do not delete the original files. Only delete the temporary files. | do not delete the original files only delete the temporary files |
| 17-zh | 否定与逻辑 / 干净 | 如果测试没有通过，就不能发布。 | 如果测试没有通过就不能发布 | 如果测试没有通过，就不能发布。 | 如果测试没有通过就不能发布 |
| 17-en | 否定与逻辑 / 干净 | If the tests fail, we cannot release the software. | if the tests fail we cannot release the software | If the tests fail, we cannot release the software. | if the tests fail we cannot release the software |
| 18-zh | 长句字幕 / 干净 | 为了保护用户隐私，我们在本地处理全部音频，并且只有用户点击导出后才会保存会议记录。 | 为了保护用户隐私我们在本地处理全部音频并且只有用户点击导出后才会保存会议记录 | 为了保护用户隐私，我们在本地处理全部音频，并且只有用户点击导出后才会保存会议记录。 | 为了保护用户隐私我们在本地处理全部音频并且只有用户点击导出后才会保存会议记录 |
| 18-en | 长句字幕 / 干净 | To protect user privacy, we process all audio locally and save meeting records only after the user clicks Export. | to protect user privacy we process all audio locally and save meeting records only after the user clicks export | To protect user privacy, we process all audio locally and save meeting records only after the user clicks export. | to protect user privacy we process all audio locally and save meeting records only after the user clicks export |
| 19-zh | 长句字幕 / 干净 | 虽然新的识别模型准确率更高，但是在普通电脑上运行速度较慢，所以我们先保留快速模型，再对完整句子进行复核。 | 虽然新的识别模型准确率更高但是在普通电脑上运行速度较慢所以我们先保留快速模型再对完整句子进行复合 | 虽然新的识别模型准确率更高，但是在普通电脑上运行速度较慢，所以我们先保留快速模型，再对完整句子进行复核。 | 虽然新的识别模型准确率更高但是在普通电脑上运行速度较慢所以我们先保留快速模型再对完整句子进行复合 |
| 19-en | 长句字幕 / 干净 | Although the new recognition model is more accurate, it runs more slowly on ordinary computers, so we keep the fast model and then review complete sentences. | although the new recognition model is more accurate it runs more slowly on ordinary computers so we keep the fast model and then review complete sentences | Although the new recognition model is more accurate, it runs more slowly on ordinary computers. So we keep the fast model and then review complete sentences. | although the new recognition model is more accurate it runs more slowly on ordinary computers so we keep the fast model and then review complete sentences |
| 20-zh | 中英混说 / 干净 | 请打开 Windows 设置，检查 microphone 权限，然后重新启动。 | 请打开 windows 设置检查 microphone 权限然后重新启动 | 请打开Windows设置，检查麦克风权限，然后重新启动。 | 请打开 windows 设置检查 microphone 权限然后重新启动 |
| 20-en | 中英混说 / 干净 | Please open Windows Settings, check microphone permissions, and restart. | please open windows settings check microphone permissions and restart | Please open Windows Settings, check microphone permissions, and restart. | please open windows settings check microphone permissions and restart |
| 21-zh | 中英混说 / 干净 | 这个项目完全 offline，不需要 API key，也不会上传 audio。 | 这个项目完全 offline 不需要 a p i key 也不会上传 audio | 这个项目完全 offline，不需要 API key，也不会上传 audio。 | 这个项目完全 offline 不需要 a p i key 也不会上传 audio |
| 21-en | 中英混说 / 干净 | This project is completely offline. It does not need an API key and does not upload audio. | this project is completely offline it does not need an api key and does not upload audio | This project is completely offline. It does not need an API key and does not upload audio. | this project is completely offline do not need an api key and does not upload audio |
| 22-zh | 中英混说 / 干净 | 今天先做 benchmark，明天再优化 latency 和 throughput。 | 今天先做 benchmark 明天再优化 latency 和 throughput | 今天先做benchmark，明天再优化 latency 和 throughput。 | 今天先做 benchmark 明天 在优化 latency 和 throughput |
| 22-en | 中英混说 / 干净 | Run the benchmark today, then optimize latency and throughput tomorrow. | run the benchmark today then optimize latency and throughput tomorrow | Run the benchmark today, then optimize latency and throughput tomorrow. | run the benchmark today then optimize latency and throughput tomorrow |
| 23-zh | 中英混说 / 干净 | 打开 winintel.com，下载数萌 AI 输入法。 | 打开 one until tell 点 com 下载数萌 a i 输入法 | 打开 winntel 点 com，下载数盟 AI 输入法。 | 打开 one until tell 点 com 下载数萌 a i 输入法 |
| 23-en | 中英混说 / 干净 | Open winintel.com and download the winintel AI input method. | openwinintelcom and download the wineintel ai input method | Open winintel.com and download the Winintel AI Input Method. | openwinintelcom and download the wineintel ai input method |
| 0-zh-noise | 日常交流 / 12dB白噪声 | 你好，请问附近哪里有地铁站？ | 你好请问附近哪里有地铁站 | 你好，请问附近哪里有地铁站？ | 你好请问附近哪里有地铁站 |
| 0-en-noise | 日常交流 / 12dB白噪声 | Hello, where is the nearest subway station? | hello where is the nearest subway station | Hello, where is the nearest subway station? | hello where is the nearest subway station |
| 4-zh-noise | 会议与待办 / 12dB白噪声 | 我们决定下周一发布新版本。 | 我们决定下周一发布新版本 | 我们决定下周一发布新版本。 | 我们决定下周一发布新版本 |
| 4-en-noise | 会议与待办 / 12dB白噪声 | We decided to release the new version next Monday. | we decided to release the new version next monday | We decided to release the new version next Monday. | we decided to release the new version next monday |
| 8-zh-noise | 硬件与技术 / 12dB白噪声 | 这台服务器有八张 V100 显卡，每张显存三十二 GB。 | 这台服务器有8张 V100显卡每张显存32G c | 这台服务器有八张D100显卡，每张显存32GB。 | 这台服务器有8张 V100显卡每张显存32G c |
| 8-en-noise | 硬件与技术 / 12dB白噪声 | This server has eight V100 graphics cards, each with 32 GB of memory. | this server has 8 V100 graphics cards each with 32GB of memory | This server has eight B one zero zero graphics cards, each with thirty two G B of memory. | this server has 8 V100 graphics cards each with 32GB of memory |
| 12-zh-noise | 数字与交易 / 12dB白噪声 | 订单总金额是一千二百五十元，包含运费。 | 订单总金额是1250元包含运费 | 订单总金额是1250元，包含运费。 | 订单总金额是1250元包含运费 |
| 12-en-noise | 数字与交易 / 12dB白噪声 | The total order amount is 1250 yuan, including shipping. | the total order amount is one thousand two hundred and fifty yuan including shipping | The total order amount is one thousand two hundred and fifty yuan, including shipping. | the total order amount is one thousand two hundred and fifty yuan including shipping |
| 16-zh-noise | 否定与逻辑 / 12dB白噪声 | 不要删除原始文件，只删除临时文件。 | 不要删除原始文件只删除临时文件 | 不要删除原始文件，只删除临时文件。 | 不要删除原始文件只删除临时文件 |
| 16-en-noise | 否定与逻辑 / 12dB白噪声 | Do not delete the original files. Only delete the temporary files. | do not delete the original files only delete the temporary files | Do not delete the original files. Only delete the temporary files. | do not delete the original files only delete the temporary files |
| 18-zh-noise | 长句字幕 / 12dB白噪声 | 为了保护用户隐私，我们在本地处理全部音频，并且只有用户点击导出后才会保存会议记录。 | 为了保护用户隐私我们在本地处理全部音频并且只有用户点击导出后才会保存会议记录 | 为了保护用户隐私，我们在本地处理全部音频，并且只有用户点击导出后才会保存会议记录。 | 为了保护用户隐私我们在本地处理全部音频并且只有用户点击导出后才会保存会议记录 |
| 18-en-noise | 长句字幕 / 12dB白噪声 | To protect user privacy, we process all audio locally and save meeting records only after the user clicks Export. | to protect user privacy we process all audio locally and save meeting records only after the user clicks export | To protect user privacy, we process all audio locally and save meeting records only after the user clicks export. | to protect user privacy we process all audio locally and save meeting records only after the user clicks export |
| 20-zh-noise | 中英混说 / 12dB白噪声 | 请打开 Windows 设置，检查 microphone 权限，然后重新启动。 | 请打开 windows 设置检查 microphone 权限然后重新启动 | 请打开 Windows 设置，检查 microphone 权限，然后重新启动。 | 请打开 windows 设置检查 microphone 权限然后重新启动 |
| 20-en-noise | 中英混说 / 12dB白噪声 | Please open Windows Settings, check microphone permissions, and restart. | please open windows settings check microphone permissions and restart | Please open Windows Settings, check microphone permissions, and restart. | please open windows settings check microphone permissions and restart |
| 23-zh-noise | 中英混说 / 12dB白噪声 | 打开 winintel.com，下载数萌 AI 输入法。 | 打开 y 点 com 下载数萌 a i 输入法 | 打开 yntel 点 com，下载树萌 AI 输入法。 | 打开 y 点 com 下载数萌 a i 输入法 |
| 23-en-noise | 中英混说 / 12dB白噪声 | Open winintel.com and download the winintel AI input method. | openwinintelt com and download the wine andtel ai input method | Open winintel.com and download the Winintel AI Input Method. | openwinintelt com and download the wine andtel ai input method |

## 翻译逐条结果

| ID | 场景 | 原文 | 参考译文 | 实际译文 | 暖机 ms | chrF2 | 语义评审 / 原因 |
|---|---|---|---|---|---:|---:|---|
| 0-zh-en | 日常交流 | 你好，请问附近哪里有地铁站？ | Hello, where is the nearest subway station? | Hello, where's the subway station? | 166 | 65.35 | 部分：地铁站问句保留，但遗漏附近这一位置限定。 |
| 0-en-zh | 日常交流 | Hello, where is the nearest subway station? | 你好，请问附近哪里有地铁站？ | 喂,最近的地铁站在哪里? | 158 | 12.76 | 完整：最近地铁站的询问保留；喂的语气不自然。 |
| 1-zh-en | 日常交流 | 我想订一间双人房，住三个晚上。 | I would like to book a double room for three nights. | I want to book a double room for three nights. | 221 | 77.81 | 完整：预订双人房及3晚住宿保留。 |
| 1-en-zh | 日常交流 | I would like to book a double room for three nights. | 我想订一间双人房，住三个晚上。 | 我想订1个双人房 住3个晚上 | 350 | 33.42 | 完整：预订双人房及3晚住宿保留。 |
| 2-zh-en | 日常交流 | 请把这份文件发到我的邮箱。 | Please send this document to my email address. | Please send this document to my mailbox. | 279 | 71.07 | 完整：发送指定文档至邮箱的动作保留。 |
| 2-en-zh | 日常交流 | Please send this document to my email address. | 请把这份文件发到我的邮箱。 | 请将此文档发送到我的电子邮件地址 。 | 411 | 14.89 | 完整：文档、发送及邮件地址保留。 |
| 3-zh-en | 日常交流 | 这道菜不要放辣椒，我对花生过敏。 | Please do not add chili to this dish. I am allergic to peanuts. | Don't put pepper on this dish. I'm allergic to peanuts. | 570 | 54.36 | 部分：否定和花生过敏保留，但辣椒译为泛指pepper，未明确chili。 |
| 3-en-zh | 日常交流 | Please do not add chili to this dish. I am allergic to peanuts. | 这道菜不要放辣椒，我对花生过敏。 | 请不要把辣椒加到这盘菜上我对花生过敏 | 602 | 32.28 | 完整：不要加辣椒及花生过敏保留。 |
| 4-zh-en | 会议与待办 | 我们决定下周一发布新版本。 | We decided to release the new version next Monday. | We've decided next Monday Publish a new version. | 696 | 54.89 | 部分：下周一、新版本、发布和决定均出现，但碎片语序使日期归属决定还是发布不清。 |
| 4-en-zh | 会议与待办 | We decided to release the new version next Monday. | 我们决定下周一发布新版本。 | 我们决定下周一发布新版本 | 266 | 92.01 | 完整：已决定下周一发布新版本。 |
| 5-zh-en | 会议与待办 | 测试由张伟负责，截止时间是星期五下午。 | Zhang Wei is responsible for testing. The deadline is Friday afternoon. | The test is performed by Zhang Wai, and the deadline is Friday afternoon. | 512 | 60.02 | 部分：测试责任和周五下午截止保留；张伟拼写成Zhang Wai。 |
| 5-en-zh | 会议与待办 | Zhang Wei is responsible for testing. The deadline is Friday afternoon. | 测试由张伟负责，截止时间是星期五下午。 | 张伟负责测试截止日期是星期五下午。 | 420 | 42.10 | 完整：张伟负责测试及周五下午截止保留。 |
| 6-zh-en | 会议与待办 | 今天的会议讨论预算、交付时间和质量问题。 | Today's meeting covers the budget, delivery schedule, and quality issues. | Today's meeting is devoted to the issues of budget, delivery time and quality. | 570 | 60.18 | 完整：今天会议的预算、交付时间、质量3项保留。 |
| 6-en-zh | 会议与待办 | Today's meeting covers the budget, delivery schedule, and quality issues. | 今天的会议讨论预算、交付时间和质量问题。 | 今天的会议涉及预算、交付时间表和质量问题。 | 355 | 57.04 | 完整：会议日期与3项讨论内容保留。 |
| 7-zh-en | 会议与待办 | 请确认客户需求，然后更新项目计划。 | Please confirm the customer's requirements and then update the project plan. | Please identify the client needs and then update the project plan. | 429 | 54.97 | 部分：先需求后计划的顺序保留，但确认需求译成识别需求，动作发生变化。 |
| 7-en-zh | 会议与待办 | Please confirm the customer's requirements and then update the project plan. | 请确认客户需求，然后更新项目计划。 | 请确认客户的要求,然后更新项目计划。 | 351 | 60.28 | 完整：确认客户要求后更新计划的顺序保留。 |
| 8-zh-en | 硬件与技术 | 这台服务器有八张 V100 显卡，每张显存三十二 GB。 | This server has eight V100 graphics cards, each with 32 GB of memory. | There are eight servers on this one. V100 graphics card Every one of them. 32GB. | 1027 | 45.57 | 失败：8张显卡错误变成8台服务器；32GB的显存归属也不明确。 |
| 8-en-zh | 硬件与技术 | This server has eight V100 graphics cards, each with 32 GB of memory. | 这台服务器有八张 V100 显卡，每张显存三十二 GB。 | 此服务器有 8 V100 显卡, 每个有 32GB 的显存 。 | 697 | 37.57 | 完整：单台服务器、8张V100及每张32GB显存保留。 |
| 9-zh-en | 硬件与技术 | 使用 NVLink 连接显卡，网络带宽是五十六 Gbps。 | Connect the graphics cards using NVLink. The network bandwidth is 56 Gbps. | Use NVLink Connection graphics card, the web bandwidth is... 56Gbps. | 1065 | 55.57 | 完整：NVLink连接显卡与56Gbps网络带宽均保留，语序生硬。 |
| 9-en-zh | 硬件与技术 | Connect the graphics cards using NVLink. The network bandwidth is 56 Gbps. | 使用 NVLink 连接显卡，网络带宽是五十六 Gbps。 | 使用 NVLink 连接显卡 。网络带宽为56Gbps。 | 785 | 68.38 | 完整：显卡连接方式及带宽和单位保留。 |
| 10-zh-en | 硬件与技术 | 我们在 CPU 上运行离线语音识别，不使用 GPU。 | We run offline speech recognition on the CPU without using a GPU. | We run offline voice recognition on CPU, not GPU. | 478 | 47.93 | 完整：CPU运行、离线识别及不使用GPU全部保留。 |
| 10-en-zh | 硬件与技术 | We run offline speech recognition on the CPU without using a GPU. | 我们在 CPU 上运行离线语音识别，不使用 GPU。 | 我们不用GPU在CPU上运行离线语音识别。 | 515 | 63.34 | 完整：CPU、GPU否定和离线识别全部保留。 |
| 11-zh-en | 硬件与技术 | 部署 DeepSeek 模型后，使用 vLLM 测试并发性能。 | After deploying the DeepSeek model, use vLLM to test concurrent performance. | Deployment DeepSeek After model, use vLLM Test concurrent performance. | 933 | 74.36 | 部分：品牌和并发性能保留，但碎片语序没有清晰表达先部署模型再测试的关系。 |
| 11-en-zh | 硬件与技术 | After deploying the DeepSeek model, use vLLM to test concurrent performance. | 部署 DeepSeek 模型后，使用 vLLM 测试并发性能。 | 部署后DeepSeek型号,使用vLLM要测试并发性能. | 1032 | 52.49 | 部分：DeepSeek、vLLM及并发测试保留；model译为型号，模型概念发生错译。 |
| 12-zh-en | 数字与交易 | 订单总金额是一千二百五十元，包含运费。 | The total order amount is 1250 yuan, including shipping. | The total amount of the order is... 1250 yuan, including freight. | 939 | 59.48 | 完整：订单总额1250元和包含运费均保留。 |
| 12-en-zh | 数字与交易 | The total order amount is 1250 yuan, including shipping. | 订单总金额是一千二百五十元，包含运费。 | 订货总额为1250元,包括货运。 | 414 | 22.23 | 完整：金额、币种与包含运费均保留。 |
| 13-zh-en | 数字与交易 | 请在十一月二十五日上午九点半参加会议。 | Please attend the meeting at 9:30 AM on November 25. | The meeting is invited to take place at 9:30 a.m. on 25 November. | 641 | 50.08 | 部分：11月25日9:30上午保留，但请参加会议错译成会议被邀请举行。 |
| 13-en-zh | 数字与交易 | Please attend the meeting at 9:30 AM on November 25. | 请在十一月二十五日上午九点半参加会议。 | 请出席11月25日上午9时30分的会议。 | 413 | 40.38 | 完整：参加会议、日期及上午9:30保留。 |
| 14-zh-en | 数字与交易 | 温度从二十五度上升到三十八度。 | The temperature rose from 25 degrees to 38 degrees. | Temperatures rose from 25 to 38 degrees. | 376 | 66.25 | 完整：从25度上升至38度保留。 |
| 14-en-zh | 数字与交易 | The temperature rose from 25 degrees to 38 degrees. | 温度从二十五度上升到三十八度。 | 温度从25度上升到38度。 | 343 | 100.00 | 完整：两端温度、方向及单位保留。 |
| 15-zh-en | 数字与交易 | 这个季度收入增长了百分之十二点五。 | Revenue increased by 12.5 percent this quarter. | Income increased by 12.5 per cent this quarter. | 322 | 85.23 | 完整：本季度收入增加12.5%保留。 |
| 15-en-zh | 数字与交易 | Revenue increased by 12.5 percent this quarter. | 这个季度收入增长了百分之十二点五。 | 本季度收入增加了12.5%。 | 256 | 29.62 | 完整：季度、收入、增长与比例保留。 |
| 16-zh-en | 否定与逻辑 | 不要删除原始文件，只删除临时文件。 | Do not delete the original files. Only delete the temporary files. | Do not delete original files, but only temporary files. | 372 | 65.74 | 完整：原始文件不删除、仅删除临时文件保留。 |
| 16-en-zh | 否定与逻辑 | Do not delete the original files. Only delete the temporary files. | 不要删除原始文件，只删除临时文件。 | 不删除原始文件 。只删除临时文件。 | 427 | 61.93 | 完整：否定对象和仅删除对象均正确。 |
| 17-zh-en | 否定与逻辑 | 如果测试没有通过，就不能发布。 | If the tests fail, we cannot release the software. | If the test is not passed, it cannot be published. | 414 | 32.32 | 完整：测试不通过就不能发布的条件保留。 |
| 17-en-zh | 否定与逻辑 | If the tests fail, we cannot release the software. | 如果测试没有通过，就不能发布。 | 如果测试失败,我们就不能释放软件。 | 316 | 19.52 | 部分：条件和否定保留，但软件发布译作释放软件，技术动作不准确。 |
| 18-zh-en | 长句字幕 | 为了保护用户隐私，我们在本地处理全部音频，并且只有用户点击导出后才会保存会议记录。 | To protect user privacy, we process all audio locally and save meeting records only after the user clicks Export. | To protect user privacy, we process all audio locally and only after the user clicks to export the meeting records. | 772 | 87.45 | 部分：本地处理全部音频保留；保存会议记录的动词被遗漏，只有导出后保存的完整关系未表达。 |
| 18-en-zh | 长句字幕 | To protect user privacy, we process all audio locally and save meeting records only after the user clicks Export. | 为了保护用户隐私，我们在本地处理全部音频，并且只有用户点击导出后才会保存会议记录。 | 为了保护用户隐私,我们处理所有本地音频,仅在用户点击导出后保存会议记录。 | 699 | 44.00 | 部分：导出后保存记录保留；本地处理所有音频变成处理所有本地音频，处理地点的关系变化。 |
| 19-zh-en | 长句字幕 | 虽然新的识别模型准确率更高，但是在普通电脑上运行速度较慢，所以我们先保留快速模型，再对完整句子进行复核。 | Although the new recognition model is more accurate, it runs more slowly on ordinary computers, so we keep the fast model and then review complete sentences. | Although the new identification model is more accurate, it is running slowly on the common computer, so we retain the fast model before reviewing the entire sentence. | 999 | 58.88 | 完整：精度提高但普通电脑速度较慢、保留快模型后复核整句的主旨保留。 |
| 19-en-zh | 长句字幕 | Although the new recognition model is more accurate, it runs more slowly on ordinary computers, so we keep the fast model and then review complete sentences. | 虽然新的识别模型准确率更高，但是在普通电脑上运行速度较慢，所以我们先保留快速模型，再对完整句子进行复核。 | 虽然新的识别模式更准确,但它在普通计算机上运行得更慢, 所以我们保留快速模型, 然后审查完整句子。 | 939 | 29.20 | 部分：精度、速度和两阶段处理保留；recognition model译作识别模式而非模型。 |
| 20-zh-en | 中英混说 | 请打开 Windows 设置，检查 microphone 权限，然后重新启动。 | Please open Windows Settings, check microphone permissions, and restart. | Open it, please. Windows Settings, check microphone Permissions, and then restart. | 1157 | 85.62 | 完整：Windows设置、检查麦克风权限和重启全部保留，句子被切碎。 |
| 20-en-zh | 中英混说 | Please open Windows Settings, check microphone permissions, and restart. | 请打开 Windows 设置，检查 microphone 权限，然后重新启动。 | 请打开 Windows 设置, 检查麦克风权限, 并重新启动 。 | 699 | 41.37 | 完整：设置、权限和重启流程保留。 |
| 21-zh-en | 中英混说 | 这个项目完全 offline，不需要 API key，也不会上传 audio。 | This project is completely offline. It does not need an API key and does not upload audio. | This project is completely... offline No, I don't need it. API key And they won't upload it. audio. | 1518 | 60.25 | 部分：关键词和否定存在，但项目主体被改成I/they，碎片句不能完整表达项目无需密钥且不上传音频。 |
| 21-en-zh | 中英混说 | This project is completely offline. It does not need an API key and does not upload audio. | 这个项目完全 offline，不需要 API key，也不会上传 audio。 | 这个项目完全离线了它不需要 API 密钥, 也不上传音频 。 | 757 | 25.80 | 完整：项目离线、无需API密钥及不上传音频保留。 |
| 22-zh-en | 中英混说 | 今天先做 benchmark，明天再优化 latency 和 throughput。 | Run the benchmark today, then optimize latency and throughput tomorrow. | Let's do it today. benchmark Tomorrow, we'll optimize it. latency and throughput. | 1293 | 65.63 | 部分：今天、明天、benchmark、latency及throughput存在，但任务与日期被断成碎片，指令关系不清。 |
| 22-en-zh | 中英混说 | Run the benchmark today, then optimize latency and throughput tomorrow. | 今天先做 benchmark，明天再优化 latency 和 throughput。 | 运行基准测试今天,然后优化延迟和吞吐量明天见 | 819 | 4.83 | 部分：基准测试与明天优化两项保留，但新增原文没有的明天见。 |
| 23-zh-en | 中英混说 | 打开 winintel.com，下载数萌 AI 输入法。 | Open winintel.com and download the winintel AI input method. | Open winintel.com, Download winintel AI input method. | 776 | 79.11 | 完整：域名、打开、下载及数萌品牌保留。 |
| 23-en-zh | 中英混说 | Open winintel.com and download the winintel AI input method. | 打开 winintel.com，下载数萌 AI 输入法。 | 打开winintel.com并下载数萌 AI 输入法. | 884 | 79.30 | 完整：域名、下载动作及品牌保留。 |

## 字幕端到端语义评审

| ID | 结论 | 依据 |
|---|---|---|
| 0-zh | 失败 | ASR词语正确但未恢复问句标点；附近哪里有地铁站被翻译成附近有地铁站的陈述。 |
| 0-en | 完整 | 最近地铁站的询问完整保留。 |
| 4-zh | 部分 | 日期和发布存在，但分段译文使决定与下周一发布的关系不清。 |
| 5-en | 部分 | ASR把Zhang Wei识别为jihang wei，译文进一步变为郑惠；截止日期变成周五下午负责测试。 |
| 8-zh | 失败 | ASR正确识别8张V100和32GB，译文却变成8台服务器及显卡可见。 |
| 9-en | 部分 | 连接与56Gbps保留；ASR的envlink被原样传入译文，未正确恢复NVLink。 |
| 12-zh | 完整 | 订单总额1250元与包含运费保留。 |
| 13-en | 失败 | ASR输出930 am且未恢复冒号；译文遗漏11月25日，新增会议开始之前的关系。 |
| 16-zh | 完整 | 不删除原始文件、只删除临时文件保留。 |
| 17-en | 部分 | 测试失败的否定条件保留，软件发布仍译成释放软件。 |
| 18-zh | 完整 | 保护隐私、本地处理全部音频、仅点击导出后保存记录的逻辑保留。 |
| 19-en | 完整 | 精度与速度权衡、快模型后整句复核保留。 |
| 20-zh | 完整 | 设置、麦克风权限和重启动作保留，语序生硬。 |
| 21-en | 完整 | 离线、无需API密钥及不上传音频完整保留。 |
| 22-zh | 部分 | ASR的再优化变成在优化；译文碎片使今天基准、明天优化的安排不清。 |
| 23-en | 失败 | ASR丢失域名分隔并误识别品牌；翻译成网络与葡萄酒英特尔，域名和品牌事实错误。 |
