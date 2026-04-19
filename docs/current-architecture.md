# 現在のアーキテクチャ — API・処理フロー全体図

## 全体構成

```
フロントエンド (React)
    ↓ HTTP
コントローラー (API層)
    ↓
ユースケース (Application層)
    ↓
ドメインエンティティ (Domain層)  +  リポジトリIF (Domain層)
                                        ↓ 実装
                                  リポジトリ (Infrastructure層)
                                        ↓
                                  PostgreSQL (DB)
```

---

## 1. 小口現金の処理フロー

### 1-1. 入金登録

```
フロント: POST /api/petty-cash-transactions
    ↓
PettyCashTransactionsController.Create()
    ↓
CreatePettyCashTransactionUseCase.ExecuteAsync()
    ├── PettyCashTransaction.Create()          … ドメインエンティティ生成
    ├── sequenceNumberService.AssignAsync()     … 採番（金庫単位の通し番号）
    ├── balanceService.AssignBalanceAsync()     … ★ 最新取引のbalanceから新残高を計算
    ├── transactionRepository.AddAsync()        … DBに追加
    └── unitOfWork.SaveChangesAsync()           … コミット

DB: petty_cash_transactions に1行INSERT
    (id, sequence_number, safe_id, type=0, amount, balance, description, created_at)
```

### 1-2. 出納帳表示（ダッシュボード）

```
フロント: GET /api/pettycash-dashboard?safeId=2
    ↓
DashboardController.GetPettyCashDashboard()
    ↓
GetPettyCashDashboardUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync(2)
    │     └── LoadBalances()
    │           └── ★ SELECT balance FROM petty_cash_transactions
    │              WHERE safe_id=2 ORDER BY created_at DESC, id DESC LIMIT 1
    │           → safe.SetBalances(vendorBalance, pettyCashBalance)
    │
    ├── getTransactions.ExecuteAsync(2)
    │     └── ★ SELECT * FROM petty_cash_transactions WHERE safe_id=2
    │        → List<PettyCashTransactionDto> に変換
    │
    └── getDenomChecks.ExecuteAsync(2)
          └── SELECT * FROM safe_denomination_checks WHERE safe_id=2

戻り値: PettyCashDashboardDto { Safe, Transactions, DenominationChecks }
```

### 1-3. 有高チェック（金庫）

```
フロント: POST /api/denominationchecks/safe/{safeId}
    ↓
DenominationChecksController.CheckSafe()
    ↓
CheckSafeUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync()           … 現在残高を取得
    ├── PettyCashDenominationCheck.CreateForSafe() … 金種チェック記録を生成
    ├── sequenceNumberService.AssignAsync()
    ├── checkRepository.AddAsync()
    │
    ├── if (差額 != 0):
    │     ├── VendorTransaction.CreateSafeAdjustment() … 調整取引を生成
    │     ├── sequenceNumberService.AssignAsync()
    │     ├── balanceService.AssignBalanceAsync()       … ★ 残高再計算
    │     └── transactionRepository.AddAsync()
    │
    └── unitOfWork.SaveChangesAsync()
```

---

## 2. 業者管理の処理フロー

### 2-1. 釣り銭バッグ入金

```
フロント: POST /api/bags/deposit
    ↓
BagsController.Deposit()
    ↓
DepositBagUseCase.ExecuteAsync()
    ├── ChangeBag.CreateDeposit()               … バッグ＋入金取引を同時生成
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()     … ★ 残高計算
    ├── bagRepository.AddAsync()                … バッグと取引をまとめてINSERT
    └── unitOfWork.SaveChangesAsync()

DB: change_bags に1行INSERT + vendor_transactions に1行INSERT
```

### 2-2. 釣り銭バッグ → レジ移動（出金）

```
フロント: POST /api/bags/{id}/move
    ↓
BagsController.MoveToRegister()
    ↓
MoveBagToRegisterUseCase.ExecuteAsync()
    ├── bagRepository.GetByIdAsync()
    ├── bag.MoveToRegister()                    … ドメインロジック（状態変更＋出金取引生成）
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()     … ★ 残高計算
    ├── bagRepository.UpdateAsync()
    └── unitOfWork.SaveChangesAsync()

DB: change_bags を UPDATE（status変更）+ vendor_transactions に1行INSERT
```

