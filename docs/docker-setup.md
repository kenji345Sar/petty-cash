# Docker セットアップガイド

## 前提条件

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) がインストール済みであること

## 構成

| サービス | 説明 | ポート |
|---------|------|--------|
| db | PostgreSQL 16 | 5432 |
| backend | .NET 9 ASP.NET Core API | 5141 |
| frontend | Vite + React | 5173 (dev) / 8080 (prod) |

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
- フロントエンド: http://localhost:5173
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

### 初回ビルドが遅い

初回はベースイメージ（.NET SDK 約800MB, Node.js 約130MB, PostgreSQL 約230MB）のダウンロードが必要です。
2回目以降はキャッシュが効くため数秒で完了します。
