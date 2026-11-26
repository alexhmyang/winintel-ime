# 离线翻译依赖与版权

翻译模型：Helsinki-NLP OPUS-MT `opus-mt-zh-en`、`opus-mt-en-zh`；ONNX 转换版本由 Xenova 发布。模型依据原模型 Apache-2.0 许可分发；原模型说明和转换说明位于 `models/translation`。

Transformers.js 3.8.1：Hugging Face，Apache-2.0。ONNX Runtime：Microsoft，MIT。完整许可文本和第三方声明随包提供，node_modules 同时保留依赖包许可证。

已校验模型下载响应中的 SHA-256：

| 模型 / 文件 | SHA-256 |
|---|---|
| zh-en encoder_model_quantized.onnx | 84d5e171b626bc8b6b220d022ac58696e9528c25deeacca62b5cbf4364547a99 |
| zh-en decoder_model_merged_quantized.onnx | c6b7f04ff1ba0fbd1bf6852599b4c0cad6fe512d57cd887f44ef36cf705424cb |
| en-zh encoder_model_quantized.onnx | d3b7912bf6a9bd27e4c074c2df91d4ff3d5b4bc5f7f6c8d7cc9c805c98fbafee |
| en-zh decoder_model_merged_quantized.onnx | 023be4f841f4c47cd65fffcbaa81c0d99d7f7e0138f7ba0e03fa220a4e688aff |

本项目自研桌面交互、队列、字幕和导出逻辑；不声称第三方模型权重为数萌自主训练。没有付费 API 依赖，运行时禁用远程模型下载。
