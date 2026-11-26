本表记录拆字母合成语音的实际识别；不匹配保留完整输出，不代表真人准确率。草稿在合并分段后另做技术词规范化，见 Voice-acronym-draft.md。

100 ms 音频分块 → 实时识别 → 停顿分段 → 整句复核 → 结束会话；全部为合成音频。

| 预期 | 工作线程完整结果 | 结果 |
|---|---|---|
| V100 32G SXM3 | V100 32G SXM3 | 通过 |
| NVLink NVLink | NVLink NVLink | 通过 |
| 我们的网站叫 winintel.com | 我们的网站叫 w i n intel com | 失败 |
| 56Gbps 8 bits | 56Gbps pies | 失败 |
| 纯静音 | （空） | 通过 |
