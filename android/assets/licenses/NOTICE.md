# winintel AI Android

Application code Copyright (C) 2026 winintel AI — GPL-3.0-or-later.

| Component | Source | License |
|---|---|---|
| Pinyin dictionary data | iDvel/rime-ice, revision 927e1cf1d3eeb9bf4be85e3f684dba4dd485b6f6 | GPL-3.0, full text included |
| sherpa-onnx Android JNI 2.0.3 | k2-fsa/sherpa-onnx | Apache-2.0 |
| ONNX Runtime | microsoft/onnxruntime, bundled with upstream AAR | MIT + included third-party notices |
| Kotlin stdlib 1.9.24 | JetBrains/kotlin | Apache-2.0 |
| SenseVoiceSmall INT8 | Alibaba / FunAudioLLM/SenseVoiceSmall; ONNX conversion csukuangfj | FunASR Model Open Source License Agreement 1.1, separately licensed model weights |

The binary WIME0002 index is generated from the fixed dictionary revision and winintel starter terminology. The full model license, upstream identity and SHA256 are included. No paid dependency or server is required.

Android 0.1.5 also includes cn_dicts/ext.dict.yaml, tencent.dict.yaml,
others.dict.yaml and 41448.dict.yaml from the same pinned revision.
The expanded source list and upstream blob identifiers are recorded in
dictionary-provenance.json. This uses dictionary data only; the keyboard engine
remains winintel's own implementation.
