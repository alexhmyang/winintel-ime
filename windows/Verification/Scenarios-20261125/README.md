# 场景测试复现

测试内容包含 24 组中英文本、48 条本机合成音频、16 条确定种子生成的 12dB 白噪声样本，以及真实节奏输入、字幕串联和桌面功能检查。语音不包含真人录音。

准备一个测试目录，通过环境变量 `WININTEL_BENCH_ROOT` 指定路径，将本目录的脚本和 `scenarios.json` 放入。将完整离线安装资产放入该目录的 `meeting-build/payload`。在 Windows 安装 Huihui / Zira 系统语音后，执行 `ScenarioFixtures.ps1 -BenchmarkRoot <测试目录>` 生成音频。

使用资产内的 `runtime/node.exe` 运行：

1. `BenchmarkScenarios.mjs sense`、`BenchmarkScenarios.mjs stream`；如需 Qwen 对照，将指定模型额外放入测试目录，再运行 `BenchmarkScenarios.mjs qwen`。
2. `BenchmarkTranslation.mjs`，每条文本运行 3 次。
3. `BenchmarkCaptionPipeline.mjs`，将第 1 步真实识别结果送入翻译。
4. `BenchmarkRealtime.mjs`，按墙钟时间每 100ms 发送音频块；两套 ONNX 原生库在不同进程执行。

报告中的基线 `scenario-translation-baseline.json` 是未使用术语保护的独立对照；语义评审记录固定在 `scenario-semantic-review.json`，不能自动把它当成新输出的评审。重新测试后需要重新逐条核查，并更新语义记录，再运行 `BuildScenarioReport.mjs`。输出目录通过 `WININTEL_REPORT_OUTPUT` 指定。

使用 .NET Framework 编译 `MeetingProbe.cs` 与桌面 Meeting、MeetingWindow、Translation 源码，并引用 System.Windows.Forms、System.Drawing、System.Web.Extensions；验证 12 项导出与窗口行为。编译 `TranslationProbe.cs` 与 Translation 源码、引用 System.Web.Extensions；测试 EXE 必须放在资产目录根部运行，验证真实子进程与中文编码。

原始测试结果随本目录归档；面向用户的全部数据以 Markdown 表格报告展示。基线时间与最终版本时间来自各自一次测试，不能据此推算纯算法加速比例。
