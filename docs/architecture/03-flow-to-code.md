# 業務フロー → コード対応ガイド

「この画面ボタンを押すと、コードのどこが動くのか」を業務の流れに沿って整理する。
業務そのものの流れ（コードなし）は [spec/01-business-flow.md](../spec/01-business-flow.md)、API の一覧は [api-screens/api-endpoints.md](../api-screens/api-endpoints.md) を参照。

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
    ├── balanceService.AssignBalanceAsync() … 新しい業者残高を計算して取引行にセット
    ├── bagRepository.AddAsync()           … DB保存
    └── unitOfWork.SaveChangesAsync()      … コミット

【DB変化】
  change_bags        : 1行INSERT（status = InSafe）
  vendor_transactions: 1行INSERT（type = Deposit）
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
    ├── balanceService.AssignBalanceAsync() … 出金後の業者残高を計算して取引行にセット
    ├── bagRepository.UpdateAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  change_bags        : status を InSafe → MovedToRegister に UPDATE
  vendor_transactions: 1行INSERT（type = Withdrawal）
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
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  cash_bags          : 1行INSERT（status = MovedToSafe）
  vendor_transactions: 1行INSERT（type = Deposit）
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
    ├── cashBagRepository.GetByIdAsync()   … 選択された売上バッグを取得
    ├── PrepBag.Create()                   … 準備バッグを生成し、売上バッグを紐づける（Domain層）
    ├── prepBagRepository.AddAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  prep_bags : 1行INSERT（status = Preparing）
  cash_bags : 対象の prep_bag_id に準備バッグを設定
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
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  prep_bags          : status を Preparing → HandedOver に UPDATE
  vendor_transactions: 1行INSERT（type = Withdrawal）
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
    │     └── bag.UpdateAmount()               … バッグ金額を実数に修正
    │
    └── unitOfWork.SaveChangesAsync()

【DB変化（差額なし）】
  vendor_denomination_checks: 1行INSERT

【DB変化（差額あり）】
  vendor_denomination_checks: 1行INSERT
  vendor_transactions       : 1行INSERT（type = Adjustment）
  change_bags / cash_bags   : totalAmount を UPDATE
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
    ├── balanceService.AssignBalanceAsync() … 新しい小口残高を計算して取引行にセット
    ├── transactionRepository.AddAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  petty_cash_transactions  : 1行INSERT
```

---

### シナリオ8: 金庫の有高チェック（小口現金）

```
【業務】金庫内の現金を実際に数えて、小口帳簿と照合する

【画面】小口タブ → 「有高チェック」ボタン → 金種を入力 → 登録

【API】POST /api/denominationchecks/safe/{safeId}

【コード】
CheckSafeUseCase.ExecuteAsync()
    ├── safeRepository.GetByIdAsync()      … 小口の帳簿残高を取得（業者残高は含めない）
    ├── PettyCashDenominationCheck.CreateForSafe() … チェック記録を生成
    ├── sequenceNumberService.AssignAsync()
    ├── checkRepository.AddAsync()
    │
    ├── if (差額 != 0):
    │     ├── PettyCashTransaction.CreateAdjustment() … 小口の調整取引を生成
    │     ├── sequenceNumberService.AssignAsync()
    │     ├── balanceService.AssignBalanceAsync()
    │     └── transactionRepository.AddAsync()
    │
    └── unitOfWork.SaveChangesAsync()

【DB変化（差額なし）】
  safe_denomination_checks: 1行INSERT

【DB変化（差額あり）】
  safe_denomination_checks : 1行INSERT
  petty_cash_transactions  : 1行INSERT（type = Adjustment）
```

---

## 画面表示（データ読み込み）の流れ

画面を開いたとき・金庫を切り替えたときに起きること。
残高は取引テーブルの最新行の `balance` 列を、出納帳は取引テーブルそのものを読む。

```
【小口タブを開く】
GET /api/pettycash-dashboard?safeId=X
    ↓
GetPettyCashDashboardUseCase
    ├── petty_cash_transactions / vendor_transactions の最新行 → ヘッダーの残高表示
    ├── petty_cash_transactions  → 出納帳テーブル
    └── safe_denomination_checks → 有高チェック履歴

【業者タブを開く】
GET /api/vendor-dashboard?safeId=X
    ↓
GetVendorDashboardUseCase
    ├── petty_cash_transactions / vendor_transactions の最新行 → ヘッダーの残高表示
    ├── vendor_transactions      → 出納帳テーブル
    ├── change_bags              → 両替金バッグ一覧
    ├── cash_bags                → 売上バッグ一覧
    ├── prep_bags                → 準備バッグ一覧
    └── vendor_denomination_checks → 有高チェック履歴

【金庫セレクトボックスの初期表示】
GET /api/safes
    ↓
GetSafesUseCase
    └── safes ＋ 各取引テーブルの最新行 → 各金庫の残高（業者・小口）
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
| 採番・残高計算 | `backend/PettyCash.Infrastructure/Services/` |
| 出納帳の読み込み（QueryService） | `backend/PettyCash.Infrastructure/Queries/` |

イベントソーシング（ES+CQRS）を入れた場合にこの流れがどう変わるかは [event-sourcing/changes-from-current.md](../event-sourcing/changes-from-current.md) を参照。
