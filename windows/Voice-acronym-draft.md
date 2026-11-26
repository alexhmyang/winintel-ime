Windows Huihui 合成音频 → 工作线程逐段结果 → 实际 VoiceDraft.Accept 草稿合并。

| 预期 | 实际草稿 | 技术字段验证 |
|---|---|---|
| V100 32G SXM3 | V100 32G SXM3 | 通过 |
| NVLink NVLink | NVLink NVLink | 通过 |
| 我们的网站叫 winintel.com | 我们的网站叫 winintel.com | 通过 |
| 56Gbps 8 bits | 56Gbps pies | 失败 |
| 纯静音 | （空） | 通过 |

8 bits 样本中模型漏掉数字并输出 pies，格式规则无法恢复该录音信息。测试不代表真人准确率。
