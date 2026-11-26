# 数萌 AI 输入法 · Android 0.1.1

[下载安装 APK](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.4/winintel-ime-android-0.1.1.apk) · [完整项目说明](../README.md)

Android 8.0+，支持 arm64 手机和 x86_64 模拟器。原生系统键盘、本地拼音词库、英文切换、选词学习、离线 SenseVoiceSmall INT8、语音草稿和文字笔记。APK 无 INTERNET 权限。

安装后打开应用，依次点击“启用数萌输入法”“切换到数萌输入法”。键盘点击候选选词，横向滑动查看更多，`›` 翻页，空格选首项。语言按钮切换中英文。

授权麦克风后点击“语音”，停顿后识别，再次点击停止；确认草稿后上屏。首页可以保存文字笔记。音频只在内存中处理；密码框禁用语音并不学习选词。隐藏键盘或离开输入目标会取消当前录音和迟到回调。

Android 与 Windows 独立编号。本次 0.1.1 完善源码打包、Manifest 和发布构建流程；识别仍采用停顿后处理，Windows 的实时翻译与 SRT 功能尚未移植。

## 编译

需要 JDK 17+、Node.js 22+。SDK 35、sherpa-onnx 2.0.3 AAR 和 Kotlin stdlib 1.9.24 由准备脚本下载并缓存在源码目录外；模型从官方 APK 提取并校验。

```powershell
.\tools\PrepareRelease.ps1 -PublishedApk "D:\downloads\winintel-ime-android-0.1.1.apk"
.\BuildRelease.ps1 -JavaHome "C:\Program Files\Java\jdk-20" `
  -KeyStore "D:\private\release.jks" -PasswordFile "D:\private\password.txt" `
  -Output "D:\packages"
```

密钥别名为 `winintel`，签名密钥与密码文件必须放在仓库外。覆盖安装要求相同签名。脚本自动生成词库、编译 Java / DEX、打包模型和两种 ABI、签名并验证 16 KB 页对齐。

应用代码：GPL-3.0-or-later。模型和运行库遵守各自授权，完整许可在 `licenses` 和安装包 assets 中。
