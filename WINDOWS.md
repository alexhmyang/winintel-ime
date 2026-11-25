# 数萌AI输入法 Windows 安装版

品牌：**winintel AI**。作者：数萌AI服务器 (winintel)。

从 [GitHub Releases](https://github.com/alexhmyang/winintel-ime/releases) 下载 `winintel-AI-Input-Setup-0.1.0.exe`。安装包包含 Windows 托盘程序、Node.js、本地中英文词库和 INT8 流式语音模型，无需客户安装 Node.js 或 Python。

双击安装，保留“注册 Windows 系统输入法”，首次安装允许 Windows 管理员授权。完成后右下角出现“萌”图标。按 **Win + 空格**选择“小狼毫”，按 **Ctrl + `**选择“数萌AI输入法”。中文模式输入拼音、简拼或英文词汇，空格或数字键选词；Shift 切换英文直输。

语音输入：点击目标软件的文本框，按 **F8**开始，再按 **F8**结束。右下角浮窗实时展示识别文字；识别句子提交到原文本框。窗口或焦点变化时，识别结果留在托盘设置页。普通权限的托盘无法向管理员权限的程序注入文字。

双击托盘打开设置，可启用登录启动、查看识别记录、修复输入方案。关闭设置窗口后仍在托盘运行，退出使用托盘菜单。

键盘的基础联想、纠错和语音离线可用。**大模型排序需要另行安装 Ollama 和所选模型**，默认模型名称为 `qwen3:0.6b`。设置中启用后只访问本机 Ollama，模型超时回退本地候选。

本版本面向 Windows 10/11 x64，建议 4 GB 内存和 1 GB 可用空间。它是可安装的首版测试包，采用 Rime/小狼毫作为系统输入宿主，语言栏显示“小狼毫”。安装 EXE 暂未签名。没有搜狗同条件对照数据，不能据此声称准确率或速度已超过搜狗。

已有 Rime YAML 配置会结构化保留并创建备份，数萌方案添加到原方案列表。卸载数萌保留用户数据、方案与独立安装的小狼毫宿主。授权与来源见 [发行说明](desktop/distribution-notices.txt)，客户步骤见 [安装使用说明](desktop/customer-guide.txt)。

构建使用 Windows .NET Framework C# 编译器和 NSIS。开发者先安装 Node.js 24、GitHub CLI 并完成 GitHub 登录，再依次运行 `npm ci`、`npm test`、`npm run desktop:prepare`、`npm run desktop:build`。准备步骤下载并校验 NSIS、小狼毫、语音模型和授权文件；产物在仓库的上一级目录。词库来源与模型 SHA256 固定在 `data/rime-ice/provenance.json` 与语音下载产生的 `models/voice/model-manifest.json`。

开发者可运行安装后的 `winintel-ime.exe /self-test`，检查打包运行时、完整词库、语音 WebSocket 与 Unicode SendInput。`/self-test` 使用模型自带样例，不测试真实麦克风硬件。安装器 `/SKIPIME` 供隔离打包验证使用。
