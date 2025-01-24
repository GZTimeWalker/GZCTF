<picture>
  <source media="(prefers-color-scheme: dark)" srcset="assets/banner.dark.svg">
  <img alt="Banner" src="assets/banner.light.svg">
</picture>

# GZ::CTF

[![publish](https://github.com/GZTimeWalker/GZCTF/actions/workflows/ci.yml/badge.svg)](https://github.com/GZTimeWalker/GZCTF/actions/workflows/ci.yml)
![version](https://img.shields.io/github/v/release/GZTimeWalker/GZCTF?include_prereleases&label=version)
![license](https://img.shields.io/github/license/GZTimeWalker/GZCTF?color=FF5531)
[![Crowdin](https://badges.crowdin.net/gzctf/localized.svg)](https://crowdin.com/project/gzctf)

[![Telegram Group](https://img.shields.io/endpoint?color=blue&url=https%3A%2F%2Ftg.sumanjay.workers.dev%2Fgzctf)](https://telegram.dog/gzctf)
[![QQ Group](https://img.shields.io/badge/QQ%20Group-903244818-blue)](https://jq.qq.com/?_wv=1027&k=muSqhF9x)
[![Discord](https://img.shields.io/discord/1239476909033656320?label=Discord)](https://discord.gg/dV9A6ZjVhC)

[English](./README.md), [简体中文](./README.zh.md), [日本語](./README.ja.md)

GZ::CTF 是一个基于 ASP.NET Core 的开源 CTF 平台。

> [!IMPORTANT]
>
> **为了避免不必要的时间浪费，使用前请详细阅读使用文档：[https://gzctf.gzti.me/](https://gzctf.gzti.me/zh)**

> [!WARNING]
>
> **升级和迁移注意事项：**
>
> 1. 如需升级平台，只需拉取最新的镜像并重新启动，数据库迁移会自动进行。
> 2. 一般情况下，不支持 **降级** 操作，时间跨度过大的版本升级可能会导致数据不兼容，请做好数据备份。
> 3. 升级后可能会有新的配置项和文件结构变更，建议查看官方文档或咨询社区。
> 4. 如迁移到其他分支项目，请注意数据库结构是否存在变更，**变更后的数据库不支持回退到原始版本**。
> 5. 社区及官方不对数据丢失、数据不兼容等问题负责，分支项目问题请联系对应项目维护者。

## 特性 🛠️

- 创建高度可自定义的题目

  - 题目类型：静态附件、动态附件、静态容器、动态容器

    - 静态附件：共用附件，任意添加的 flag 均可提交。
    - 动态附件：需要至少满足队伍数量的 flag 和附件，附件及 flag 按照队伍进行分发。
    - 静态容器：共用容器模版，不下发 flag，任意添加的 flag 均可提交。
    - 动态容器：自动生成并通过容器环境变量进行 flag 下发，每个队伍 flag 唯一。

  - 动态分值

    - 分值曲线：

      $$f(S, r, d, x) = \left \lfloor S \times \left[r  + ( 1- r) \times \exp\left( \dfrac{1 - x}{d} \right) \right] \right \rfloor $$

      其中 $S$ 为原始分值、 $r$ 为最低分值比例、 $d$ 为难度系数、 $x$ 为提交次数。前三个参数可通过自定义实现绝大部分的动态分值需求。

    - 三血奖励：
      平台对一二三血分别奖励 5%、3%、1% 的当前题目分值

  - 比赛进行中可启用、禁用题目，可多次放题
  - 动态 flag 中启用作弊检测，可选的 flag 模版，leet flag 功能

- **分组队伍**得分时间线、分组积分榜
- 基于 **Docker 或 K8s** 的动态容器分发、管理、多种端口映射方式
- 基于 SignalR 的**实时**比赛通知、比赛事件和 flag 提交监控及日志监控
- SMTP 邮件验证功能、基于 Cloudflare Turnstile 的恶意注册防护
- 用户封禁、用户三级权限管理
- 可选的队伍审核、邀请码、注册邮箱限制
- 平台内 Writeup 收集、查阅、批量下载
- 可下载导出积分榜、可下载全部提交记录
- 比赛期间裁判监控、提交和主要事件日志
- 题目流量 **TCP over WebSocket 代理转发**、可配置流量捕获
- 基于 Redis 的集群缓存、基于 PGSQL 的数据库存储后端
- 支持本地文件系统和 **对象存储 (MinIO, S3 等)** 作为文件存储后端
- 支持暗黑模式、多国语言、自定义主题颜色
- 支持 **网站页脚**、**网站图标** 和 **HTML 描述** 的自定义，以提升 SEO
- 全局配置项自定义、平台标题、备案信息
- 支持测量和分布式追踪
- 以及更多……

## 关于 i18n 🌐

目前，该平台支持多种语言，翻译进度如下：

### 由社区翻译

- 英语 (en-US)：完全支持，默认语言
- 简体中文 (zh-CN)：完全支持
- 繁体中文 (zh-TW)：完全支持
- 日语 (ja-JP)：完全支持，由 [Steve](https://github.com/hez2010) 翻译
- 印度尼西亚语 (id-ID)：完全支持，由 [Rio](https://github.com/riodrwn) 翻译
- 韩语 (ko-KR)：完全支持，由 [Sy2n0](https://github.com/Sy2n0), [kimjw0427](https://github.com/kimjw0427), [LittleDev0617](https://github.com/LittleDev0617), [Jungwoong Kim](https://github.com/jungwngkim) 和 [blluv](https://github.com/blluv) 翻译
- 俄语 (ru-RU)：完全支持，由 [FazaN](https://github.com/CyberFazaN) 翻译
- 越南语 (vi-VN)：完全支持，由 [Ethical Hacker Club](https://github.com/FPTU-Ethical-Hackers-Club) 翻译

### 由机器和 AI 翻译

- 德语 (de-DE)
- 法语 (fr-FR)
- 西班牙语 (es-ES)

这些翻译并不完美，我们需要你的帮助来改进它们。

如果你有兴趣为翻译作出贡献，请参考 [Crowdin 项目](https://crowdin.com/project/gzctf)。

## Demo 🗿

![index.webp](assets/images/index.webp)
![game.list.webp](assets/images/game.list.webp)
![game.challenges.webp](assets/images/game.challenges.webp)
![game.scoreboard.webp](assets/images/game.scoreboard.webp)
![admin.settings.webp](assets/images/admin.settings.webp)
![admin.challenges.webp](assets/images/admin.challenges.webp)
![admin.challenge.info.webp](assets/images/admin.challenge.info.webp)
![admin.challenge.flags.webp](assets/images/admin.challenge.flags.webp)
![admin.game.info.webp](assets/images/admin.game.info.webp)
![admin.game.review.webp](assets/images/admin.game.review.webp)
![admin.teams.webp](assets/images/admin.teams.webp)
![admin.instances.webp](assets/images/admin.instances.webp)
![monitor.game.events.webp](assets/images/monitor.game.events.webp)
![monitor.game.submissions.webp](assets/images/monitor.game.submissions.webp)

## 贡献者 👋

<a href="https://github.com/GZTimeWalker/GZCTF/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=GZTimeWalker/GZCTF" />
</a>

## 赛事案例 🏆

已经有一些赛事的举办者选择了 GZCTF 并圆满完赛，他们的信任、支持和及时的反馈是 GZCTF 不断完善的第一推动力。

### 国际赛事

- [**San Diego CTF 2024: Organized by University of California, San Diego**](https://ctftime.org/event/2325)
- [**R3CTF 2024: Organized by r3kapig**](https://ctftime.org/event/2273)
- [**TCP1P CTF 2024: Exploring Nusantara's Digital Realm**](https://ctftime.org/event/2256/)

### 其他赛事

- **清华大学网络安全技术挑战赛 THUCTF 2022**
- **浙江大学 ZJUCTF 2022/2023/2024**
- **东南大学虎踞龙蟠杯网络安全挑战赛 SUSCTF 2022/2023/2024**
- **甘肃政法大学 DIDCTF 2022/2023/2024**
- **中山大学信息安全新手赛 W4terCTF [2023](https://github.com/W4terDr0p/W4terCTF-2023)/2024**
- **浙江师范大学 ZJNUCTF [2023](https://github.com/A1natas/zjnuctf-school-contest-2023)/[2024](https://github.com/A1natas/zjnuctf-school-contest-2024)**
- **山东科技大学第一届网络安全实践大赛 woodpecker**
- **西北工业大学 NPUCTF 2022**
- **SkyNICO 网络空间安全三校联赛 (厦门理工学院、福建师范大学、齐鲁工业大学)**
- **湖南警察学院网络安全攻防大赛**
- **同济大学第五届网络安全竞赛 TongjiCTF 2023**
- **同济大学信息安全竞赛（新生赛）CatCTF 2023/2024**
- **重庆工商大学第一届网络安全竞赛 CTBUCTF 2023**
- **西北工业大学第一届安全实验技能竞赛 NPUCTF 2023**
- **浙江师范大学行知学院第一届网络安全新手赛 XZCTF 2023**
- **哈尔滨工程大学贡橙杯新生赛 ORGCTF 2023**
- **"山河"网络安全技能挑战赛 SHCTF 2023**
- **天津科技大学 2023 年大学生创客训练营网络安全组选拔**
- **湖南衡阳师范学院玄天网安实验室招新赛 HYNUCTF 2023**
- **南阳师范学院招新赛 NYNUCTF S4**
- **商丘师范学院首届网络安全新生挑战赛**
- **苏州市职业大学 2023 年冬季新生赛 [SVUCTF-WINTER-2023](https://github.com/SVUCTF/SVUCTF-WINTER-2023)**
- **北京经济管理职业学院 首届 BIEM“信安杯”CTF 竞赛**
- **北京航空航天大学 BUAACTF 2024**
- **曲阜师范大学第一届“曲 star”网络安全技能竞赛**
- **成都理工大学第四届/第五届信安大挑战 DinoCTF**
- **重庆邮电大学 2024 年红岩杯信息安全竞赛 RedrockCTF**
- **重庆安全技术职业学院 2024 年“先锋杯”网络安全技能竞赛 WAXFCTF**

_排名不分先后，欢迎提交 PR 进行补充。_

## 特别感谢 ❤️‍🔥

感谢 THUCTF 2022 的组织者 NanoApe 提供的赞助及阿里云公网并发压力测试，帮助验证了 GZCTF 单机实例（16c90g）在千级并发、三分钟 134w 请求压力下的服务稳定性。

## Stars ✨

[![Stargazers over time](https://starchart.cc/GZTimeWalker/GZCTF.svg?variant=adaptive)](https://starchart.cc/GZTimeWalker/GZCTF)
