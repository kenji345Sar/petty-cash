# Docker ネットワーク・ポート構成図

## 基礎知識

### ポートとは

ポートは**OSI参照モデルの第4層（トランスポート層）**の概念。
IPアドレスが「どのマシンか」を特定し、ポート番号が「そのマシン上のどのプロセスか」を特定する。

```
localhost:5141
    ↓         ↓
  IPアドレス  ポート番号
 （どのマシン）（どのプロセス）
```

- ポート番号は **0〜65535**（TCPヘッダーが16ビット = 2^16 = 65536）
- **1つのポートに同時に1つのプロセスしかバインドできない** ← 衝突の原因
- 0〜1023: ウェルノウンポート（80=HTTP, 443=HTTPS, 22=SSH, 5432=PostgreSQL等）
- 1024以上: アプリケーションが自由に使える

### コンテナとは

**アプリの実行環境を丸ごとパッケージ化したもの**。自分のPCに直接インストールせずにアプリを動かせる。

```
通常:
  自分のPC → PostgreSQLをインストール → 設定 → 起動
  自分のPC → Node.jsをインストール → npm install → 起動
  → 環境が汚れる、バージョン違いで動かない等の問題

コンテナ:
  自分のPC → docker compose up
  → PostgreSQL、Node.js、ASP.NET Core が各コンテナ内で独立して起動
  → 自分のPCには何もインストールしなくていい
```

```
ホスト（自分のmacOS）
┌──────────────────────────────────┐
│                                  │
│  コンテナA（PostgreSQL）          │  ← 独立した小さなLinux環境
│  ┌────────────────────────┐      │     中にPostgreSQLが入っている
│  │ PostgreSQL :5432       │      │     自分のPCにインストール不要
│  └────────────────────────┘      │
│                                  │
│  コンテナB（ASP.NET Core）        │  ← 別の独立した環境
│  ┌────────────────────────┐      │
│  │ ASP.NET Core :5141     │      │
│  └────────────────────────┘      │
│                                  │
│  コンテナC（Vite + React）        │
│  ┌────────────────────────┐      │
│  │ Node.js + Vite :5173   │      │
│  └────────────────────────┘      │
│                                  │
└──────────────────────────────────┘
```

各コンテナは**隔離された環境**。互いに干渉しない。

### ポートマッピング — なぜ必要か

コンテナは隔離されているので、**そのままではホストからアクセスできない**。
ポートマッピングで「ホストのポート → コンテナのポート」を繋いで初めて外からアクセスできる。

```
ポートマッピングなし:
  ブラウザ → localhost:5173 → ???  ← コンテナの中は見えない

ポートマッピングあり（"5174:5173"）:
  ブラウザ → localhost:5174 → Docker → コンテナ内:5173 → Viteが応答
               ホスト側         転送        コンテナ側
```

```yaml
# docker-compose.yml での書き方
ports:
  - "5174:5173"
#    ↑      ↑
#  ホスト  コンテナ内
```

コンテナ内のポートは変更不要（アプリのデフォルト設定のまま）。
ホスト側のポートだけを変えて競合を回避する。

### Docker Compose とは

**複数のコンテナをまとめて定義・起動する仕組み**。

```yaml
# docker-compose.yml = コンテナの設計図
services:
  db:        # コンテナA: PostgreSQL
    image: postgres:16-alpine
    ports: ["5434:5432"]

  backend:   # コンテナB: ASP.NET Core
    build: ./backend
    ports: ["5141:5141"]

  frontend:  # コンテナC: Vite + React
    build: ./frontend
    ports: ["5174:5173"]
```

`docker compose up` で3つのコンテナが一括で起動する。

### 衝突が起きる仕組み

```
ホストのポート5173を使いたいプロセスが2つある:

  1. 日報のnode（ローカルで直接起動）    → :5173 を使用中
  2. petty-cashのDockerコンテナ          → :5173 にマッピングしたい

  → 1つのポートに2つは入れない → 衝突！
  → petty-cashのホスト側を :5174 にずらして解決
```

### Dockerなし（ローカル開発）の仕組み

Dockerを使わない場合は、自分のPC上で直接プロセスを起動する。

```
ローカル開発:
┌────────────────────────────────────────────┐
│  ホスト（自分のmacOS）                       │
│                                            │
│  PostgreSQL     :5432  ← brewでインストール済 │
│  ASP.NET Core   :5141  ← dotnet run で起動   │
│  Vite + React   :5173  ← npm run dev で起動  │
│                                            │
│  全部が同じマシン上で直接動いている            │
│  コンテナなし、隔離なし                       │
└────────────────────────────────────────────┘
```

```bash
# 起動手順（ターミナル3つ）
# ターミナル1: PostgreSQLは常駐（brewで自動起動済み）
# ターミナル2: cd backend/PettyCash.Api && dotnet run
# ターミナル3: cd frontend && npm run dev
```

