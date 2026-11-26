# 第三方组件与模型说明

数萌 AI 输入法应用代码按 GPL-3.0-or-later 发布。以下开源依赖和模型保留原授权，不声称它们由数萌自主训练或开发。没有付费 API 依赖。

| 组件 | 来源 | 许可 / 说明 |
|---|---|---|
| 原始词典数据 | [rime-ice](https://github.com/iDvel/rime-ice) | GPL-3.0；固定来源与提交见 `data/rime-ice/provenance.json` |
| sherpa-onnx 2.0.3 | [k2-fsa/sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx) | Apache-2.0，许可证随平台安装包提供 |
| Zipformer 中英流式模型 | sherpa-onnx 模型发布 | 清单、来源及摘要见 `models/voice` |
| SenseVoiceSmall INT8 | FunAudioLLM / FunASR、sherpa-onnx 转换 | FunASR 模型协议 1.1，见 `models/sensevoice/MODEL_LICENSE` |
| OPUS-MT 中英翻译模型 | Helsinki-NLP、Xenova ONNX 转换 | Apache-2.0；模型文件摘要见 `windows/Legal/Translation-notice.md` |
| Transformers.js 3.8.1 | Hugging Face | Apache-2.0 |
| ONNX Runtime | Microsoft | MIT；第三方声明随安装包提供 |
| Node.js | Node.js 项目 | MIT 及所含组件条款；仅 Windows 运行包需要 |
| ws | websockets/ws | MIT |
| Kotlin stdlib 1.9.24 | JetBrains | Apache-2.0；Android 语音运行库依赖 |

词典数据用于生成自研二进制索引。当前产品不依赖 Rime / 小狼毫的进程、DLL、配置窗口或网络服务。

Windows 许可证目录：`windows/Legal`；Android：`android/licenses`。模型权重随安装包分发而非存入 Git；构建脚本复用已安装的模型或提取官方 APK 中的固定模型。开发阶段获取依赖需要网络，产品运行时的输入、识别与翻译保持离线。
