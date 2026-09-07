# Petty Cash（小口現金管理システム）

## 必要なもの

- Node.js 22（nvm 推奨）
- .NET 8 SDK
- PostgreSQL 15+

## セットアップ

### 1. Node.js

```bash
nvm install    # .nvmrc を読んで Node 22 をインストール
nvm use        # Node 22 に切り替え
```

### 2. PostgreSQL

データベースとユーザーを作成する。

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

起動時にマイグレーションが自動実行される（`Program.cs`）。
デフォルトで `http://localhost:5001` で起動。

### 4. フロントエンド

```bash
cd frontend
npm install
npm run dev
```

`http://localhost:5173` で起動。

## 開発時の起動手順（毎日）

```bash
# ターミナル1: バックエンド
cd backend/PettyCash.Api && dotnet run

# ターミナル2: フロントエンド
cd frontend && nvm use && npm run dev
```

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
```