| | ローカル開発 | Docker |
|---|---|---|
| 起動方法 | 3つのターミナルで手動起動 | `docker compose up` 1コマンド |
| 前提条件 | PostgreSQL、.NET SDK、Node.jsをPCにインストール | Dockerだけあればいい |
| DB | PCにインストールしたPostgreSQL | コンテナ内のPostgreSQL（別のDB） |
| データ | ローカルDBのデータ | Dockerボリューム（別のデータ） |
| ポート | 全てホスト上で直接使用 | ポートマッピングでホストに公開 |

**DBのデータが違う理由:**

```
ローカル:
  PostgreSQL（brewインストール）
    → データは /usr/local/var/postgres/ に保存
    → 既存の取引データがある

Docker:
  PostgreSQL（コンテナ内）
    → データは pgdata ボリュームに保存（別の場所）
    → 初回起動時は空 → シードデータのみ
```

同じ`petty_cash`というDB名でも、物理的に別のPostgreSQLプロセス、別のデータファイル。

### Mac / Windows / WSL2 の違い

#### Mac

```
macOS
┌──────────────────────────────────┐
│  ファイルシステムは1つ             │
│  /Users/apple/petty-cash/        │
│                                  │
│  Docker Desktop                  │
│    → コンテナがmacOSのファイルを   │
│      直接マウントして使う          │
└──────────────────────────────────┘
```

シンプル。ファイルの場所に悩むことはない。

#### Windows（ローカル直接、Docker未使用）

```
Windows
┌──────────────────────────────────┐
│  ファイルシステムは1つ             │
│  C:\Users\apple\petty-cash\      │
│                                  │
│  PostgreSQL → Windowsインストーラ  │
│  .NET SDK   → Windowsインストーラ  │
│  Node.js    → Windowsインストーラ  │
└──────────────────────────────────┘
```

Macのローカル開発と同じ構造。パスが `\` になるだけ。

#### Windows + WSL2 + Docker

```
Windows
┌─────────────────────────────────────────────┐
│                                             │
│  Windows側のファイルシステム                   │
│  C:\Users\apple\...                         │
│                                             │
│  ┌─── WSL2（Linux仮想環境）───────────────┐  │
│  │                                       │  │
│  │  Linux側のファイルシステム（別物）       │  │
│  │  /home/apple/...                      │  │
│  │                                       │  │
│  │  Docker Engine はここで動く            │  │
│  │    → コンテナもここで動く              │  │
│  │                                       │  │
│  │  Windows側のファイルも見える:           │  │
│  │  /mnt/c/Users/apple/...               │  │
│  │  ↑ 遅い（ファイルシステムをまたぐ）     │  │
│  │                                       │  │
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
```

WSL2は**Windows内にLinuxマシンがもう1台ある**状態。
ファイルシステムが2つあるのが混乱ポイント。

**コードをどこに置くかで性能が大きく変わる:**

```
パターン1: Windows側にコードを置く（遅い）
  C:\Users\apple\petty-cash\
  → WSL2からは /mnt/c/Users/apple/petty-cash/ として見える
  → ファイルシステムをまたぐので遅い（docker compose upに数分かかることも）

パターン2: WSL2のLinux側にコードを置く（速い・推奨）
  /home/apple/petty-cash/
  → WSL2内で完結する → 速い
  → VS Codeは "Remote - WSL" 拡張でWSL内のファイルを直接編集できる
```

| 環境 | コード配置 | パス例 | Docker性能 |
|---|---|---|---|
| Mac | ローカル | `/Users/apple/petty-cash/` | 普通 |
| Windows直接 | ローカル | `C:\Users\apple\petty-cash\` | Docker未使用 |
| WSL2 + Windows側 | Windows | `/mnt/c/Users/apple/petty-cash/` | **遅い** |
| WSL2 + Linux側 | WSL2内 | `/home/apple/petty-cash/` | **速い（推奨）** |

**WSL2を使う場合の推奨構成:**

```bash
# WSL2のターミナルで
cd /home/apple
git clone <リポジトリURL> petty-cash
cd petty-cash
docker compose up

