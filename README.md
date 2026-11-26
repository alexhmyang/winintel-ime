# 数萌 AI 输入法 · winintel AI

开源的 Windows / Android 系统输入法。使用本地拼音词库、自研候选排序与离线语音识别，输入内容和录音无需上传；核心功能不依赖付费 API。

**[下载安装包](https://github.com/alexhmyang/winintel-ime/releases/tag/v0.5.4)** · **[中文旁白产品演示](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.4/winintel-ime-0.5.4-demo.mp4)** · [官网](https://winintel.com) · [Bilibili](https://space.bilibili.com/429959998) · [YouTube](https://www.youtube.com/@winintelcom)

## 下载与功能

| 平台 | 安装包 | 功能 |
|---|---|---|
| Windows 10 / 11，64 位 | [0.5.4 EXE](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.4/winintel-ime-0.5.4-setup.exe) | 拼音、英文直通、Shift 快切、智能中英候选、流式语音、实时翻译、双语字幕与会议记录 |
| Android 8.0+，arm64 手机 | [0.1.1 APK](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.4/winintel-ime-android-0.1.1.apk) | 系统拼音键盘、英文切换、选词学习、离线语音草稿与笔记；包含 x86_64 模拟器运行库 |

Windows 包含 x64 与 x86 TSF 组件，支持 64 位系统中的两种应用。两平台独立编号；Android 采用停顿后识别，实时翻译和 SRT 导出目前仅在 Windows 提供。

Release 提供完整源码 ZIP、SHA-256 校验文件、验证报告、1080p 中文旁白视频和字幕。模型与运行库随安装包提供，不放入 Git 源码仓库。

## Windows 安装与使用

1. 下载并运行 EXE，按提示授予管理员权限。
2. 用 **Win + 空格**选择“数萌 AI 输入法”。必要时在“设置 → 时间和语言 → 语言和区域 → 中文 → 语言选项”中添加。
3. 打开记事本输入 `nihao`，空格选择“你好”；数字 **1–9**选词，**PageDown / =**下一页，**PageUp / -**上一页。
4. 单独按下并松开 **Shift**切换中英文；长按只切换一次，Shift + 字母 / Ctrl 等组合不切换语言。切换时保留未提交字母，手动选择不会被智能判断改回。
5. 智能模式根据本地规则识别英文词、技术名称和网址，如 `hello`、`github`、`python`；拼音显示中文候选。短串可能有歧义，用 Shift 明确选择。工具栏语言按钮可切换，**Ctrl + 点击语言按钮**恢复智能模式。
6. 左键托盘图标展开迷你工具栏；右键打开语音、实时翻译与设置面板。

升级后关闭并重新打开已有应用，以加载新版组件。候选、工具栏和语音草稿不抢焦点；翻译工作区是普通独立应用窗口。

### 实时翻译、字幕与会议

右键托盘 → **实时语音翻译**，按 **F8**或录音按钮开始，再按一次停止。选择自动判断、中译英或英译中。

开启自动翻译后，讲话中显示临时识别与译文，停顿后更新整句。临时识别每 650 毫秒提交最新文字；待处理预览只保留最新一条，旧译文不会覆盖定稿。首次使用需要加载本地模型。

“字幕与会议”页可导出带原文、译文和时间戳的 **SRT 双语字幕**与 **Markdown 会议记录**。普通语音输入先进入草稿，停止后可编辑，确认后上屏。

## Android 安装与使用

1. 安装 APK，打开“数萌 AI 输入法”。
2. 点击“启用数萌输入法”，在系统设置中开启。
3. 点击“切换到数萌输入法”，选择数萌键盘。
4. 在首页、微信或浏览器中输入拼音；点击候选选词，横向滑动查看更多，`›` 翻页，空格选首项，语言按钮切换中英文。
5. 授予麦克风权限后点击“语音”，停顿后识别；停止后确认草稿上屏。首页支持离线语音笔记和手动保存 TXT。

隐藏键盘或离开输入框会停止录音并忽略迟到结果。密码框禁用语音且不学习选词。APK 不申请 INTERNET 权限；实际速度取决于手机 CPU 与系统环境。

## 原理与隐私

| 模块 | 实现 |
|---|---|
| 系统接入 | Windows 原生 TSF；Android 原生 InputMethodService |
| 拼音与排序 | 本地二进制词库、内存映射查询、词频、匹配度、纠错代价、本机选词历史 |
| 英文判断 | 本地英文词表、拼音候选与网址规则；无法准确预测全部中英歧义 |
| Windows 语音 | sherpa-onnx Zipformer 流式识别与 SenseVoiceSmall 整句复核，独立工作进程 |
| Android 语音 | sherpa-onnx SenseVoiceSmall INT8，独立 `:voice` 进程 |
| Windows 翻译 | OPUS-MT 中英模型、Transformers.js、ONNX Runtime，CPU INT8 |

运行时不调用在线排序、语音或翻译 API。音频在内存中处理，不自动保存录音；选词历史留在本机。主动导出的字幕、会议记录与笔记由用户管理。开发阶段下载工具、依赖与模型需要网络，安装后的核心功能可离线运行。

产品不依赖 Rime / 小狼毫的程序、DLL 或配置窗口；使用开源词典数据不意味着依赖 Rime 输入法程序。第三方模型、词库与运行库保留各自许可。

## 在本机资源管理器中打开

完整项目位于 `winintel-ime-current`。上一级目录的 `数萌AI输入法.code-workspace` 可用 TRAE / VS Code 打开，左侧资源管理器将显示整个项目。

从 GitHub 获取：

```powershell
git clone https://github.com/alexhmyang/winintel-ime.git
```

## 编译 Windows

需要 Visual Studio 2022 C++ Build Tools、Windows SDK、.NET Framework 4.x 和 NSIS 3。先安装官方 Windows 包以取得离线资源，或解包后通过 `-AssetRoot` 指定。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\windows\BuildRelease.ps1
```

自动查找 MSVC、SDK、NSIS 与本机已安装资源，每次重新生成词库并编译 x64 / x86 组件和桌面程序。

```powershell
.\windows\BuildRelease.ps1 -AssetRoot "D:\winintel-assets" -Nsis "C:\Program Files (x86)\NSIS\makensis.exe" -Output "D:\packages"
```

支持 `-VcRoot`、`-SdkRoot`、`-BuildRoot` 和 `-SkipInstaller`。默认缓存在 `%TEMP%\winintel-ime-release-windows`，输出到仓库上一级目录。`BuildRelease.ps1` 是本次发布入口；早期脚本与测试资料用于版本追溯。

## 编译 Android

需要 JDK 17+、Node.js 22+。构建使用 Java 8 字节码、SDK 35、sherpa-onnx 2.0.3 AAR、Kotlin stdlib 1.9.24，不依赖 Gradle。

```powershell
.\android\tools\PrepareRelease.ps1 -PublishedApk "D:\downloads\winintel-ime-android-0.1.1.apk"
.\android\BuildRelease.ps1 -JavaHome "C:\Program Files\Java\jdk-20" `
  -KeyStore "D:\private\release.jks" -PasswordFile "D:\private\password.txt" `
  -Output "D:\packages"
```

准备脚本将工具缓存到 `%LOCALAPPDATA%\winintel-build\android-tools`，模型从官方 APK 提取到 `%LOCALAPPDATA%\winintel-build\android-models` 并校验 SHA-256。

签名密钥别名为 `winintel`；首次开发可自行创建密钥，覆盖安装要求相同签名。密钥与密码文件必须位于仓库外，可用 `WININTEL_KEYSTORE` / `WININTEL_KEY_PASSWORD_FILE` 环境变量指定。支持 `-ToolRoot`、`-ModelRoot`、`-BuildRoot`。默认构建缓存 `%TEMP%\winintel-ime-release-android`，自动验证 APK 签名及 16 KB 页对齐。

## 源码目录

| 路径 | 内容 |
|---|---|
| `windows/App` | 托盘、设置、语音草稿、中英模式、翻译、字幕与会议 |
| `windows/Native` | TSF、候选窗口、键盘处理与本地排序 |
| `windows/BuildRelease.ps1` | Windows 发布构建入口 |
| `standalone` | 拼音引擎、词库生成、语音与翻译工作线程 |
| `desktop` | 语音会话与子进程生命周期 |
| `android/src` | 系统键盘、语音服务、笔记与设置 |
| `android/BuildRelease.ps1` | Android 发布构建入口 |
| `data` | 原始词典与固定来源 |
| `models` / `windows/Legal` / `android/licenses` | 模型清单与第三方许可 |
| `windows/Verification` / `android/verification` | 功能验证与历史基线 |

## 许可证与联系

应用代码 **GPL-3.0-or-later**；第三方模型与依赖采用各自许可，见 [第三方说明](THIRD_PARTY_NOTICES.md) 和 [贡献说明](CONTRIBUTING.md)。安装包包含相应许可证。

作者：**数萌AI服务器 (winintel)** · 联系：**service@winintel.com**

问题与建议：[GitHub Issues](https://github.com/alexhmyang/winintel-ime/issues)
