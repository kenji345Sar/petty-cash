# docs — 目次

このシステムはドメイン駆動設計（DDD）で作っている（方針はリポジトリ直下の [README](../README.md) を参照）。docs はその「何を作っているか」と「どう実現しているか」を書いたもの。

目的別に分けている。仕様を知りたいときは、まず [spec/](spec/README.md) から読む。DDD をどう実現しているかは [architecture/](architecture/README.md) を読む。どちらも番号順に読めばよい。

| 目的 | フォルダ |
|---|---|
| 仕様（何ができるか・業務ルール） | [spec/](spec/README.md) |
| DDD でどう実現しているか | [architecture/](architecture/README.md) |
| 画面と API の調べ方 | [api-screens/](api-screens/) |
| C#・EF Core の学習ノート | [csharp/](csharp/) |
| 環境構築 | [setup/](setup/) |
| イベントソーシング（ES+CQRS）への対応 | [event-sourcing/](event-sourcing/README.md) |
| 記録（未解決の問題・テスト・トラブル） | [issue.md](issue.md)・[test-specification.md](test-specification.md)・[troubleshooting/](troubleshooting/) |

---

## spec/ — 仕様書の本体（何を作っているか）

番号順に読む。読む順番と各ファイルの内容は [spec/README.md](spec/README.md) を参照。

| ファイル | 内容 |
|---|---|
| [README.md](spec/README.md) | 全体像・画面マップ・用語・読む順番 |
| [01-business-flow.md](spec/01-business-flow.md) | 業務の流れ（コードの説明なし） |
| [02-common.md](spec/02-common.md) | 小口・売上金で共通する概念 |
| [03-petty-cash.md](spec/03-petty-cash.md) | 小口タブの仕様 |
| [04-vendor.md](spec/04-vendor.md) | 売上金タブの仕様 |

## architecture/ — DDD でどう実現しているか

番号順に読む。読む順番と各ファイルの内容は [architecture/README.md](architecture/README.md) を参照。

| ファイル | 内容 |
|---|---|
| [01-ddd-layers.md](architecture/01-ddd-layers.md) | 4層の構成と依存のルール、DB テーブル |
| [02-domain-model.md](architecture/02-domain-model.md) | モデル一覧（エンティティ・値オブジェクト）と関係、ないもの、各モデルの業務ルール |
| [03-flow-to-code.md](architecture/03-flow-to-code.md) | 業務の操作ごとの処理の流れ、UseCase 一覧、1例を各層ごとに詳しく追う |
| [04-unit-of-work.md](architecture/04-unit-of-work.md) | 一括コミットの仕組み |
| [05-balance-design.md](architecture/05-balance-design.md) | 残高を取引行に持たせる設計と、その変遷 |

## api-screens/ — 画面と API の調べ方

| ファイル | 内容 |
|---|---|
| [api-endpoints.md](api-screens/api-endpoints.md) | API エンドポイント一覧 |
| [display-logic.md](api-screens/display-logic.md) | 画面の表示ロジック |
| [frontend-api-flow.md](api-screens/frontend-api-flow.md) | フロントエンド → API 呼び出しの概要 |
| [frontend-api-flow-detail.md](api-screens/frontend-api-flow-detail.md) | 同上の詳細（1操作の HTTP リクエストを追う） |
| [vendor-screen-data-flow.md](api-screens/vendor-screen-data-flow.md) | 売上金管理画面の表示データの出どころ |
| [how-to-investigate-screen.md](api-screens/how-to-investigate-screen.md) | 画面からコードを調査する手順 |

## csharp/ — C# 学習ノート

C# / EF Core の書き方を学ぶためのノート。このプロジェクトの設計の説明は [architecture/](architecture/README.md) にある。

| ファイル | 内容 |
|---|---|
| [csharp-concepts.md](csharp/csharp-concepts.md) | C# の基本概念 |
| [routing.md](csharp/routing.md) | ASP.NET Core のルーティング規則 |
| [db-context.md](csharp/db-context.md) | DbContext の仕組み（DB 接続まで） |
| [db-context-code-walkthrough.md](csharp/db-context-code-walkthrough.md) | 同上を実際のコードで追う |
| [entity-framework-core.md](csharp/entity-framework-core.md) | EF Core の仕組み |
| [ef-core-sql-mapping.md](csharp/ef-core-sql-mapping.md) | EF Core の構文と SQL の対応表 |

## setup/ — 環境構築

ローカル（brew の PostgreSQL）での起動手順はリポジトリ直下の [README.md](../README.md) にある。

| ファイル | 内容 |
|---|---|
| [docker-setup.md](setup/docker-setup.md) | Docker での起動手順 |
| [docker-network-diagram.md](setup/docker-network-diagram.md) | Docker のネットワーク・ポート構成図 |
| [pgadmin-setup.md](setup/pgadmin-setup.md) | pgAdmin 4 のセットアップ |

## event-sourcing/ — イベントソーシング（ES+CQRS）への対応

現行コードは ES+CQRS を使っていない。ES 対応は別リポジトリで行う予定で、ここはその資料。

| ファイル | 内容 |
|---|---|
| [README.md](event-sourcing/README.md) | このフォルダの位置づけ |
| [motivation.md](event-sourcing/motivation.md) | なぜ ES+CQRS を入れるのか（現行構成の課題） |
| [changes-from-current.md](event-sourcing/changes-from-current.md) | 現行構成から何が変わるか（コード・DB・テスト） |
| [es-cqrs-architecture.md](event-sourcing/es-cqrs-architecture.md) | 以前の実装の処理フロー全体図 |
| [es-cqrs-poc.md](event-sourcing/es-cqrs-poc.md) | 以前の PoC 実装の説明 |
| [read-model-design.md](event-sourcing/read-model-design.md) | Read Model の設計 |

## 記録

| ファイル | 内容 |
|---|---|
| [issue.md](issue.md) | 未解決の問題（見つかったが、まだ直していないもの） |
| [test-specification.md](test-specification.md) | テスト仕様書 |
| [troubleshooting/troubleshooting.md](troubleshooting/troubleshooting.md) | アプリのトラブルシューティング記録 |
| [troubleshooting/docker-troubleshooting.md](troubleshooting/docker-troubleshooting.md) | Docker のトラブルシューティング記録 |