# VS Codeで開く
code .
# → "Remote - WSL" 拡張が自動的にWSL内のファイルを開く
```

---

## 全体図 — ホスト・コンテナ・ポートの関係

```
┌─────────────────────────────────────────────────────────────────────────┐
│  ホストマシン（macOS）                                                    │
│                                                                         │
│  ┌──────────────────────┐                                               │
│  │ ローカルプロセス       │                                               │
│  │                      │                                               │
│  │  PostgreSQL :5432    │  ← ローカルDB（petty-cash + 日報の開発用）      │
│  │  node       :5173    │  ← 日報フロントエンド（npm run dev）            │
│  └──────────────────────┘                                               │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │ Docker                                                         │    │
│  │                                                                 │    │
│  │  ┌─── petty-cash（docker-compose.yml）──────────────────────┐   │    │
│  │  │                                                          │   │    │
│  │  │  frontend  :5174 ─────→ コンテナ内 :5173 (Vite)         │   │    │
│  │  │  backend   :5141 ─────→ コンテナ内 :5141 (ASP.NET Core) │   │    │
│  │  │  db        :5434 ─────→ コンテナ内 :5432 (PostgreSQL)   │   │    │
│  │  │                                                          │   │    │
│  │  │  コンテナ間通信:                                          │   │    │
│  │  │    backend → db:5432（ホストを経由しない）                │   │    │
│  │  │    frontend → ホスト:5141（ブラウザ経由）                 │   │    │
│  │  └──────────────────────────────────────────────────────────┘   │    │
│  │                                                                 │    │
│  │  ┌─── daily-report（日報のdocker-compose.yml）─────────────┐   │    │
│  │  │                                                          │   │    │
│  │  │  db        :5433 ─────→ コンテナ内 :5432 (PostgreSQL)   │   │    │
│  │  │                                                          │   │    │
│  │  └──────────────────────────────────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  ブラウザ                                                               │
│    http://localhost:5174  → petty-cash フロントエンド                    │
│    http://localhost:5173  → 日報 フロントエンド（ローカルnode）           │
│    http://localhost:5141  → petty-cash API（直接アクセス）               │
└─────────────────────────────────────────────────────────────────────────┘
```

## ポート一覧

| ポート | 使用者 | 種別 | 備考 |
|-------|--------|------|------|
| **5173** | 日報フロントエンド | ローカルnode | npm run devで起動 |
| **5174** | petty-cash フロントエンド | Docker → ホスト | コンテナ内は5173、ホスト側を5174にずらして競合回避 |
| **5141** | petty-cash バックエンド | Docker → ホスト | ASP.NET Core API |
| **5432** | ローカル PostgreSQL | ローカルプロセス | ローカル開発用DB |
| **5433** | 日報 Docker DB | Docker → ホスト | 日報のPostgreSQL |
| **5434** | petty-cash Docker DB | Docker → ホスト | petty-cashのPostgreSQL |

## よくある衝突パターンと対処

### パターン1: フロントエンドのポート競合（5173）

```
問題:
  日報（ローカルnode）  :5173
  petty-cash（Docker） :5173  ← 同じポート！

症状: ブラウザで localhost:5173 を開くと日報が表示される

対処: petty-cashのホスト側ポートを5174にずらした
  docker-compose.yml: "5174:5173"
```

### パターン2: DBのポート競合（5432）

```
問題:
  ローカルPostgreSQL    :5432
  petty-cash Docker DB :5432  ← 同じポート！

症状: Docker DBが起動できない、またはどちらのDBに繋がっているかわからない

対処: Docker側のホストポートをずらした
  petty-cash: "5434:5432"
  日報:       "5433:5432"
```

### パターン3: CORSエラー

```
問題:
  フロントエンド（localhost:5174）→ API（localhost:5141）
  オリジンが異なるためブラウザがブロック

症状: コンソールに "Access-Control-Allow-Origin" エラー

対処: appsettings.Development.json の CorsOrigins にフロントのURLを追加
  "CorsOrigins": ["http://localhost:5173", "http://localhost:5174", ...]

注意: Program.cs のフォールバック値ではなく appsettings が優先される
```

### パターン4: バックエンドにアクセスできない

```
問題:
  バックエンドが localhost:5141 にバインド
  → コンテナ内のlocalhostであり、ホストからアクセスできない

症状: curl http://localhost:5141/api/safes が接続エラー

原因: launchSettings.json の applicationUrl が "http://localhost:5141"
  → dotnet watch が ASPNETCORE_URLS=http://+:5141 を上書きしてしまう

対処: launchSettings.json を "http://0.0.0.0:5141" に変更
```

## ポートマッピングの仕組み

```
ホスト側ポート : コンテナ内ポート

"5174:5173" の意味:
  ホストの5174番ポートへのアクセスを、コンテナ内の5173番ポートに転送する

  ブラウザ → localhost:5174 → Docker → コンテナ内:5173 → Viteが応答
```

コンテナ内のポートは変更不要（アプリのデフォルト設定のまま）。
ホスト側のポートだけを変えて競合を回避する。

## コンテナ間通信 vs ホスト経由通信

```
コンテナ間通信（Docker内部ネットワーク）:
  backend → db:5432     ← サービス名で直接通信、ホストを経由しない
  ホスト側のポート（5434）は関係ない

  docker-compose.yml:
    ConnectionStrings: "Host=db;Port=5432;..."
    ※ Host=db はDockerのサービス名、Host=localhost ではない

ホスト経由通信（ブラウザ → API）:
  ブラウザ → localhost:5141 → Docker → backend:5141
  ※ ブラウザはDocker内部ネットワークにアクセスできないので、ホスト側ポートを使う

  フロントエンド:
    VITE_API_BASE: "http://localhost:5141/api"
    ※ "http://backend:5141/api" ではない（ブラウザから見えない）
```

## pgAdminからの接続

```
ローカルDB:
  Host: localhost  Port: 5432  DB: petty_cash

petty-cash Docker DB:
  Host: localhost  Port: 5434  DB: petty_cash

日報 Docker DB:
  Host: localhost  Port: 5433  DB: （日報のDB名）

※ 全て Host は localhost だがポートで区別する
```