### 2-3. キャッシュバッグ入金

```
フロント: POST /api/cashbags/deposit
    ↓
CashBagsController.Deposit()
    ↓
DepositCashBagUseCase.ExecuteAsync()
    ├── CashBag.CreateDeposit()
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()     … ★ 残高計算
    ├── cashBagRepository.AddAsync()
    └── unitOfWork.SaveChangesAsync()

DB: cash_bags に1行INSERT + vendor_transactions に1行INSERT
```

### 2-4. 準備バッグ引渡（出金）

```
フロント: POST /api/prepbags/{id}/handover
    ↓
PrepBagsController.HandOver()
    ↓
HandOverPrepBagUseCase.ExecuteAsync()
    ├── prepBagRepository.GetByIdAsync()
    ├── bag.MarkHandedOver()                    … ドメインロジック（状態変更＋出金取引生成）
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()     … ★ 残高計算
    ├── vendorTransactionRepository.AddAsync()
    └── unitOfWork.SaveChangesAsync()

DB: prep_bags を UPDATE + vendor_transactions に1行INSERT
```

### 2-5. 有高チェック（釣り銭バッグ / キャッシュバッグ）

```
フロント: POST /api/denominationchecks/changebag/{bagId}
         POST /api/denominationchecks/cashbag/{bagId}
    ↓
CheckChangeBagUseCase / CheckCashBagUseCase
    ├── bagRepository.GetByIdAsync()
    ├── VendorDenominationCheck.CreateFor...()  … 金種チェック記録
    ├── sequenceNumberService.AssignAsync()
    ├── checkRepository.AddAsync()
    │
    ├── if (差額 != 0):
    │     ├── VendorTransaction.CreateAdjustment() … 調整取引
    │     ├── sequenceNumberService.AssignAsync()
    │     ├── balanceService.AssignBalanceAsync()   … ★ 残高再計算
    │     ├── transactionRepository.AddAsync()
    │     └── bag.UpdateAmount()                    … バッグ金額を実数に修正
    │
    └── unitOfWork.SaveChangesAsync()
```

### 2-6. 業者ダッシュボード表示

```
フロント: GET /api/vendor-dashboard?safeId=2
    ↓
DashboardController.GetVendorDashboard()
    ↓
GetVendorDashboardUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync()
    │     └── LoadBalances()
    │           └── ★ SELECT balance FROM vendor_transactions  (最新1件)
    │           └── ★ SELECT balance FROM petty_cash_transactions (最新1件)
    │
    ├── getBags.ExecuteAsync()        → change_bags
    ├── getCashBags.ExecuteAsync()    → cash_bags
    ├── getTransactions.ExecuteAsync() → ★ vendor_transactions（全件）
    ├── getDenomChecks.ExecuteAsync()  → vendor_denomination_checks
    └── getPrepBags.ExecuteAsync()    → prep_bags

戻り値: VendorDashboardDto { Safe, Bags, CashBags, Transactions, DenominationChecks, PrepBags }
```

---

## 3. 金庫一覧（ヘッダー表示）

```
フロント: GET /api/safes
    ↓
SafesController.GetAll()
    ↓
GetSafesUseCase.ExecuteAsync()
    └── safeRepository.GetAllAsync()
          └── 各金庫ごとに LoadBalances()
                └── ★ SELECT balance FROM vendor_transactions (最新1件)
                └── ★ SELECT balance FROM petty_cash_transactions (最新1件)

戻り値: List<SafeDto> { Id, Name, CurrentBalance, VendorBalance, PettyCashBalance, ... }
```

---

## 4. 全API一覧

