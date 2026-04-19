# ES+CQRS アーキテクチャ — 処理フロー全体図

## 用語とコードの対応

| ES+CQRS用語 | 説明 | 実装の場所 |
|---|---|---|
| **イベントストア** | 「何が起きたか」をpayloadで記録する場所 | `domain_events`テーブル / `EventStore.cs` |
| **Command** | 書き込み操作（状態を変更する処理） | 各UseCase（`CreatePettyCashTransactionUseCase`等） |
| **Query** | 読み込み操作（状態を変更しない） | `VendorLedgerQueryService` / `PettyCashLedgerQueryService` |
| **Read Model** | 読み込み専用テーブル | `safe_balances` / `vendor_ledger_view` / `petty_cash_ledger_view` |
| **プロジェクション** | イベント発生後にRead Modelを更新する処理 | `ProjectionService.cs` |
| **集約(Aggregate)** | ドメインの整合性境界 | `Safe`（domain_eventsの`aggregate_type`） |
| **ドメインイベント** | 発生した事実の記録 | `PettyCashDeposited`, `VendorMoneyWithdrawn`等（`event_type`） |
| **payload** | イベントの詳細データ（JSON） | `{"safeId":2, "amount":23000, "balance":23000, ...}` |

### 処理順序と用語の関係

```
    Command（書き込み）
        │
        ↓
    UseCase（ドメイン操作）
        │
        ├── transactionRepository.AddAsync()   … 従来のトランザクション記録
        │
        ├── eventStore.AppendAsync()            … イベントストアにドメインイベントを記録
        │     → domain_events テーブル
        │       { event_type: "PettyCashDeposited",
        │         payload: {"safeId":2, "amount":23000, ...} }
        │
        ├── projectionService.ProjectAsync()   … プロジェクション（Read Model更新）
        │     → safe_balances を UPDATE
        │     → ledger_view に INSERT
        │
        └── unitOfWork.SaveChangesAsync()       … 全て同一トランザクションでコミット


    Query（読み込み）
        │
        ↓
    QueryService
        │
        └── Read Model テーブルから取得
              → safe_balances（残高）
              → vendor_ledger_view（業者出納帳）
              → petty_cash_ledger_view（小口出納帳）
              ※ イベントストア・トランザクションテーブルは一切読まない
```

---

## 全体構成

```
フロントエンド (React)
    ↓ HTTP
コントローラー (API層)
    ↓
ユースケース (Application層)
    ↓
    ├── ドメインエンティティ生成（Domain層）
    ├── イベントストアに記録（EventStore → domain_events）        ← ES
    ├── トランザクションテーブルにINSERT（従来のCRUD記録）
    ├── Read Model を更新（ProjectionService）                    ← CQRS
    └── コミット

読み込み（Query側）:
    QueryService → Read Model テーブルから取得                   ← CQRS
    ※ イベントストア・トランザクションテーブルは一切読まない
```

---

## 1. 書き込みの流れ（Command側）

全ての書き込みユースケースは以下の順序で処理する。

```
UseCase.ExecuteAsync()
    ├── 1. ドメインエンティティ生成        … Domain層のファクトリメソッド
    ├── 2. sequenceNumberService.AssignAsync()  … 金庫単位の通し番号を採番
    ├── 3. balanceService.AssignBalanceAsync()  … ランニングバランスを計算
    ├── 4. transactionRepository.AddAsync()     … トランザクションテーブルにINSERT
    ├── 5. eventStore.AppendAsync()             … ★ domain_events にpayload記録（ES）
    ├── 6. projectionService.ProjectAsync()     … ★ Read Model を更新（CQRS）
    └── 7. unitOfWork.SaveChangesAsync()        … 全て同一トランザクションでコミット
```

### 1-1. 小口入金

