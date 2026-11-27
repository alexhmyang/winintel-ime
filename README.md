# 数萌 AI 输入法 · winintel AI

开源的 Windows / Android 系统输入法。使用本地拼音词库、自研候选排序与离线语音识别，输入内容和录音无需上传；核心功能不依赖付费 API。

[English](README.en.md)

最新发布：Windows **0.5.11** / Android **0.1.5**。新增长词与扩展词库、绿色反色候选高亮；Android 去除编辑和发送按钮，语音草稿直接点选编辑，修复同一输入框重启连接导致拼音丢失，并在本机保存纠正词和连续输入的新词。

**[下载安装包](https://github.com/alexhmyang/winintel-ime/releases/tag/v0.5.11)** · **[中文旁白产品演示（0.5.4）](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.4/winintel-ime-0.5.4-demo.mp4)** · [官网](https://winintel.com) · [Bilibili](https://space.bilibili.com/429959998) · [YouTube](https://www.youtube.com/@winintelcom)

## 下载与功能

| 平台 | 安装包 | 功能 |
|---|---|---|
| Windows 10 / 11，64 位 | [0.5.11 EXE](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.11/winintel-ime-0.5.11-setup.exe) | 扩展长词、绿色候选高亮、长拼音编辑、个人词库、英文补全、实时新闻、离线语音与实时翻译 |
| Android 8.0+，arm64 手机 | [0.1.5 APK](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.11/winintel-ime-android-0.1.5.apk) | 九宫格与全键盘、大按键与绿色候选、直接编辑、本机纠正记词、英文补全、实时新闻、离线语音草稿与笔记；包含 x86_64 运行库 |

Windows 包含 x64 与 x86 TSF 组件，支持 64 位系统中的两种应用。两平台独立编号；Android 采用停顿后识别，实时翻译和 SRT 导出目前仅在 Windows 提供。

Release 提供完整源码 ZIP、SHA-256 校验文件和验证报告；中文旁白视频与字幕见 0.5.4 演示。模型与运行库随安装包提供，不放入 Git 源码仓库。

### 本次更新与验证

新增 ext、tencent、others、41448 词典源，整合 1,751,276 条全拼与简拼索引记录。手机候选使用绿色白字，按键按下变深绿色；语言、数字、符号分类与录音状态会高亮。回车统一为“换行”。同一输入框重启连接时保留待选拼音，纠正和连续中文组合词保存在本机，进程重启后可继续使用。Windows 缓存长拼音分段查词，改善句中编辑响应。

| 验证项目 | 结果 |
|---|---|
| Android 词库、九宫格与个人记词 | JVM 18 项通过，192MB 堆上限 |
| Android 编辑与输入连接 | 最终 APK 27 项通过 |
| Android 独立应用交互 | 14 项通过，含每次上屏重启连接、纠正长词与进程重启保留 |
| Windows 界面与输入 | 57 项通过；另有 7 个引擎基础与错拼样例通过 |
| Android 发布包 | 原签名，v2/v3 与 16KB 对齐校验通过 |

详细资料：[升级说明](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.11/winintel-ime-0.5.11-upgrade-guide.md) · [验证报告](https://github.com/alexhmyang/winintel-ime/releases/download/v0.5.11/winintel-ime-0.5.11-release-report.md)。手机交互使用 x86_64 模拟器和独立测试应用验证，微信等真机版本仍需实际使用验证。覆盖安装保留个人词库，卸载或清除应用数据会删除记录。

## Windows 安装与使用

1. 下载并运行 EXE，按提示授予管理员权限。
2. 用 **Win + 空格**选择“数萌 AI 输入法”。必要时在“设置 → 时间和语言 → 语言和区域 → 中文 → 语言选项”中添加。
3. 打开记事本输入 `nihao`，空格选择“你好”；数字 **1–9**选词，**PageDown / =**下一页，**PageUp / -**上一页。
4. 单独按下并松开 **Shift**切换中英文；长按只切换一次，Shift + 字母 / Ctrl 等组合不切换语言。切换时保留未提交字母，手动选择不会被智能判断改回。
5. 智能模式根据本地规则识别英文词、技术名称和网址，如 `hello`、`github`、`python`；拼音显示中文候选。短串可能有歧义，用 Shift 明确选择。工具栏语言按钮可切换，**Ctrl + 点击语言按钮**恢复智能模式。
6. 左键托盘图标展开迷你工具栏；右键打开语音、实时翻译与设置面板。

升级后关闭并重新打开已有应用，以加载新版组件。候选、工具栏和语音草稿不抢焦点；翻译工作区是普通独立应用窗口。

### Windows 0.5.8：英文补全、双语热词与每日新闻

本版本新增 22,437 项英文前缀词库：EN 模式输入 `hel`，按 **Tab** 补全 `hello`；上下键选择，空格输入原词与空格，数字与快捷键保持正常用途。中文新增智能体、具身智能等精选热词，英文支持 DeepSeek、NVLink、InfiniBand、vLLM 等专业名称。

右键托盘 → **每日新闻与热词**，查看 IT之家与 NPR 世界新闻，默认每 **5 分钟**刷新，支持手动更新、RSS / Atom、离线缓存以及自有 HTTPS `/v1/news` 接口，接口失败可回退免费源。文章发布日期与下载时间分别显示。精选热词包为 2026-11-26 版本，可通过自有后台 `/v1/hotwords` 更新；公网后台需要自行部署。详见 [使用说明](windows/English-news-guide.md)。

### Windows 0.5.7：词库服务、表情与软件助手

右键托盘打开工具箱，新增 **表情与符号**、**软件助手**、**服务与赞助**。表情面板提供 6 类 Emoji、数学符号、箭头与颜文字；先点击软件的输入框，再从面板选择，面板保持输入焦点。通过标准 TSF 接入 x64 / x86 应用，通过 Unicode 插入完整表情；具体字体效果由目标软件决定。管理员权限运行的软件需要权限匹配。

软件助手支持 `微信找 张三`、`打开微信`、`打开记事本`、`切换到 Excel`，也可切换 Word 和浏览器。微信应已登录并处于主界面，优先使用辅助功能控件，无法读取控件时使用 Windows 本机 OCR 识别搜索框和结果。同名或未匹配结果留给用户选择。软件指令不会发送聊天消息。语音说“软件指令，微信找 张三”，停止录音并确认后进入助手，点击执行。普通语音继续进入文字草稿。

商业模式采用 **GPL 免费客户端 + 企业词库托管、部署和技术支持收费**。核心输入和全部本地功能免费；服务端源码同样开放，企业可自建。API 仅在用户手动同步时下载词库，不上传按键、选词记录、语音或联系人。同步成功后离线使用，失败保留旧词库；个人词库与学习记录优先。设置支持服务地址、词库 ID 和本机加密保存的访问密钥。详见 [服务部署说明](service/README.md)。本次提供可部署服务端和客户端接入，公网服务、在线支付及自动扣费尚未上线。

工具箱预留标明“广告”的赞助位，默认关闭，不在候选框显示。赞助内容来自本机配置，用户主动点击才打开网站；无广告 SDK、追踪或自动请求。

兼容性实测：普通输入框、富文本框与未上屏拼音后插入 emoji 已通过。本机微信 4.2.0.53 已能识别带放大镜图标的搜索标签，但联系人对话自动打开未通过端到端验证；窗口切换时助手会停止。微信、TRAE 和 Office 各版本的全面输入兼容性需要逐项实测。

### Windows 0.5.6：简拼与个人词库

当前简拼索引最多保留 192 个候选，完整拼音最多保留 96 个候选，界面仍按每页 9 个显示。完整拼音选词后，按实际读音自动学习对应简拼，支持多音字匹配；后续输入简拼时优先显示近期选词。

打开 **设置 → 键盘输入 → 个人词库**，填写姓名或专业词。拼音自动填入，多音字可手动修改，用空格分隔每个字的拼音。保存后完整拼音与首字母简拼立即可查，支持删除。个人词库和学习记录只保存在本机，不进入公共词库。简拼可能对应多个姓名，首次需要明确选择或添加，无法仅靠几个字母确定具体汉字。

### Windows 0.5.5：长拼音编辑

未上屏的拼音支持最多 256 个字母。按 **← / →**移到错误位置，直接补字母；**Home / End**移到开头或末尾，**Backspace**删除左侧，**Delete**删除右侧。**Ctrl + Z**撤销，**Ctrl + Y / Ctrl + Shift + Z**重做，最多保留 32 步；上屏或取消后清空编辑历史。没有待选拼音时，快捷键交给当前应用。

也可点击候选框上方的拼音定位。蓝色光标和位置计数显示当前编辑点，长串自动横向滚动，修改后重新计算整串候选。已上屏的文字仍在当前应用中编辑。

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

EN 模式输入 `hel` 后点击 `hello` 补全，**Space**保留原词与空格。首页或键盘“新闻”打开实时新闻面板：默认关闭，勾选启用并保存后在该页面每 5 分钟刷新；离开页面停止定时刷新，缓存离线可看。支持免费 IT之家 / NPR 和自有 HTTPS `/v1/news`，可配置失败回退。安装包内置同一份中英文精选词库，覆盖升级使用原签名并保留本机选词记录。

隐藏键盘或离开输入框会停止录音并忽略迟到结果。密码框禁用语音、补全和选词学习。APK 的 INTERNET 权限仅供主动开启的新闻使用；输入和录音不上传。实际语音速度取决于手机 CPU 与系统环境。

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

Copyright (c) 2026 数萌AI服务器 (winintel) / Alex YANG。源码保留版权与许可声明；分发修改版须遵守 GPL 并提供对应源码。作者署名、品牌及独立宣传素材授权见 [作者与版权](AUTHORS.md)。

问题与建议：[GitHub Issues](https://github.com/alexhmyang/winintel-ime/issues)
