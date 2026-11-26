# SenseVoiceSmall 离线整句复核

模型来源和固定版本见 model-manifest.json。模型权重适用随附 MODEL_LICENSE，不适用项目代码的 GPL。不得将该模型宣称为数萌自训练或自主产权。

运行 windows/AcquireSenseVoice.ps1 -AssetRoot <基础运行资产目录>，下载并校验标准 SenseVoiceSmall INT8 及词表，再将该目录传给 windows/Build.ps1 的 AssetRoot 参数。默认 CPU、自动语言、逆文本标准化。未使用粤语微调版本。

识别音频只在本机两个独立工作线程中处理：Zipformer 实时预览，SenseVoice 完整语句复核；不向任何 API 上传音频或自动提交文字。