```
フロント: POST /api/petty-cash-transactions
    ↓
PettyCashTransactionsController.Create()
    ↓
CreatePettyCashTransactionUseCase.ExecuteAsync()
    ├── PettyCashTransaction.Create()
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── transactionRepository.AddAsync()
    │     → petty_cash_transactions にINSERT
    ├── eventStore.AppendAsync()
    │     → domain_events にINSERT:
    │       { event_type: "PettyCashDeposited",
    │         payload: {"safeId":2,"amount":10000,"balance":79900,...} }
    ├── projectionService.ProjectAsync()
    │     → petty_cash_ledger_view にINSERT
    │     → safe_balances の petty_cash_balance を UPDATE
    └── unitOfWork.SaveChangesAsync()
```

### 1-2. 釣り銭バッグ入金

```
フロント: POST /api/bags/deposit
    ↓
BagsController.Deposit()
    ↓
DepositBagUseCase.ExecuteAsync()
    ├── ChangeBag.CreateDeposit()               … バッグ＋入金取引を同時生成
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── bagRepository.AddAsync()                … change_bags + vendor_transactions にINSERT
    ├── eventStore.AppendAsync()
    │     → domain_events: { event_type: "VendorMoneyDeposited", payload: {...} }
    ├── projectionService.ProjectAsync()
    │     → vendor_ledger_view にINSERT
    │     → safe_balances の vendor_balance を UPDATE
    └── unitOfWork.SaveChangesAsync()
```

### 1-3. 釣り銭バッグ → レジ移動（出金）

```
フロント: POST /api/bags/{id}/move
    ↓
BagsController.MoveToRegister()
    ↓
MoveBagToRegisterUseCase.ExecuteAsync()
    ├── bag.MoveToRegister()                    … 状態変更＋出金取引生成
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── bagRepository.UpdateAsync()
    ├── eventStore.AppendAsync()
    │     → domain_events: { event_type: "VendorMoneyWithdrawn", payload: {...} }
    ├── projectionService.ProjectAsync()
    │     → vendor_ledger_view にINSERT
    │     → safe_balances の vendor_balance を UPDATE
    └── unitOfWork.SaveChangesAsync()
```

### 1-4. キャッシュバッグ入金

```
フロント: POST /api/cashbags/deposit
    ↓
CashBagsController.Deposit()
    ↓
DepositCashBagUseCase.ExecuteAsync()
    ├── CashBag.CreateDeposit()
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── cashBagRepository.AddAsync()
    ├── eventStore.AppendAsync()
    │     → domain_events: { event_type: "VendorMoneyDeposited", payload: {...} }
    ├── projectionService.ProjectAsync()
    └── unitOfWork.SaveChangesAsync()
```

### 1-5. 準備バッグ引渡（出金）

```
フロント: POST /api/prepbags/{id}/handover
    ↓
PrepBagsController.HandOver()
    ↓
HandOverPrepBagUseCase.ExecuteAsync()
    ├── bag.MarkHandedOver()
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── vendorTransactionRepository.AddAsync()
    ├── eventStore.AppendAsync()
    │     → domain_events: { event_type: "VendorMoneyWithdrawn", payload: {...} }
    ├── projectionService.ProjectAsync()
    └── unitOfWork.SaveChangesAsync()
```

### 1-6. 有高チェック（差額調整あり）

```
フロント: POST /api/denominationchecks/changebag/{bagId}
         POST /api/denominationchecks/cashbag/{bagId}
         POST /api/denominationchecks/safe/{safeId}
    ↓
CheckChangeBagUseCase / CheckCashBagUseCase / CheckSafeUseCase
    ├── 金種チェック記録を生成
    ├── sequenceNumberService.AssignAsync(check)
    ├── checkRepository.AddAsync(check)
    │
    ├── if (差額 != 0):
    │     ├── 調整取引を生成
    │     ├── sequenceNumberService.AssignAsync(adjustment)
    │     ├── balanceService.AssignBalanceAsync(adjustment)
    │     ├── transactionRepository.AddAsync(adjustment)
    │     ├── eventStore.AppendAsync(adjustment)
    │     │     → domain_events: { event_type: "VendorBalanceAdjusted", payload: {...} }
    │     └── projectionService.ProjectAsync(adjustment)
    │
    └── unitOfWork.SaveChangesAsync()
```

---

## 2. 読み込みの流れ（Query側）

