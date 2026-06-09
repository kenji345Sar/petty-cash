# pgAdmin 4 セットアップガイド

## インストール

```bash
brew install --cask pgadmin4
```

## 起動

- Finder → アプリケーション → **pgAdmin 4** をダブルクリック
- またはターミナルから:
```bash
open -a "pgAdmin 4"
```

## 初回起動時

1. **マスターパスワードの設定**を求められる
   - pgAdmin 自体のパスワード（DB のパスワードとは別）
   - 忘れないように控えておく

## サーバー登録（DB への接続設定）

1. 左パネルの「Servers」を右クリック → **Register** → **Server...**

2. **General タブ:**

   | 項目 | 入力値 |
   |------|--------|
   | Name | `petty-cash-local`（表示名。自由に設定可） |

3. **Connection タブ:**

   | 項目 | 入力値 |
   |------|--------|
   | Host name/address | `localhost` |
   | Port | `5432` |
   | Maintenance database | `petty_cash` |
   | Username | `postgres` |
   | Password | `postgres` |
   | Save password? | ON にする |

   **注意:** Host name/address には `localhost` と入力する。サーバー名（petty-cash-local）を入れないこと。

4. **Save** をクリック

## Docker DB のサーバー登録

Docker環境で起動したDBに接続する場合は、別のサーバーとして登録します。

1. 左パネルの「Servers」を右クリック → **Register** → **Server...**

2. **General タブ:**

   | 項目 | 入力値 |
   |------|--------|
   | Name | `petty-cash-docker`（表示名。自由に設定可） |

3. **Connection タブ:**

   | 項目 | 入力値 |
   |------|--------|
   | Host name/address | `localhost` |
   | Port | `5434` |
   | Maintenance database | `petty_cash` |
   | Username | `postgres` |
   | Password | `postgres` |
   | Save password? | ON にする |

   **ローカルDBとの違いはポート番号のみ**（5432 → 5434）。

4. **Save** をクリック

> **前提**: `docker compose up -d` でコンテナが起動している必要があります。
> コンテナが停止していると接続エラーになります。

### ローカル DB と Docker DB の見分け方

| サーバー名 | ポート | データ |
|-----------|--------|--------|
| `petty-cash-local` | 5432 | ローカル開発で蓄積したデータ |
| `petty-cash-docker` | 5434 | Docker初回起動時にシードされたデータ（名古屋・梅田・銀座） |

両方のDBは独立しているため、片方でデータを変更してももう片方には影響しません。

## テーブルの確認方法

接続後、左のツリーを以下の順で展開する：

```
petty-cash-local
  → Databases
    → petty_cash
      → Schemas
        → public
          → Tables
```

## 現在のテーブル一覧

| テーブル名 | 内容 |
|-----------|------|
| `change_bags` | 釣り銭バッグ（金庫管理） |
| `cash_bags` | キャッシュバッグ（レジ管理） |
| `transactions` | 出納帳（全ての入金・出金記録） |
| `denomination_checks` | 金種チェック記録 |

## テーブルのデータを見る

テーブルを右クリック → **View/Edit Data** → **All Rows** でデータが表示される。

## SQL を直接実行する

上部メニューの **Tools** → **Query Tool** で SQL エディタが開く。

例：
```sql
SELECT * FROM change_bags;
SELECT * FROM transactions ORDER BY created_at DESC;
```
