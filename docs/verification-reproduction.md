# 本轮复现说明

数萌AI服务器 (winintel) / Alex YANG

完整源码包包含实际使用的词库生成器、姓名先验、Windows 原生引擎、Android 源码和各平台验证程序。构建方法见根目录 README。离线模型、工具链与签名密钥不包含在源码包内。

行业报告中的复现脚本来自 `F:\Temp\winintel-domain-20261128`。脚本目录包含冻结的 `suite.tsv` 和旧版基线头文件。把本版源码解压到脚本目录的 `source` 子目录，并把 Windows 安装目录中的 `dictionary.bin`、`character-readings.tsv`、`name-parts.tsv`、`hot-chinese.tsv`、`english-words.tsv`、`industry-words.tsv` 复制到 `test-assets` 子目录。创建 `checks` 目录，按本机路径修改 `compile.ps1` 中的 MSVC/Windows SDK 设置，然后执行。复测必须使用新的隔离学习目录，避免上一次选词影响计分。

本轮 `benchmark.cpp` 的参数顺序为：词库资产目录、无表头六列 TSV 用例（编号、行业、类型、组别、输入、目标）、输出 TSV、隔离学习目录。类型 `C` 表示中文拼音，`E` 表示英文补全。新版使用行业选择，旧版通过 `BASELINE` 编译标志使用原 0.5.14 头文件；两者使用同一资产目录和同一冻结输入。引擎性能在桌面测量，不代表手机实机速度。

`report.mjs` 根据已保存的 Windows、Android 和搜狗候选计算逐例结果；其报告输出及旧报告路径需按本机位置修改。冻结 JSON 和哈希随报告提供。搜狗原 200 例沿用之前实际采集的结果，本轮新增 1,095 例拼音单独采集；200 例代码英文未测搜狗。原始输出不会自动重新采集。搜狗采集器源码随报告提供，重新采集需安装对应输入法，在隔离测试编辑框运行，采集期间保持该窗口焦点。

Android 的设备验证使用 `android/verification` 工程的 `AccuracyUpgradeCheck`、`IndustryCheck` 和 `CaretCheck`，目标包为 `com.winintel.ime`。桌面 JVM 的 `DomainBenchmark` 使用同一行业层及基础词库。Windows `UiProbe` 使用同一原生产品源码构建带 `WININTEL_TEST_STORE` 的测试组件，接入自有测试编辑框；发布组件不包含该测试接入点。
