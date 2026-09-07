# 業務フロー → コード対応ガイド

「この画面ボタンを押すと、コードのどこが動くのか」を業務の流れに沿って整理する。

---

## 登場する概念の一覧

| 業務用語 | システム上の名前 | 説明 |
|---------|---------------|------|
| 金庫 | Safe | 店舗ごとの現金管理単位 |
| 両替金バッグ | ChangeBag (CA-XXX) | レジに渡す両替金の束 |
| 売上バッグ | CashBag (BAG-XXX) | レジから回収した現金の束 |
| 準備バッグ | PrepBag | 複数の売上バッグをまとめて業者に渡す袋 |
| 有高チェック | DenominationCheck | 実際の現金を数えて帳簿残高と照合する作業 |
| 業者残高 | vendorBalance | 両替金・売上バッグの収支合計 |
| 小口残高 | pettyCashBalance | 小口現金の収支合計 |

---

## 業者管理の流れ

### シナリオ1: 両替金バッグを作って金庫に入れる

```
【業務】レジに持っていく両替金を金庫に入金する

【画面】業者タブ → 「両替金追加」ボタン → 金額・備考を入力 → 登録

【API】POST /api/bags/deposit

【コード】
BagsController.Deposit()
    ↓
DepositBagUseCase.ExecuteAsync()
    ├── ChangeBag.CreateDeposit()          … バッグと入金取引を同時生成（Domain層）
    ├── sequenceNumberService.AssignAsync() … 通し番号を採番
    ├── balanceService.AssignBalanceAsync() … 業者残高を更新
    ├── bagRepository.AddAsync()           … DB保存
    ├── eventStore.AppendAsync()           … イベント記録（VendorMoneyDeposited）
    ├── projectionService.ProjectAsync()   … 残高Read Modelを更新
    └── unitOfWork.SaveChangesAsync()      … コミット

【DB変化】
  change_bags        : 1行INSERT（status = InSafe）
  vendor_transactions: 1行INSERT（type = Deposit）
  vendor_ledger_view : 1行INSERT（Read Model）
  safe_balances      : vendor_balance を UPDATE（Read Model）
  domain_events      : 1行INSERT（event_type = VendorMoneyDeposited）
```

---

### シナリオ2: 両替金バッグをレジに移動する

```
【業務】金庫内の両替金バッグをレジへ持っていく

【画面】業者タブ → 両替金バッグ一覧の「移動」ボタン

【API】POST /api/bags/{id}/move

【コード】
BagsController.MoveToRegister()
    ↓
MoveBagToRegisterUseCase.ExecuteAsync()
    ├── bagRepository.GetByIdAsync()       … バッグを取得
    ├── bag.MoveToRegister()               … 状態変更＋出金取引生成（Domain層）
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync() … 業者残高を減らす
    ├── bagRepository.UpdateAsync()
    ├── eventStore.AppendAsync()           … イベント記録（VendorMoneyWithdrawn）
    ├── projectionService.ProjectAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  change_bags        : status を InSafe → MovedToRegister に UPDATE
  vendor_transactions: 1行INSERT（type = Withdrawal）
  vendor_ledger_view : 1行INSERT（Read Model）
  safe_balances      : vendor_balance を UPDATE（Read Model）
  domain_events      : 1行INSERT（event_type = VendorMoneyWithdrawn）
```

---

### シナリオ3: レジの現金を回収して売上バッグに入れる

```
【業務】レジの現金を売上バッグに入れて金庫へ

【画面】業者タブ → 「売上バッグ追加」ボタン → 金額・備考を入力 → 登録

【API】POST /api/cashbags/deposit

【コード】
CashBagsController.Deposit()
    ↓
DepositCashBagUseCase.ExecuteAsync()
    ├── CashBag.CreateDeposit()
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── cashBagRepository.AddAsync()
    ├── eventStore.AppendAsync()           … イベント記録（VendorMoneyDeposited）
    ├── projectionService.ProjectAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  cash_bags          : 1行INSERT（status = AtRegister）
  vendor_transactions: 1行INSERT（type = Deposit）
  vendor_ledger_view : 1行INSERT（Read Model）
  safe_balances      : vendor_balance を UPDATE（Read Model）
  domain_events      : 1行INSERT（event_type = VendorMoneyDeposited）
```

