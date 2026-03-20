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
