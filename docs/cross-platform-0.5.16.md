# 跨平台输入法 0.5.16 / Android 0.1.12

数萌AI服务器 (winintel) / Alex YANG · GPL-3.0-or-later

| 平台 | 本轮交付 | 实际范围 |
|---|---|---|
| Windows | 完整 0.5.16 EXE，x64 / x86 TSF DLL | 合入 18:41 补丁，新编译的独立 TSF 输入检查 377 项通过，包括编号 10–25、中文/自动模式、实际矩形方向键 |
| Android | 原发布密钥签名的 0.1.12 APK，versionCode 13 | 25 项分页可横向滚动，展开连续排词、48dp 点击区域，降低键盘高度，长按删除取消多删 |
| Linux | x86_64 IBus / GTK 安装包、CMake 源码 | 现有 C++ 中文、姓名与行业引擎，英文补全，持久化选词，25 项候选窗口 |
| macOS | InputMethodKit 源码、词库、Intel / Apple Silicon Universal 构建脚本 | 共享引擎和原生候选面板已实现；没有 macOS 主机，未编译、未验证实际输入 |

Linux 和 macOS 本轮新增文字输入入口。Windows / Android 的离线语音、翻译、OCR、会议等工具没有自动移植到这两个新入口。

## 按键

桌面每页 25 个候选，编号从 1 开始。输入 `1`、`5` 选择第 15 项；`2`、`5` 选择第 25 项。输入单独 `1` 或 `2` 后按空格确认。方向键取消待输入编号，按实际候选位置移动；上下跨行，左右同行，边缘保持。PgUp / PgDn 翻页，空格 / 回车确认，Esc 取消。英文支持 Tab 补全。Linux / macOS 单独按下再释放 Shift 切中英文，Shift 加字母不切换。

## Linux 安装与构建

Ubuntu / Debian 依赖：`ibus python3-gi gir1.2-ibus-1.0 gir1.2-gtk-3.0`。下载并解压 Linux 包，在包目录运行 `sh install.sh`；安装到当前用户的数据目录，同时生成 IBus 组件定义。用户方便时重启 IBus，再从系统输入设置添加“数萌 AI”。安装脚本不替用户重启桌面服务。现有个人词不覆盖。

源码构建需要 `cmake g++`，运行 `sh linux/build.sh /path/to/assets /path/to/build`。包中的词库目录可以作为 assets。其他 CPU 架构需要在目标机重建共享库。本次 x86_64 包使用 Ubuntu 24.04 / glibc 2.39，较旧发行版建议源码构建。GTK 候选窗口在 X11 环境验证；Wayland 原生应用的候选定位及桌面兼容尚待验证。当前提供 IBus 入口，未提供 Fcitx 入口。

## macOS 构建与安装

当前交付的是源码构建包，没有已验证的 `.app` 或 `.dmg`。在 macOS 11+ 安装 Xcode Command Line Tools 后，源码根目录运行 `sh macos/build.sh ./assets ./build-macos`。脚本编译 arm64 + x86_64 Universal 应用，签名校验并生成 ZIP。默认使用本地 ad-hoc 签名；如有自己的 Apple 签名证书，可通过 `WININTEL_SIGN_IDENTITY` 指定。未申请 Apple 公证。

将生成的 `Winintel.app` 复制到 `~/Library/Input Methods/`，注销并重新登录，再从“系统设置 → 键盘 → 输入源”添加“数萌 AI”。macOS 的编译、输入源注册、输入兼容、焦点切换和密码框保护均需在实机确认。

## 本地数据与行业

Linux 数据保存在 `$XDG_DATA_HOME/winintel-ime-native`，默认 `~/.local/share/winintel-ime-native`；macOS 在 `~/Library/Application Support/winintel-ime-native`。选词采用文件锁和原子替换，本地持久化，不联网。测试通过 `WININTEL_DATA_HOME` 使用隔离目录。

Linux / macOS 暂无行业设置面板，可在该数据目录创建 `industry.ini`：

```ini
[Industry]
Profile=programming
CodeAssist=1
```

行业 ID 取自随包 `industry-words.tsv` 第一列，`general` 为默认通用模式。两个新入口不自动识别宿主编辑器。

## Windows / Android 更新

Windows 使用新的完整安装包。已打开应用可能仍缓存旧 TSF DLL，方便时彻底退出再重开目标应用；本轮不替用户关闭 Trae 或微信。

Android 覆盖安装新版 APK，保留原签名及本机个人数据。候选条可左右滑动浏览当前页全部 25 项，点“25项”展开连续排列，再上下滚动；固定“上页 / 下页”按钮更换候选页。模拟器检查通过不等于所有真实手机都已适配。
