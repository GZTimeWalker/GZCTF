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

GZ::CTF は ASP.NET Core を基づいたオープンソース CTF プラットフォーム。

> [!IMPORTANT]
> **お使いの前にドキュメントを読むことは極めてお勧めします：[https://docs.ctf.gzti.me/ja](https://docs.ctf.gzti.me/ja)**

> [!WARNING]
> 2024/01/01 より，`develop` ブランチから構築されたイメージのデータベーススキーマに破壊的変更が行われたため、従来のデータベースとの交換性はありません。従来のバージョンを引き続き使いたかった場合は `v0.17` を使ってください。
>
> 新しい機能の開発期間で `develop` イメージを生産環境にデプロイすることはお勧めしません。データ損失が発生する恐れがあります。

## 機能 🛠️

- 高度カスタマイズ可能なチャレンジを作れる

  - チャレンジ種類：静的アタッチメント、動的アタッチメント、静的コンテイナー、動的コンテイナー

    - 静的アタッチメント：アタッチメントは共有され、追加されたフラッグのどれも提出可能となります。
    - 動的アタッチメント：チーム数に応じたフラッグやアタッチメントが少なくとも必要となり、チームに応じてアタッチメントやフラッグが配布されます。
    - 静的コンテイナー：コンテナーのテンプレートは共有され、フラッグは発行されず、追加されたフラッグのどれも提出可能となります。
    - 動的コンテイナー：フラッグは自動的に生成され、コンテナ環境変数を通じて配布され、各チームのフラッグは一意です。

  - 動的スコアリング

    - スコアリングカーヴ：

      $$f(S, r, d, x) = \left \lfloor S \times \left[r  + ( 1- r) \times \exp\left( \dfrac{1 - x}{d} \right) \right] \right \rfloor $$

      このうち、 $S$ はオリジナルスコア、 $r$ は最低スコア率、 $d$ は難易度係数、 $x$ は提出数です。 最初の 3 つのパラメータは、ほとんどの動的なスコア要件を達成するためにカスタマイズできます。

    - ボーナス：
      プラットフォームは、ファーストブラッド、セカンドブラッドとサードブラッドに対して、それぞれポイントの 5%、3%、および 1% をボーナスとして与えます。

  - チャレンジはゲーム中に有効または無効にすることができ、複数回公開もできます。
  - 動的フラッグのチャレンジに対する不正検出機能、オプションのフラッグテンプレートとリートフラッグ機能

- **組織ごとにグループされたチーム**のスコアタイムライン、組織順位表
- **Docker または K8S** に基づいた動的なコンテナの分散、管理、および複数のポート マッピング方法のサポート
- SignalR に基づいた**リアルタイム**ゲーム通知、ゲームイベントとフラッグ送信とログの監視
- SMTP メール検証機能、Google ReCaptchav3 による悪意のある登録防止
- ユーザーのブロックおよび権限管理
- オプションのチームレビュー、招待コード、登録メール制限
- プラットフォーム内での記事の収集、レビューとバッチダウンロード
- ランキングのダウンロードおよびエクスポート、すべての提出もダウンロードできます
- ゲーム中の審判監視、提出とメインイベントのログ
- チャレンジに対するトラフィック **TCP over WebSocket プロキシ転送**、トラフィック キャプチャの設定ができます
- Redis ベースのクラスター キャッシュ、PGSQL をデータベースとして使用
- グローバル設定でプラットフォームタイトルと他の情報を設定可能
- マトリクスと分散トレーシングのサポート
- その他...

## Demo 🗿

![index.png](docs/public/images/index.png)
![game.challenges.png](docs/public/images/game.challenges.png)
![game.scoreboard.png](docs/public/images/game.scoreboard.png)
![admin.settings.png](docs/public/images/admin.settings.png)
![admin.challenges.png](docs/public/images/admin.challenges.png)
![admin.challenge.info.png](docs/public/images/admin.challenge.info.png)
![admin.challenge.flags.png](docs/public/images/admin.challenge.flags.png)
![admin.game.info.png](docs/public/images/admin.game.info.png)
![admin.game.review.png](docs/public/images/admin.game.review.png)
![admin.teams.png](docs/public/images/admin.teams.png)
![admin.instances.png](docs/public/images/admin.instances.png)
![monitor.game.events.png](docs/public/images/monitor.game.events.png)
![monitor.game.submissions.png](docs/public/images/monitor.game.submissions.png)

## i18n について 🌐

多言語対応についてはいま取り込んでいます、詳細や翻訳を提供するなどは [translate.ctf.gzti.me](https://translate.ctf.gzti.me) まで参照してください。

## 貢献者 👋

<a href="https://github.com/GZTimeWalker/GZCTF/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=GZTimeWalker/GZCTF" />
</a>

## 大会事例 🏆

以下は GZCTF を使った CTF 大会事例の一部です。主催者たちの信頼、サポートと大切なフィードバックが GZCTF の継続的改善の原動力となっております。

- **清華大学ネットワークセキュリティ技術チャレンジ THUCTF 2022**
- **浙江大学 ZJUCTF 2022/2023**
- **東南大学虎踞龍蟠杯ネットワークセキュリティチャレンジ SUSCTF 2022/2023**
- **甘粛政法大学 DIDCTF 2022/2023**
- **山東科技大学第一回ネットワークセキュリティ実践大会 woodpecker**
- **西北工業大学 NPUCTF 2022**
- **SkyNICO ネットワーク空間セキュリティ三校連合大会 (厦門理工学院、福建師範大学、齐鲁工業大学)**
- **湖南警察学院ネットワークセキュリティ攻防大会**
- **中山大学第一回情報セキュリティ新人戦 [W4terCTF 2023](https://github.com/W4terDr0p/W4terCTF-2023)**
- **同済大学第五回ネットワークセキュリティ大会 TongjiCTF 2023**
- **重慶工商大学第一回ネットワークセキュリティ大会 CTBUCTF 2023**
- **西北工業大学第一回セキュリティ実験技能大会 NPUCTF 2023**
- **浙江師範大学行知学院第一回ネットワークセキュリティ新人戦 XZCTF 2023**
- **ハルビン工程大学貢橙杯新生大会 ORGCTF 2023**
- **"山河"ネットワークセキュリティ技能チャレンジ SHCTF 2023**
- **天津科技大学 2023 年大学生クリエイタートレーニングキャンプネットワークセキュリティグループ選抜**
- **湖南衡陽師範学院玄天ネット安実験室新人戦 HYNUCTF 2023**
- **南陽師範学院新人戦 NYNUCTF S4**
- **商丘師範学院初回ネットワークセキュリティ新人戦**
- **蘇州市職業大学 2023 年冬季新人戦 [SVUCTF-WINTER-2023](https://github.com/SVUCTF/SVUCTF-WINTER-2023)**
- **北京航空航天大学 BUAACTF 2024**
- **カリフォルニア大学サンディエゴ校 San Diego CTF 2024**
- **曲阜師範大学第1回“曲star”サイバーセキュリティスキルコンテスト**

_順番は順位との関係はありません。追加ための PR は大歓迎。_

## 特別感謝 ❤️‍🔥

THUCTF 2022 の主催者である NanoApe によるスポンサーシップと、Alibaba Cloud パブリックネットワークでのストレステストのおかげで、数千の同時実行と 3 分間の 134 万のリクエストのプレッシャーの下で GZCTF 単一マシンインスタンス (16c90g) のサービスの安定性を検証することができました。ここで心から感謝申し上げます。

## Stars ✨

[![Stargazers over time](https://starchart.cc/GZTimeWalker/GZCTF.svg?variant=adaptive)](https://starchart.cc/GZTimeWalker/GZCTF)
