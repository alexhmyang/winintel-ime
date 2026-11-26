# winintel AI — Windows 0.5.8 / Android 0.1.2

此次发布汇总 Windows 0.5.5–0.5.8 的更新，并升级手机 APK。完整源码、中英双语 README、安装包和 SHA-256 校验文件一起提供。

| 平台 | 更新 |
|---|---|
| Windows 0.5.8 | 256 字母拼音编辑、撤销重做、简拼学习与个人词库；22,437 项英文补全；双语精选热词；表情、词库同步与工具箱；免费实时新闻和自有接口 |
| Android 0.1.2 | 本地英文前缀补全，点击候选完成；Space 保留原词；中英文精选热词；首页与键盘新闻入口；原签名覆盖升级 |
| 新闻 | 免费 IT之家 / NPR；默认 5 分钟刷新；自有 HTTPS `/v1/news`；可配置失败回退；离线缓存；文章日期与下载时间分别展示 |
| 隐私 | 核心输入、排序、纠错、语音和 Windows 翻译在本机 CPU 运行；新闻请求不携带输入、音频或学习历史 |

手机新闻默认关闭，在新闻页面勾选启用并保存后刷新，离开页面停止定时刷新。新增 INTERNET 权限用于新闻。Windows 更新后关闭并重新打开已有 TRAE、微信等窗口，让它们加载新的 TSF 组件。

两平台独立编号。Android 包含 arm64-v8a 与 x86_64，支持 Android 8.0+。Android 语音仍采用停顿后识别；实时翻译、双语 SRT 和会议导出由 Windows 提供。英文补全为本地前缀匹配。热词为 2026-11-26 编辑精选。

自有后台示例：`node service/news-api.mjs` 提供新闻接口，`node service/lexicon-api.mjs` 提供词库和热词。公网服务、订单与支付管理需自行部署；本次客户端不包含自动支付。

| 验证 | 结果与范围 |
|---|---|
| Android 本地功能与新闻解析 | 19 项通过：英文补全、大小写、精确词、未知词、拼音回归、中文热词、RSS / Atom、自有 JSON、无效链接日期与 XML 实体拦截、默认关闭新闻 |
| Android 构建 | Java / DEX 编译、签名验证、16 KB 对齐通过；沿用原发布签名 |
| Android 覆盖升级 | 从已安装 0.1.1 更新到 0.1.2 成功 |
| Windows 0.5.8 基线 | 英文 TSF 17 项、拼音编辑学习 15 项、内容与缓存 11 项、热词 API 5 项、安装 12 项通过；详见 windows/Verification/058-*.md |
| Windows 实时新闻更新 | 9 项客户端及 5 项服务端检查通过：5 分钟默认值、自有接口、失败缓存与 RSS 回退、实际免费源、界面 |
| Windows 最终安装包 | 解出程序 SHA-256 与发布编译文件一致，入口确认为 RealtimeProgram |

Windows 微信联系人对话自动打开功能仍属实验功能；输入兼容性随应用版本和权限变化。实际手机语音速度随 CPU 与系统环境变化。

## English

This release publishes the accumulated Windows updates and a new Android APK. Both platforms include local English prefix completion, curated bilingual vocabulary, free live news and a configurable self-hosted HTTPS news endpoint with optional fallback and offline cache.

Android news is off by default. Enable it on the News page; refresh runs every five minutes while that page is visible. Typing, learning and audio stay on the device. The APK uses the previous release signing key for in-place upgrades. Live translation and bilingual subtitle/meeting export remain Windows features.

Android local feature and news parsing checks: **19 passed**. Signing, alignment and in-place upgrade passed. Windows retains its verified 0.5.8 input components and includes the realtime news entry point; the final installer executable matches the compiled release.

Application source is GPL-3.0-or-later. Models, dictionaries and runtimes retain their own licenses. Author: 数萌AI服务器 (winintel) · service@winintel.com.