全ての読み込みは**Read Modelテーブルからのみ**取得する。イベントストア・トランザクションテーブルは読まない。

### 2-1. 小口出納帳表示

```
フロント: GET /api/pettycash-dashboard?safeId=2
    ↓
DashboardController.GetPettyCashDashboard()
    ↓
GetPettyCashDashboardUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync(2)
    │     └── SELECT * FROM safe_balances WHERE safe_id = 2   ★ Read Model
    │
    ├── pettyCashLedgerQuery.GetBySafeIdAsync(2)
    │     └── SELECT * FROM petty_cash_ledger_view WHERE safe_id = 2   ★ Read Model
    │
    └── getDenomChecks.ExecuteAsync(2)
          └── SELECT * FROM safe_denomination_checks WHERE safe_id = 2
```

### 2-2. 業者ダッシュボード表示

```
フロント: GET /api/vendor-dashboard?safeId=2
    ↓
DashboardController.GetVendorDashboard()
    ↓
GetVendorDashboardUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync(2)
    │     └── SELECT * FROM safe_balances WHERE safe_id = 2   ★ Read Model
    │
    ├── vendorLedgerQuery.GetBySafeIdAsync(2)
    │     └── SELECT * FROM vendor_ledger_view WHERE safe_id = 2   ★ Read Model
    │
    ├── getBags.ExecuteAsync()        → change_bags
    ├── getCashBags.ExecuteAsync()    → cash_bags
    ├── getDenomChecks.ExecuteAsync()  → vendor_denomination_checks
    └── getPrepBags.ExecuteAsync()    → prep_bags
```

### 2-3. 金庫一覧（ヘッダー残高表示）

```
フロント: GET /api/safes
    ↓
SafesController.GetAll()
    ↓
GetSafesUseCase.ExecuteAsync()
    └── safeRepository.GetAllAsync()
          └── 各金庫ごとに:
                SELECT * FROM safe_balances WHERE safe_id = X   ★ Read Model
```

---

## 3. イベントストア（domain_events）

### テーブル構造

```sql
CREATE TABLE domain_events (
    id           BIGSERIAL PRIMARY KEY,
    aggregate_type VARCHAR(100) NOT NULL,   -- "Safe"
    aggregate_id   INTEGER NOT NULL,        -- safe_id
    event_type     VARCHAR(100) NOT NULL,   -- イベント名
    payload        JSONB NOT NULL,          -- イベントデータ（JSON）
    created_at     TIMESTAMP NOT NULL
);
```

### イベント種別

| event_type | 発生条件 | payload内容 |
|-----------|---------|------------|
| VendorMoneyDeposited | 釣り銭/キャッシュバッグ入金 | safeId, amount, balance, description, changeBagId/cashBagId |
| VendorMoneyWithdrawn | レジ移動/準備バッグ引渡 | safeId, amount, balance, description, changeBagId/prepBagId |
| VendorBalanceAdjusted | 有高チェック差額調整 | safeId, amount, balance, description |
| PettyCashDeposited | 小口入金 | safeId, amount, balance, description |
| PettyCashWithdrawn | 小口出金 | safeId, amount, balance, description |
| PettyCashBalanceAdjusted | 金庫有高チェック差額調整 | safeId, amount, balance, description |

### payloadの例

```json
{
  "safeId": 2,
  "amount": 23000,
  "balance": 23000,
  "description": "小口入金",
  "sequenceNumber": 3
}
```

---

## 4. DB構成（ES+CQRS完成形）

| テーブル | 役割 | 読み/書き |
|---------|------|----------|
| **domain_events** | **イベントストア（ESの本体）** | **書き込みのみ** |
| vendor_transactions | トランザクション記録（従来互換） | 書き込みのみ |
| petty_cash_transactions | トランザクション記録（従来互換） | 書き込みのみ |
| **safe_balances** | **Read Model（残高）** | **読み込みのみ** |
| **vendor_ledger_view** | **Read Model（業者出納帳）** | **読み込みのみ** |
| **petty_cash_ledger_view** | **Read Model（小口出納帳）** | **読み込みのみ** |
| safes | 金庫マスタ | 読み書き |
| change_bags | 釣り銭バッグ | 読み書き |
| cash_bags | キャッシュバッグ | 読み書き |
| prep_bags | 準備バッグ | 読み書き |
| vendor_denomination_checks | 有高チェック記録 | 読み書き |
| safe_denomination_checks | 有高チェック記録 | 読み書き |