---

### シナリオ4: 売上バッグを準備バッグにまとめる

```
【業務】複数の売上バッグを1つにまとめて業者引き渡し準備をする

【画面】業者タブ → 売上バッグ一覧でチェック → 「CashBag→準備Bag」ボタン

【API】POST /api/prepbags

【コード】
PrepBagsController.Create()
    ↓
CreatePrepBagUseCase.ExecuteAsync()
    ├── PrepBag.Create()                   … 準備バッグを生成（Domain層）
    ├── 各CashBagのstatusを MovedToSafe に変更
    ├── prepBagRepository.AddAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  prep_bags : 1行INSERT（status = Preparing）
  cash_bags : 対象の status を MovedToSafe に UPDATE
  ※ この時点では残高変動なし
```

---

### シナリオ5: 準備バッグを業者に引き渡す（出金）

```
【業務】準備バッグを銀行・業者に渡して業者残高から差し引く

【画面】業者タブ → 準備バッグ一覧の「引渡」ボタン

【API】POST /api/prepbags/{id}/handover

【コード】
PrepBagsController.HandOver()
    ↓
HandOverPrepBagUseCase.ExecuteAsync()
    ├── prepBagRepository.GetByIdAsync()
    ├── bag.MarkHandedOver()               … 状態変更＋出金取引生成（Domain層）
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── vendorTransactionRepository.AddAsync()
    ├── eventStore.AppendAsync()           … イベント記録（VendorMoneyWithdrawn）
    ├── projectionService.ProjectAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  prep_bags          : status を Preparing → HandedOver に UPDATE
  vendor_transactions: 1行INSERT（type = Withdrawal）
  vendor_ledger_view : 1行INSERT（Read Model）
  safe_balances      : vendor_balance を UPDATE（Read Model）
  domain_events      : 1行INSERT（event_type = VendorMoneyWithdrawn）
```

---

### シナリオ6: 有高チェック（両替金バッグ / 売上バッグ）

```
【業務】バッグの中身を実際に数えて、登録金額と一致するか確認する
        差額があれば帳簿を自動調整する

【画面】業者タブ → バッグ行の「有高」ボタン → 金種を入力 → 登録

【API】POST /api/denominationchecks/changebag/{bagId}   （両替金バッグ）
       POST /api/denominationchecks/cashbag/{bagId}    （売上バッグ）

【コード】
CheckChangeBagUseCase / CheckCashBagUseCase
    ├── bagRepository.GetByIdAsync()
    ├── VendorDenominationCheck.CreateFor...() … 有高チェック記録を生成
    ├── sequenceNumberService.AssignAsync()
    ├── checkRepository.AddAsync()
    │
    ├── if (差額 != 0):                        … 実数 ≠ 帳簿のとき
    │     ├── VendorTransaction.CreateAdjustment()  … 調整取引を生成
    │     ├── sequenceNumberService.AssignAsync()
    │     ├── balanceService.AssignBalanceAsync()
    │     ├── transactionRepository.AddAsync()
    │     ├── eventStore.AppendAsync()         … VendorBalanceAdjusted
    │     ├── projectionService.ProjectAsync()
    │     └── bag.UpdateAmount()               … バッグ金額を実数に修正
    │
    └── unitOfWork.SaveChangesAsync()

【DB変化（差額なし）】
  vendor_denomination_checks: 1行INSERT

【DB変化（差額あり）】
  vendor_denomination_checks: 1行INSERT
  vendor_transactions       : 1行INSERT（type = Adjustment）
  change_bags / cash_bags   : totalAmount を UPDATE
  vendor_ledger_view        : 1行INSERT（Read Model）
  safe_balances             : vendor_balance を UPDATE（Read Model）
  domain_events             : 1行INSERT（event_type = VendorBalanceAdjusted）
```

---

## 小口現金管理の流れ

### シナリオ7: 小口現金の入出金を登録する

