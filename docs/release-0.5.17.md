# winintel AI · Windows 0.5.17 / Android 0.1.12

本次发布汇总最新源码、Windows 和 Android 安装包，以及 Linux IBus 安装包与 macOS 原生源码、词库。所有核心输入和语音处理继续在本机运行。

| 平台 | 本次交付 | 更新内容 |
| --- | --- | --- |
| Windows | 0.5.17 完整 EXE，x64/x86 TSF | Shift 按实际编辑框定位输入线程；安装时退出旧后台并启动新版；方向键导航、两位编号选词和每页 25 项候选 |
| Android | 0.1.12 原签名 APK，arm64/x86_64 | 候选完整分页、紧凑布局、展开热区、长按删除保护、九宫格及行业词库 |
| Linux | 0.5.16 x86_64 IBus/GTK 包 | 共用离线引擎、中英文文字输入、行业词库及持久化选词 |
| macOS | 0.5.16 原生源码与词库 | InputMethodKit 入口及 Intel/Apple Silicon Universal 构建脚本 |

Linux / macOS 本轮覆盖文字输入，语音、翻译、OCR 与会议工具尚未接入这些入口；macOS 尚未完成 Mac 上的编译及实机验证，提供的是源码包。Linux Wayland 桌面兼容仍待验证。

| 验证范围 | 结果 |
| --- | --- |
| Windows 前轮功能及候选检查 | 377 项通过 |
| Windows 0.5.17 Shift 专项 | 61 项通过 |
| 本机正式 0.5.17 后台，真实左右 Shift 与 Ctrl+Shift | 12 项通过 |
| Android 键盘布局 | 1,520 项布局检查通过 |
| Android 长按删除与词库检查 | 5 项删除检查、16 项词库检查通过 |
| Android 发布签名 | versionName 0.1.12、versionCode 13，v2/v3 签名有效 |
| Linux | 共享引擎与 GTK 集成检查通过 |

Shift 检查使用独立文本框。跨线程用例证明旧控制器找到了外层窗口线程，修复版准确找到编辑框线程；没有在微信聊天框或 Trae 编辑区写入测试内容。已打开的应用可能仍保留旧 DLL，需要重新打开才能加载新版。

Windows 覆盖安装保留个人词库；Android 使用相同证书签名，可覆盖安装保留应用数据。卸载或清除应用数据会删除个人记录。

发布页提供安装包、完整源码、发布说明及 SHA256SUMS.txt 校验文件。视频演示请访问 [YouTube 频道](https://www.youtube.com/@winintelcom)。下载入口统一指向此发布，旧软件发布清理后不再提供旧安装包。

数萌AI服务器 (winintel) / Alex YANG

https://github.com/alexhmyang/winintel-ime
