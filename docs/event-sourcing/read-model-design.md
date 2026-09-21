# Read Model 設計書

## 1. 現状の問題

イベントストア（トランザクションテーブル）を書き込みにも読み込みにも使っている。
CQRSの「C（Command）」と「Q（Query）」が分離されていない。

```
現在:
  フロント ──GET──→ トランザクションテーブル（イベントストア）
  フロント ──POST──→ トランザクションテーブル（イベントストア）
  同じテーブルに読みも書きも行っている
```

## 2. 目指す姿

```
書き込み（Command側）:
  フロント ──POST──→ UseCase ──→ トランザクションテーブル（イベントストア）
                                        ↓ プロジェクション
読み込み（Query側）:                      ↓
  フロント ──GET───→ QueryService ──→ Read Model テーブル
```

- イベントストア: 書き込み専用（追記のみ）
- Read Model: 読み込み専用（プロジェクションで更新）
- フロントのGETは**一切イベントストアを読まない**

## 3. 必要な Read Model テーブル

### 3-1. safe_balances（金庫残高）

現在SafeRepositoryがトランザクションテーブルの最新balanceを読んでいる部分を置き換える。

```sql
CREATE TABLE safe_balances (
    safe_id INTEGER PRIMARY KEY REFERENCES safes(id),
    vendor_balance INTEGER NOT NULL DEFAULT 0,
    petty_cash_balance INTEGER NOT NULL DEFAULT 0,
    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

**用途:** ヘッダーの残高表示（GET /api/safes）、出金時の残高チェック

**プロジェクション:**
- vendor_transactionsにINSERT → safe_balances.vendor_balanceを更新
- petty_cash_transactionsにINSERT → safe_balances.petty_cash_balanceを更新

### 3-2. vendor_ledger_view（業者出納帳ビュー）

現在GetVendorTransactionsUseCaseがvendor_transactionsを直接読んでいる部分を置き換える。

```sql
CREATE TABLE vendor_ledger_view (
    id SERIAL PRIMARY KEY,
    sequence_number INTEGER NOT NULL,
    safe_id INTEGER NOT NULL,
    change_bag_id INTEGER,
    cash_bag_id INTEGER,
    prep_bag_id INTEGER,
    type INTEGER NOT NULL,
    amount INTEGER NOT NULL,
    balance INTEGER NOT NULL,
    description VARCHAR(200) NOT NULL DEFAULT '',
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

**用途:** 業者出納帳の表示（GET /api/vendor-dashboard）

**プロジェクション:**
- vendor_transactionsにINSERT → vendor_ledger_viewにも同じ内容をINSERT

### 3-3. petty_cash_ledger_view（小口出納帳ビュー）

```sql
CREATE TABLE petty_cash_ledger_view (
    id SERIAL PRIMARY KEY,
    sequence_number INTEGER NOT NULL,
    safe_id INTEGER NOT NULL,
    type INTEGER NOT NULL,
    amount INTEGER NOT NULL,
    balance INTEGER NOT NULL,
    description VARCHAR(200) NOT NULL DEFAULT '',
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

**用途:** 小口出納帳の表示（GET /api/pettycash-dashboard）

**プロジェクション:**
- petty_cash_transactionsにINSERT → petty_cash_ledger_viewにも同じ内容をINSERT

## 4. プロジェクションの実装方法

取引がINSERTされた直後に、同じトランザクション内でRead Modelを更新する（同期プロジェクション）。

```
UseCase内の処理順序:

1. トランザクション生成（ドメイン）
2. 採番（SequenceNumberService）
3. 残高計算（BalanceService）
4. イベントストアにINSERT（transactionRepository.AddAsync）
5. Read ModelをUPDATE/INSERT（★ ProjectionService ← 新設）
6. コミット（unitOfWork.SaveChangesAsync）
```

### ProjectionService（新設）

```csharp
public interface IProjectionService
{
    Task ProjectAsync(VendorTransaction transaction);
    Task ProjectAsync(PettyCashTransaction transaction);
}
```

```csharp
public class ProjectionService : IProjectionService
{
    // vendor_transactionsのINSERT後に呼ばれる
    public async Task ProjectAsync(VendorTransaction transaction)
    {
        // 1. vendor_ledger_view にINSERT（出納帳表示用）
        // 2. safe_balances の vendor_balance を更新（残高表示用）
    }

    // petty_cash_transactionsのINSERT後に呼ばれる
    public async Task ProjectAsync(PettyCashTransaction transaction)
    {
        // 1. petty_cash_ledger_view にINSERT（出納帳表示用）
        // 2. safe_balances の petty_cash_balance を更新（残高表示用）
    }
}
```

## 5. 読み込み側の変更

### 変更前（イベントストアを直接読む）

```
SafeRepository.LoadBalances()
  → SELECT balance FROM vendor_transactions ... LIMIT 1
  → SELECT balance FROM petty_cash_transactions ... LIMIT 1

GetVendorTransactionsUseCase
  → SELECT * FROM vendor_transactions WHERE safe_id = X

GetPettyCashTransactionsUseCase
  → SELECT * FROM petty_cash_transactions WHERE safe_id = X
```

### 変更後（Read Modelから読む）

```
SafeRepository.LoadBalances()
  → SELECT vendor_balance, petty_cash_balance FROM safe_balances WHERE safe_id = X

GetVendorLedgerQueryService（新設）
  → SELECT * FROM vendor_ledger_view WHERE safe_id = X

GetPettyCashLedgerQueryService（新設）
  → SELECT * FROM petty_cash_ledger_view WHERE safe_id = X
```

## 6. 処理フロー（Read Model導入後）

### 小口入金の場合

```
フロント: POST /api/petty-cash-transactions
    ↓
CreatePettyCashTransactionUseCase.ExecuteAsync()
    ├── PettyCashTransaction.Create()
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── transactionRepository.AddAsync()         … イベントストアにINSERT
    ├── projectionService.ProjectAsync()          … ★ Read Modelを更新
    │     ├── petty_cash_ledger_view にINSERT
    │     └── safe_balances を UPDATE
    └── unitOfWork.SaveChangesAsync()
```

### 小口出納帳表示の場合

```
フロント: GET /api/pettycash-dashboard?safeId=2
    ↓
GetPettyCashDashboardUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync(2)
    │     └── SELECT * FROM safe_balances WHERE safe_id = 2  ★ Read Model
    │
    ├── pettyCashLedgerQuery.GetBySafeIdAsync(2)
    │     └── SELECT * FROM petty_cash_ledger_view WHERE safe_id = 2  ★ Read Model
    │
    └── getDenomChecks.ExecuteAsync(2)

※ イベントストア（petty_cash_transactions）は一切読まない
```

## 7. DB構成（Read Model導入後）

| テーブル | 役割 | 読み/書き |
|---------|------|----------|
| vendor_transactions | イベントストア（業者） | **書き込みのみ** |
| petty_cash_transactions | イベントストア（小口） | **書き込みのみ** |
| safe_balances | Read Model（残高） | **読み込みのみ** |
| vendor_ledger_view | Read Model（業者出納帳） | **読み込みのみ** |
| petty_cash_ledger_view | Read Model（小口出納帳） | **読み込みのみ** |
| safes | 金庫マスタ | 読み書き |
| change_bags | 両替金バッグ | 読み書き |
| cash_bags | 売上バッグ | 読み書き |
| prep_bags | 準備バッグ | 読み書き |
| vendor_denomination_checks | 有高チェック記録 | 読み書き |
| safe_denomination_checks | 有高チェック記録 | 読み書き |

## 8. メリットと注意点

### メリット

- **CQRSの原則に準拠**: 書き込みと読み込みが完全に分離
- **イベントストアが汚れない**: 読み込みの都合でイベントストアの構造を変える必要がない
- **Read Modelを自由に最適化**: 画面表示に最適な構造にできる（結合済みのビュー等）
- **リビルド可能**: Read Modelが壊れてもイベントストアから再構築できる

### 注意点

- **同期プロジェクション**: 書き込みとRead Model更新を同じトランザクションで行うため、整合性は保証されるが書き込みが少し遅くなる
- **テーブル増加**: Read Model分のテーブルが増える（3テーブル追加）
- **この規模では過剰**: 小口現金の取引量ではRead Modelのメリットは性能面ではほぼない。アーキテクチャの正しさ・会社の要件対応が主な目的