```
【業務】事務用品購入・交通費精算などの小口現金の入出金を記録する

【画面】小口タブ → 「入出金登録」ボタン → 種別・金額・摘要を入力 → 登録

【API】POST /api/petty-cash-transactions

【コード】
PettyCashTransactionsController.Create()
    ↓
CreatePettyCashTransactionUseCase.ExecuteAsync()
    ├── PettyCashTransaction.Create()      … 取引エンティティを生成（Domain層）
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync() … 小口残高を更新
    ├── transactionRepository.AddAsync()
    ├── eventStore.AppendAsync()           … PettyCashDeposited / PettyCashWithdrawn
    ├── projectionService.ProjectAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  petty_cash_transactions  : 1行INSERT
  petty_cash_ledger_view   : 1行INSERT（Read Model）
  safe_balances            : petty_cash_balance を UPDATE（Read Model）
  domain_events            : 1行INSERT
```

---

### シナリオ8: 金庫の有高チェック（小口現金）

```
【業務】金庫内の現金を実際に数えて、小口帳簿と照合する

【画面】小口タブ → 「有高チェック」ボタン → 金種を入力 → 登録

【API】POST /api/denominationchecks/safe/{safeId}

【コード】
CheckSafeUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync()      … 現在の帳簿残高を取得
    ├── PettyCashDenominationCheck.CreateForSafe() … チェック記録を生成
    ├── sequenceNumberService.AssignAsync()
    ├── checkRepository.AddAsync()
    │
    ├── if (差額 != 0):
    │     ├── PettyCashTransaction.CreateAdjustment() … 調整取引を生成
    │     ├── sequenceNumberService.AssignAsync()
    │     ├── balanceService.AssignBalanceAsync()
    │     ├── transactionRepository.AddAsync()
    │     ├── eventStore.AppendAsync()     … PettyCashBalanceAdjusted
    │     └── projectionService.ProjectAsync()
    │
    └── unitOfWork.SaveChangesAsync()

【DB変化（差額なし）】
  safe_denomination_checks: 1行INSERT

【DB変化（差額あり）】
  safe_denomination_checks : 1行INSERT
  petty_cash_transactions  : 1行INSERT（type = Adjustment）
  petty_cash_ledger_view   : 1行INSERT（Read Model）
  safe_balances            : petty_cash_balance を UPDATE（Read Model）
  domain_events            : 1行INSERT（event_type = PettyCashBalanceAdjusted）
```

---

## 画面表示（データ読み込み）の流れ

画面を開いたとき・金庫を切り替えたときに起きること。
**すべて Read Model テーブルからのみ読む（トランザクションテーブルは読まない）。**

```
【小口タブを開く】
GET /api/pettycash-dashboard?safeId=X
    ↓
GetPettyCashDashboardUseCase
    ├── safe_balances            → ヘッダーの残高表示
    ├── petty_cash_ledger_view   → 出納帳テーブル
    └── safe_denomination_checks → 有高チェック履歴

【業者タブを開く】
GET /api/vendor-dashboard?safeId=X
    ↓
GetVendorDashboardUseCase
    ├── safe_balances            → ヘッダーの残高表示
    ├── vendor_ledger_view       → 出納帳テーブル
    ├── change_bags              → 両替金バッグ一覧
    ├── cash_bags                → 売上バッグ一覧
    ├── prep_bags                → 準備バッグ一覧
    └── vendor_denomination_checks → 有高チェック履歴

【金庫セレクトボックスの初期表示】
GET /api/safes
    ↓
GetSafesUseCase
    └── safe_balances            → 各金庫の残高（業者・小口）
```

---

## コードの場所まとめ

| 役割 | 場所 |
|-----|------|
| 画面コンポーネント | `frontend/src/components/` |
| APIクライアント | `frontend/src/api/client.ts` |
| コントローラー（HTTPエンドポイント） | `backend/PettyCash.Api/Controllers/` |
| ユースケース（業務ロジックの入口） | `backend/PettyCash.Application/UseCases/` |
| ドメインエンティティ（ルール） | `backend/PettyCash.Domain/` |
| リポジトリ実装（DB操作） | `backend/PettyCash.Infrastructure/Repositories/` |
| Read Model更新（ProjectionService） | `backend/PettyCash.Infrastructure/Services/ProjectionService.cs` |
| イベント記録（EventStore） | `backend/PettyCash.Infrastructure/Services/EventStore.cs` |