| メソッド | エンドポイント | コントローラー | ユースケース | 書き込み先 |
|---------|-------------|--------------|------------|----------|
| GET | /api/safes | SafesController | GetSafesUseCase | - |
| POST | /api/safes | SafesController | CreateSafeUseCase | safes |
| GET | /api/vendor-dashboard | DashboardController | GetVendorDashboardUseCase | - |
| GET | /api/pettycash-dashboard | DashboardController | GetPettyCashDashboardUseCase | - |
| GET | /api/bags | BagsController | GetBagsUseCase | - |
| POST | /api/bags/deposit | BagsController | DepositBagUseCase | change_bags + vendor_transactions |
| POST | /api/bags/{id}/move | BagsController | MoveBagToRegisterUseCase | change_bags + vendor_transactions |
| GET | /api/cashbags | CashBagsController | GetCashBagsUseCase | - |
| POST | /api/cashbags/deposit | CashBagsController | DepositCashBagUseCase | cash_bags + vendor_transactions |
| GET | /api/prepbags | PrepBagsController | GetPrepBagsUseCase | - |
| POST | /api/prepbags | PrepBagsController | CreatePrepBagUseCase | prep_bags |
| POST | /api/prepbags/{id}/handover | PrepBagsController | HandOverPrepBagUseCase | prep_bags + vendor_transactions |
| POST | /api/prepbags/{id}/cancel | PrepBagsController | CancelPrepBagUseCase | prep_bags |
| GET | /api/vendor-transactions | VendorTransactionsController | GetVendorTransactionsUseCase | - |
| GET | /api/petty-cash-transactions | PettyCashTransactionsController | GetPettyCashTransactionsUseCase | - |
| POST | /api/petty-cash-transactions | PettyCashTransactionsController | CreatePettyCashTransactionUseCase | petty_cash_transactions |
| GET | /api/denominationchecks/vendor | DenominationChecksController | GetVendorDenominationChecksUseCase | - |
| GET | /api/denominationchecks/safe | DenominationChecksController | GetPettyCashDenominationChecksUseCase | - |
| POST | /api/denominationchecks/safe/{safeId} | DenominationChecksController | CheckSafeUseCase | safe_denomination_checks + vendor_transactions |
| POST | /api/denominationchecks/changebag/{bagId} | DenominationChecksController | CheckChangeBagUseCase | vendor_denomination_checks + vendor_transactions |
| POST | /api/denominationchecks/cashbag/{bagId} | DenominationChecksController | CheckCashBagUseCase | vendor_denomination_checks + vendor_transactions |
| POST | /api/denominationchecks/prepbag/{bagId} | DenominationChecksController | CheckPrepBagUseCase | vendor_denomination_checks |
| PUT | /api/denominationchecks/vendor/{id} | DenominationChecksController | UpdateVendorDenominationCheckUseCase | vendor_denomination_checks |
| PUT | /api/denominationchecks/safe/{id} | DenominationChecksController | UpdatePettyCashDenominationCheckUseCase | safe_denomination_checks |

---

## 5. 現在のDB構成と役割

| テーブル | 役割 | 読み書き |
|---------|------|---------|
| safes | 金庫マスタ（残高はbalance列から都度算出） | 読み書き |
| change_bags | 釣り銭バッグ | 読み書き |
| cash_bags | キャッシュバッグ | 読み書き |
| prep_bags | 準備バッグ | 読み書き |
| **vendor_transactions** | **業者取引（イベントストア相当）** | **読み書き ← 問題** |
| **petty_cash_transactions** | **小口取引（イベントストア相当）** | **読み書き ← 問題** |
| vendor_denomination_checks | 業者有高チェック記録 | 読み書き |
| safe_denomination_checks | 金庫有高チェック記録 | 読み書き |

---

## 6. ★ 現在の問題点 — Read Model が無い

全ての読み込みがイベントストア（トランザクションテーブル）を直接クエリしている。

```
現在:
  vendor_transactions       ←── 書き込み（イベント記録）
  vendor_transactions       ←── 読み込み（出納帳表示・残高取得）★同じテーブル

  petty_cash_transactions   ←── 書き込み（イベント記録）
  petty_cash_transactions   ←── 読み込み（出納帳表示・残高取得）★同じテーブル

本来のCQRS:
  vendor_transactions       ←── 書き込み（イベント記録）
       ↓ プロジェクション
  vendor_ledger_view（Read Model） ←── 読み込み（出納帳表示）
  safe_balances（Read Model）      ←── 読み込み（残高取得）
```

### Read Model を導入すると

- **書き込み**: 今まで通りトランザクションテーブルにINSERT
- **プロジェクション**: INSERT後にRead Modelテーブルを更新
- **読み込み**: Read Modelテーブルからのみ読む（イベントストアは読まない）
- **メリット**: 読み込みが高速、書き込みと読み込みの構造を独立して最適化できる
