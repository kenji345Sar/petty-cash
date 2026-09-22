# Petty Cash（小口現金管理システム）

業務経験をもとに、個人で設計・実装した学習用プロジェクトです。

## 設計方針：ドメイン駆動設計（DDD）

業務ルールをドメイン層（`PettyCash.Domain`）に集め、それ以外の層はドメインを「使う側」として分けている。

| 層 | プロジェクト | 役割 |
|---|---|---|
| API | PettyCash.Api | HTTP の受け口。業務ルールは持たない |
| アプリケーション | PettyCash.Application | ユースケース。ドメインを組み合わせて業務行為を実現する |
| **ドメイン** | **PettyCash.Domain** | **業務ルールそのもの（金庫・小口・業者）** |
| インフラ | PettyCash.Infrastructure | DB への読み書き（リポジトリの実装） |

どう実現しているかは docs を参照：

- [DDD レイヤー構成](docs/architecture/01-ddd-layers.md)：各層の責務とコード例
- [業務ルールの在り処](docs/architecture/02-domain-model.md)：どのルールがドメインのどこにあるか
- [業務フロー → コード対応](docs/architecture/03-flow-to-code.md)：画面の操作でどのコードが動くか

読む順番は [docs/architecture/README.md](docs/architecture/README.md) を参照。

仕様（何ができるか）は [docs/spec/](docs/spec/README.md)、docs 全体の目次は [docs/README.md](docs/README.md)。

## 必要なもの

- Node.js 22（nvm 推奨）
- .NET 8 SDK
- PostgreSQL 15+

## セットアップ（初回のみ）

2回目以降は「[開発時の起動手順（毎日）](#開発時の起動手順毎日)」だけを見ればよい。

### 1. Node.js

```bash
nvm install    # .nvmrc を読んで Node 22 をインストール
nvm use        # Node 22 に切り替え
```

### 2. PostgreSQL

PostgreSQL は Homebrew のサービスとして常駐させる前提。一度登録すれば Mac ログイン時に自動起動する。

```bash
brew services start postgresql@16   # 初回のみ（以降はログイン時に自動起動）
brew services list                  # 起動しているか確認
```

データベースとユーザーを作成する（**初回のみ**。2回目以降は不要）。

```sql
CREATE DATABASE petty_cash;
CREATE USER postgres WITH PASSWORD 'postgres';
```

接続情報は `backend/PettyCash.Api/appsettings.Development.json` で変更可能。

### 3. バックエンド

```bash
cd backend/PettyCash.Api
dotnet run
```

起動時に `EnsureCreated()` でテーブルが自動作成される（`Program.cs`）。テーブルが存在しない場合のみ作成し、既存テーブルの変更は行わない。
デフォルトで `http://localhost:5141` で起動。

### 4. フロントエンド

```bash
cd frontend
npm install
npm run dev
```

`http://localhost:5173` で起動。

## 開発時の起動手順（毎日）

PostgreSQL はログイン時に自動起動しているので操作不要（止まっている場合は `brew services start postgresql@16`）。

```bash
# ターミナル1: バックエンド
cd backend/PettyCash.Api && dotnet run

# ターミナル2: フロントエンド
cd frontend && nvm use && npm run dev
```

## Docker で開発環境を起動する（別の方法）

上記のセットアップの代わりに、`docker-compose.yml` で DB・バックエンド・フロントエンドをまとめて起動することもできる。DB はコンテナ内に自動作成されるため、PostgreSQL のセットアップ（手順2）は不要。

```bash
docker compose up
```

フロントエンドは `http://localhost:5174` で起動。バックエンドの 5141 は `dotnet run` と同じポートなので、両方を同時には起動できない。
ポート構成・ログ確認などの詳細は [docs/setup/docker-setup.md](docs/setup/docker-setup.md) を参照。

## Docker で本番構成を起動する

接続情報は `.env` から読み込む。`POSTGRES_PASSWORD` は必須で、未設定の場合は起動が中止される。

```bash
cp .env.example .env
# .env を編集して POSTGRES_PASSWORD を推測できない値にする
#   例: openssl rand -base64 32

docker compose -f docker-compose.prod.yml up -d
```

**注意:** `POSTGRES_PASSWORD` には推測できない値を設定すること。

`.env` は Git 管理外（`.gitignore` 済み）。設定できる項目は `.env.example` を参照。

## プロジェクト構成

```
backend/
  PettyCash.Api/            … ASP.NET Core Web API
  PettyCash.Application/    … ユースケース・DTO
  PettyCash.Domain/         … エンティティ・リポジトリIF
  PettyCash.Infrastructure/ … DB実装・サービス実装
frontend/
  src/                      … React + TypeScript（Vite）
docs/                       … 仕様書・設計解説・学習ノート（目次は docs/README.md）
```
