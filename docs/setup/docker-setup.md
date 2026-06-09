# Docker セットアップガイド

## 前提条件

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) がインストール済みであること

## 構成

| サービス | 説明 | ポート |
|---------|------|--------|
| db | PostgreSQL 16 | 5434 |
| backend | .NET 9 ASP.NET Core API | 5141 |
| frontend | Vite + React | 5174 (dev) / 8080 (prod) |

## 開発モード

ソースコードの変更がホットリロードで即座に反映されます。

```bash
# 起動
docker compose up

# バックグラウンドで起動
docker compose up -d

# ログ確認
docker compose logs -f

# 特定サービスのログのみ
docker compose logs -f backend

# 停止
docker compose down
```

起動後のアクセス先：
- フロントエンド: http://localhost:5174
- API: http://localhost:5141/api/safes

## 本番モード

フロントエンドはビルド済みの静的ファイルを nginx で配信します。

```bash
# ビルド＆起動
docker compose -f docker-compose.prod.yml up --build

# バックグラウンドで起動
docker compose -f docker-compose.prod.yml up --build -d

# 停止
docker compose -f docker-compose.prod.yml down
```

起動後のアクセス先：
- フロントエンド: http://localhost:8080
- API（nginx経由）: http://localhost:8080/api/safes

## よく使うコマンド

```bash
# コンテナの状態確認
docker compose ps

# イメージの再ビルド（Dockerfile変更時）
docker compose build

# DBデータも含めて完全リセット
docker compose down -v

# 特定サービスだけ再起動
docker compose restart backend

# コンテナ内でコマンド実行
docker compose exec backend bash
docker compose exec db psql -U postgres petty_cash
```

## トラブルシューティング

### Docker daemon が起動していない

```
Cannot connect to the Docker daemon at unix:///var/run/docker.sock.
```

→ Docker Desktop アプリを起動してください。

### ポートが既に使用されている

```
Error: bind: address already in use
```

→ ローカルで PostgreSQL や他のサービスが同じポートを使用している可能性があります。
ローカルの PostgreSQL を停止するか、`docker-compose.yml` のポートマッピングを変更してください。

### 他のプロジェクト（日報等）と同時起動時のポート競合

日報管理システムなど他のDockerプロジェクトがポート5173を使用している場合、petty-cashのフロントエンドと競合します。

**対応済み**: petty-cashのフロントエンドはホスト側ポートを **5174** に変更済みです。

```yaml
# docker-compose.yml
frontend:
  ports:
    - "5174:5173"  # ホスト:5174 → コンテナ内:5173
```

同時に以下のファイルも変更しています：
- `backend/PettyCash.Api/appsettings.Development.json` — CORSに `http://localhost:5174` を追加
- `backend/PettyCash.Api/Program.cs` — CORSフォールバック値に `http://localhost:5174` を追加

### バックエンドにホストからアクセスできない

`curl http://localhost:5141/api/safes` が接続エラーになる場合、バックエンドがコンテナ内の`localhost`のみにバインドされている可能性があります。

**原因**: `Properties/launchSettings.json` の `applicationUrl` が `http://localhost:5141` だと、`dotnet watch run` がDockerfileの `ASPNETCORE_URLS=http://+:5141` を上書きしてしまいます。

**対応済み**: `launchSettings.json` の `applicationUrl` を `http://0.0.0.0:5141` に変更済みです。

### CORSエラー（Access-Control-Allow-Origin）

ブラウザのコンソールに以下のエラーが出る場合：

```
Access to fetch at 'http://localhost:5141/api/safes' from origin 'http://localhost:5174'
has been blocked by CORS policy
```

**原因**: `appsettings.Development.json` の `CorsOrigins` にフロントエンドのオリジンが含まれていません。

**対応**: `appsettings.Development.json` の `CorsOrigins` 配列にフロントエンドのURLを追加してください。

```json
"CorsOrigins": [
  "http://localhost:5173",
  "http://localhost:5174",
  "http://localhost:80",
  "http://localhost"
]
```

> **注意**: `Program.cs` のフォールバック値ではなく `appsettings.Development.json` の設定が優先されます。CORS変更後は `docker compose restart backend` でコンテナ再起動が必要です（ホットリロードではProgram.csの起動時設定は反映されません）。

### Docker環境とローカル環境のDB差異

Docker環境とローカル開発環境では**別々のPostgreSQLデータベース**を使用しています。

| 環境 | DBホスト | ポート | データボリューム |
|------|---------|--------|----------------|
| ローカル開発 | localhost | 5432 | ローカルPostgreSQLのデータ |
| Docker | db（コンテナ） | 5434（ホスト側） | `pgdata` Dockerボリューム |

Docker初回起動時はDBが空のため、シードデータ（金庫: 名古屋・梅田・銀座）のみが自動作成されます。ローカルで入力した既存データはDockerのDBには含まれません。

### 初回ビルドが遅い

初回はベースイメージ（.NET SDK 約800MB, Node.js 約130MB, PostgreSQL 約230MB）のダウンロードが必要です。
2回目以降はキャッシュが効くため数秒で完了します。
