# ES+CQRS PoC 実装ドキュメント

## 概要

小口現金管理システムにEvent Sourcing（ES）+ CQRS（Command Query Responsibility Segregation）をPoCとして導入した。
対象スコープはSafeの残高管理に限定し、ライブラリ不使用のPostgreSQLイベントテーブル方式を採用。

## アーキテクチャ

### 変更前（純CRUD）

```
入金 → Transaction INSERT → 残高はSELECT SUM(transactions)で毎回計算
```

### 変更後（ES+CQRS）

```
入金 → 1. Transaction INSERT（従来のCRUDレコード）
     → 2. DomainEvent INSERT（イベントストア = ES）
     → 3. Safe.CurrentBalance UPDATE（Read Model = CQRSのQ側）
```

### 読み取り

```
変更前: SELECT SUM(CASE ...) FROM transactions WHERE safe_id = X
変更後: SELECT current_balance FROM safes WHERE id = X
```

## 構成要素

### 1. ドメインイベント（Domain層）

```
PettyCash.Domain/Events/
├── DomainEvent.cs           # 基底クラス
├── MoneyDepositedEvent.cs   # 入金イベント
├── MoneyWithdrawnEvent.cs   # 出金イベント
└── IEventStore.cs           # EventStoreインターフェース
```

**DomainEvent（基底）:**

| プロパティ | 型 | 説明 |
|-----------|------|------|
| EventId | Guid | イベント一意ID（自動生成） |
| OccurredAt | DateTime | 発生日時（UTC） |
| EventType | string | イベント種別名（抽象） |
| AggregateId | int | 対象集約のID |
| AggregateType | string | 対象集約の種別名（"Safe"等） |

**MoneyDepositedEvent / MoneyWithdrawnEvent（固有）:**

| プロパティ | 型 | 説明 |
|-----------|------|------|
| Amount | int | 金額 |
| Description | string | 摘要 |
| ChangeBagId | int? | 関連する釣り銭バッグID |
| CashBagId | int? | 関連するキャッシュバッグID |
| PrepBagId | int? | 関連する準備バッグID |

### 2. イベントストア（Infrastructure層）

```
PettyCash.Infrastructure/Events/
├── StoredEvent.cs           # DBエンティティ
└── PostgresEventStore.cs    # IEventStore実装
```

**domain_events テーブル:**

```sql
CREATE TABLE domain_events (
    id BIGSERIAL PRIMARY KEY,
    event_id UUID NOT NULL,
    aggregate_id INTEGER NOT NULL,
    aggregate_type VARCHAR(100) NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data JSONB NOT NULL,          -- イベントの全データをJSON保存
    occurred_at TIMESTAMP NOT NULL
);
CREATE INDEX idx_domain_events_aggregate ON domain_events (aggregate_id, aggregate_type);
```

**PostgresEventStore の役割:**
- `AppendAsync`: ドメインイベントをJSONシリアライズして保存
- `GetEventsAsync`: 集約ID+種別でイベント履歴を時系列取得
- デシリアライズ時に`event_type`でイベントクラスを判別

### 3. Read Model（Safe.CurrentBalance）

**変更前:**
```csharp
// 計算プロパティ（毎回Transactionから算出）
public int CurrentBalance => deposits - withdrawals;
```

**変更後:**
```csharp
// 永続化フィールド（DBに保存）
public int CurrentBalance { get; private set; }

// イベント適用メソッド
public void ApplyDeposit(int amount)  => CurrentBalance += amount;
public void ApplyWithdrawal(int amount) => CurrentBalance -= amount;
```

safesテーブルに`current_balance`カラムを追加済み。

## データフロー詳細

### Command側（書き込み）

```
UseCase.ExecuteAsync(dto)
  │
  ├─ 1. Transaction INSERT     ← 従来のCRUD（出納帳表示用に残す）
  │
  ├─ 2. EventStore.AppendAsync  ← domain_eventsにイベントINSERT
  │     MoneyDepositedEvent {
  │       aggregateId: 1,
  │       aggregateType: "Safe",
  │       amount: 12000,
  │       description: "小口入金"
  │     }
  │
  └─ 3. Safe.ApplyDeposit(amount)  ← Read Model更新
        SafeRepository.UpdateAsync  ← safes.current_balance UPDATE
```

### Query側（読み取り）

```
GET /api/safes
  → SafeRepository.GetAllAsync()
    → SELECT id, name, current_balance FROM safes
      （Transactionの全件SUMは不要）
```

## イベント発行箇所

| UseCase | イベント | 説明 |
|---------|---------|------|
| CreateTransactionUseCase | MoneyDeposited / MoneyWithdrawn | 手動入出金 |
| DepositBagUseCase | MoneyDeposited | 釣り銭バッグ入金 |
| MoveBagToRegisterUseCase | MoneyWithdrawn | 釣り銭バッグ出金（レジへ移動） |
| DepositCashBagUseCase | MoneyDeposited | キャッシュバッグ入金 |
| HandOverPrepBagUseCase | MoneyWithdrawn | 準備バッグ引渡 |

## マイグレーション

Program.csの起動時マイグレーションで以下を実行:

1. `domain_events`テーブル作成
2. `safes`に`current_balance`カラム追加（DEFAULT 0）
3. 既存Transactionから各金庫の残高を算出して`current_balance`を初期化
4. 既存Transactionから`domain_events`にイベント履歴を自動生成

```sql
-- 残高初期化
UPDATE safes SET current_balance = COALESCE((
    SELECT SUM(CASE WHEN t.type = 0 THEN t.amount ELSE -t.amount END)
    FROM transactions t WHERE t.safe_id = safes.id
), 0);

-- イベント履歴移行
INSERT INTO domain_events (...)
SELECT ... FROM transactions t ORDER BY t.created_at;
```

## 現在の方式の特徴

### 二重書き込み方式（Dual Write）

現在はTransactionテーブルとdomain_eventsテーブルの両方に書き込む「二重書き込み」方式。

```
書き込み: Transaction INSERT + DomainEvent INSERT + Safe UPDATE（3回）
読み取り: safes.current_balance（1回、O(1)）
```

**メリット:**
- 既存の出納帳表示（Transactionベース）がそのまま動作
- Read Modelにより残高取得がO(1)
- 段階的な移行が可能

**制約:**
- Transactionとイベントの整合性は同一DbContext内のSaveChangesで保証
- 分散トランザクションではないため、プロセス障害時のデータ不整合リスクあり（PoCレベルでは許容）

### 純粋なESとの違い

| 観点 | 純粋なES | 現在の実装 |
|------|---------|-----------|
| 真のデータ源 | イベントのみ | Transaction + イベント（二重） |
| 状態復元 | イベントリプレイ | Read Modelカラム参照 |
| Transactionテーブル | 不要（イベントで代替） | 残す（出納帳表示に使用） |
| スナップショット | 必要（パフォーマンス） | 不要（常にRead Model参照） |

### 本番化に向けた検討事項

1. **Outboxパターン**: 二重書き込みの整合性を保証するため、Outboxテーブル経由でイベントを非同期配信
2. **Marten導入**: PostgreSQL上のES専用ライブラリ。スナップショット、プロジェクション、バージョニングが組み込み
3. **Transaction廃止**: イベントを唯一の真のデータ源とし、出納帳はプロジェクションで生成
4. **イベントバージョニング**: スキーマ変更時のイベント互換性管理