---

## 5. 全API一覧

### 書き込み（Command）API — イベントストア + トランザクション + Read Model に書き込む

| メソッド | エンドポイント | ユースケース | イベント |
|---------|-------------|------------|---------|
| POST | /api/petty-cash-transactions | CreatePettyCashTransactionUseCase | PettyCashDeposited / PettyCashWithdrawn |
| POST | /api/bags/deposit | DepositBagUseCase | VendorMoneyDeposited |
| POST | /api/bags/{id}/move | MoveBagToRegisterUseCase | VendorMoneyWithdrawn |
| POST | /api/cashbags/deposit | DepositCashBagUseCase | VendorMoneyDeposited |
| POST | /api/prepbags/{id}/handover | HandOverPrepBagUseCase | VendorMoneyWithdrawn |
| POST | /api/denominationchecks/changebag/{bagId} | CheckChangeBagUseCase | VendorBalanceAdjusted（差額時） |
| POST | /api/denominationchecks/cashbag/{bagId} | CheckCashBagUseCase | VendorBalanceAdjusted（差額時） |
| POST | /api/denominationchecks/safe/{safeId} | CheckSafeUseCase | VendorBalanceAdjusted（差額時） |

### 読み込み（Query）API — Read Model からのみ読む

| メソッド | エンドポイント | 読み込み元 |
|---------|-------------|----------|
| GET | /api/safes | safe_balances（Read Model） |
| GET | /api/pettycash-dashboard | petty_cash_ledger_view + safe_balances（Read Model） |
| GET | /api/vendor-dashboard | vendor_ledger_view + safe_balances（Read Model） |

### その他API（Read Modelに依存しない）

| メソッド | エンドポイント | 対象テーブル |
|---------|-------------|------------|
| POST | /api/safes | safes |
| GET | /api/bags | change_bags |
| GET | /api/cashbags | cash_bags |
| GET | /api/prepbags | prep_bags |
| POST | /api/prepbags | prep_bags |
| POST | /api/prepbags/{id}/cancel | prep_bags |
| GET | /api/denominationchecks/vendor | vendor_denomination_checks |
| GET | /api/denominationchecks/safe | safe_denomination_checks |
| PUT | /api/denominationchecks/vendor/{id} | vendor_denomination_checks |
| PUT | /api/denominationchecks/safe/{id} | safe_denomination_checks |

---

## 6. サービス一覧

| サービス | インターフェース | 実装 | 責務 |
|---------|----------------|------|------|
| 採番 | ISequenceNumberService | SequenceNumberService | 金庫単位の通し番号を採番 |
| 残高計算 | IBalanceService | BalanceService | ランニングバランスを計算してセット |
| イベント記録 | IEventStore | EventStore | domain_eventsにpayloadでイベントを記録 |
| プロジェクション | IProjectionService | ProjectionService | Read Modelテーブルを更新 |
| 業者出納帳Query | IVendorLedgerQueryService | VendorLedgerQueryService | vendor_ledger_viewから読み込み |
| 小口出納帳Query | IPettyCashLedgerQueryService | PettyCashLedgerQueryService | petty_cash_ledger_viewから読み込み |

---

## 7. 変更履歴

| コミット | 方式 | 残高の取得方法 |
|---------|------|--------------|
| 475b4a9以前 | CRUD | SUM(transactions) で毎回計算 |
| 475b4a9 | CRUD + ランニングバランス | 最新取引のbalance列を読む |
| Read Model導入 | CQRS | safe_balancesテーブルから読む |
| EventStore導入 | **ES+CQRS（完成）** | safe_balancesテーブルから読む + domain_eventsにpayload記録 |
