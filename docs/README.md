# docs — 目次

目的別に分けている。仕様を知りたいときは、まず [spec/](spec/README.md) から読む。

| 目的 | フォルダ |
|---|---|
| 仕様（何ができるか・業務ルール） | [spec/](spec/README.md) |
| 業務とコードの対応・設計判断 | [architecture/](architecture/) |
| 画面と API の調べ方 | [api-screens/](api-screens/) |
| C#・EF Core の学習ノート | [csharp/](csharp/) |
| 環境構築 | [setup/](setup/) |
| 記録（トラブル・テスト・過去の設計） | [troubleshooting/](troubleshooting/)・[archive/](archive/README.md)・[test-specification.md](test-specification.md) |

---

## spec/ — 仕様書の本体

| ファイル | 内容 |
|---|---|
| [README.md](spec/README.md) | 仕様書の入口・全体像 |
| [common.md](spec/common.md) | 小口・業者で共通する概念 |
| [petty-cash.md](spec/petty-cash.md) | 小口タブの仕様 |
| [vendor.md](spec/vendor.md) | 業者タブの仕様 |

## architecture/ — 業務とコードの対応・設計判断

| ファイル | 内容 |
|---|---|
| [business-flow.md](architecture/business-flow.md) | 業務フロー（コードの説明なし） |
| [business-flow-to-code.md](architecture/business-flow-to-code.md) | 業務の流れに沿って、どのコードが動くかを対応づけ |
| [current-architecture.md](architecture/current-architecture.md) | API・処理フローの全体図 |
| [ddd-layer-architecture.md](architecture/ddd-layer-architecture.md) | DDD レイヤー構成の解説 |
| [domain-rules.md](architecture/domain-rules.md) | 業務ルールがコードのどこにあるか |
| [unit-of-work.md](architecture/unit-of-work.md) | UnitOfWork パターン |
| [denomination-vs-check.md](architecture/denomination-vs-check.md) | 金種表と有高チェックの設計方針 |

## api-screens/ — 画面と API の調べ方

| ファイル | 内容 |
|---|---|
| [api-endpoints.md](api-screens/api-endpoints.md) | API エンドポイント一覧 |
| [display-logic.md](api-screens/display-logic.md) | 画面の表示ロジック |
| [frontend-api-flow.md](api-screens/frontend-api-flow.md) | フロントエンド → API 呼び出しの概要 |
| [frontend-api-flow-detail.md](api-screens/frontend-api-flow-detail.md) | 同上の詳細（1操作の HTTP リクエストを追う） |
| [vendor-screen-data-flow.md](api-screens/vendor-screen-data-flow.md) | 業者管理画面の表示データの出どころ |
| [how-to-investigate-screen.md](api-screens/how-to-investigate-screen.md) | 画面からコードを調査する手順 |

## csharp/ — C# 学習ノート

| ファイル | 内容 |
|---|---|
| [csharp-concepts.md](csharp/csharp-concepts.md) | C# の基本概念 |
| [routing.md](csharp/routing.md) | ASP.NET Core のルーティング規則 |
| [usecase-domain-repository-flow.md](csharp/usecase-domain-repository-flow.md) | UseCase・Domain・Repository の流れ |
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

## 記録

| ファイル | 内容 |
|---|---|
| [test-specification.md](test-specification.md) | テスト仕様書 |
| [troubleshooting/troubleshooting.md](troubleshooting/troubleshooting.md) | アプリのトラブルシューティング記録 |
| [troubleshooting/docker-troubleshooting.md](troubleshooting/docker-troubleshooting.md) | Docker のトラブルシューティング記録 |
| [archive/](archive/README.md) | 削除済みの ES+CQRS の設計記録（現行コードとは一致しない） |
