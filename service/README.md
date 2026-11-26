# winintel AI 词库 API / Lexicon API

## 实时新闻接口 / Realtime news

运行 `node service/news-api.mjs`，默认监听 `127.0.0.1:8788`，提供只读 `GET /v1/news`。公网使用 HTTPS 反向代理。维护 `service/news.json` 中的 `schema: 1` 和 `items`；每条包含标题 `title`、HTTPS 原文 `url`、HTTPS 来源 `source`、ISO 8601 发布日期 `published_at`。最多 200 条、2 MB；只展示标题、来源、日期和链接。

配置 `WININTEL_NEWS_HOST`、`WININTEL_NEWS_PORT`、`WININTEL_NEWS_FILE` 可改变监听地址与数据文件。初始示例为空，部署后由自有后台提供新闻条目。Windows 0.5.8 和 Android 0.1.2 可填入完整 HTTPS `/v1/news` 地址，选择失败时回退免费 IT之家 / NPR。默认刷新间隔 5 分钟；Android 新闻默认关闭，开启后仅在新闻页面定时刷新。

Run `node service/news-api.mjs` for the self-hosted read-only news API. Put an HTTPS reverse proxy in front of port 8788 and maintain `news.json`. Both platforms support the endpoint and optional fallback to free feeds. Requests never contain input, learning history or audio.

## 中英文热词更新（Windows 0.5.8）

服务端提供公开只读接口 `GET /v1/hotwords`，返回同目录的 `hotwords.json`。维护该文件即可发布中英文词包；`version` 使用 `yyyy-MM-dd`，中文字段为 `code`（拼音）与 `text`，英文字段为词语数组。单包最多各 10,000 项、2 MB，客户端全量校验后替换本地缓存。在“每日新闻与热词”中填写你自有后台的完整接口地址，即可随每日刷新一起更新；本机开发地址为 `http://127.0.0.1:8787/v1/hotwords`。

该免费内容接口不涉及支付或客户密钥，也不接收输入、录音和用户历史。新闻默认从公开 RSS 直接下载，也可将新闻源设置为你后台提供的 HTTPS RSS / Atom。正文、图片不重新分发，客户端仅展示标题、日期和链接。现有企业词库接口继续按原有访问密钥规则管理。

采用免费 GPL 客户端与收费企业服务模式。企业服务可按年收取词库托管、部署、行业词库维护与技术支持费用；客户可自行运行完整开源代码。定价和合同通过 service@winintel.com 联系。没有付费第三方 API、客户端功能锁或在线支付。

| 模式 | 适用性 | 采用情况 |
|---|---|---|
| 客户端买断 / 功能订阅 | 需要许可与功能限制 | 未采用 |
| 企业服务与词库托管 | 开源客户端不受限制，服务产生收入 | 主模式 |
| 赞助 / 广告 | 自愿开启、明确标识 | 工具箱预留入口 |

使用 Node.js 22+，无 npm 依赖：

```powershell
node service/lexicon-api.mjs
```

默认监听 `127.0.0.1:8787`。公网部署应通过 HTTPS 反向代理，保持源站只监听本机。配置环境变量 `WININTEL_LEXICON_PORT`、`WININTEL_LEXICON_HOST`、`WININTEL_LEXICON_ROOT`。`WININTEL_LEXICON_TOKENS` 是以逗号分隔的企业密钥，生产使用随机长密钥。不要把密钥提交到源码或公开日志。

| 请求 | 返回 |
|---|---|
| GET /v1/catalog | 社区 / 企业词库目录 |
| GET /v1/lexicons/hardware | 免费示例硬件词库 |
| GET /v1/lexicons/team-demo | 企业示例，需 Authorization: Bearer 密钥 |

服务只接受 GET，每 IP 每分钟最多 60 次。词库名称仅接受字母数字与连字符。示例企业密钥由管理员自行设置，不内置。使用全局企业密钥演示鉴权；多租户隔离、计费、密钥吊销管理和支付由运营部署另行配置，目前代码不包含这些管理系统。

客户端：工具箱 → 服务与赞助 → 启用词库 API → 地址 `http://127.0.0.1:8787` → ID `hardware` → 同步词库。公网地址只接受 HTTPS，禁止自动跳转。最多 2 MB、10000 词条，15 秒超时。有效词库先验证后替换 `%LOCALAPPDATA%\winintel-ime-native\api-phrases.tsv`。个人词库优先于 API 缓存。

词库格式：

```json
{"schema":1,"id":"hardware","name":"硬件词库","entries":[{"text":"数萌服务器","pinyin":"shu meng fu wu qi"}]}
```

赞助位配置：在 `%LOCALAPPDATA%\winintel-ime-native\sponsor.json` 放置下面格式，并由用户主动开启工具箱赞助开关：

```json
{"label":"企业服务器方案","url":"https://winintel.com","expires":"2027-12-31T00:00:00Z"}
```

过期、无效或非 HTTPS 配置不显示。只在工具箱显示“广告”，没有后台网络请求、曝光统计或点击上报。

## English

The GPL client and all local features remain free. Revenue comes from enterprise dictionary hosting, deployment, dictionary maintenance and support. The server is open source and self-hostable, using Node.js 22+ without package dependencies.

Start with `node service/lexicon-api.mjs`. The default bind address is `127.0.0.1:8787`; put an HTTPS reverse proxy in front for public deployment. Configure `WININTEL_LEXICON_TOKENS` for managed packs. GET `/v1/catalog` lists packs; GET `/v1/lexicons/hardware` downloads the community sample; `team-demo` requires a Bearer token. This implementation includes access checks, not payment processing or tenant management.

Clients synchronize only when requested. No typed text, learning history, audio or contact names are sent. Packs are validated before replacing the offline cache. Downloads are limited to 2 MB / 10,000 entries / 15 seconds, with redirects disabled. The optional sponsored placement uses local configuration, is off by default and is labeled as advertising in the toolbox.
