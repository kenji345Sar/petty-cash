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
| 売上金残高 | vendorBalance | 両替金・売上バッグの収支合計 |
| 小口残高 | pettyCashBalance | 小口現金の収支合計 |

---

## UseCase 一覧（この資料のどこで説明しているか）

書き込み系はすべてシナリオとして載せている。読み込み系は「画面表示の流れ」でまとめて扱う。

| UseCase | 操作 | 説明している場所 |
|---|---|---|
| DepositBagUseCase | 両替金バッグの入金 | シナリオ1 |
| MoveBagToRegisterUseCase | 両替金バッグをレジへ | シナリオ2 |
| DepositCashBagUseCase | 売上バッグの入金 | シナリオ3 |
| CreatePrepBagUseCase | 準備バッグにまとめる | シナリオ4 |
| HandOverPrepBagUseCase | 準備バッグの引渡 | シナリオ5 |
| CheckChangeBagUseCase / CheckCashBagUseCase | バッグの有高チェック | シナリオ6 |
| CreatePettyCashTransactionUseCase | 小口の入出金登録 | シナリオ7 |
| CheckSafeUseCase | 小口の有高チェック | シナリオ8 |
| ReversePettyCashTransactionUseCase | 小口の赤伝 | シナリオ9 |
| ReverseVendorTransactionUseCase | 売上金の赤伝 | シナリオ10 |
| UpdatePettyCashDenominationCheckUseCase / UpdateVendorDenominationCheckUseCase | 有高チェックの修正 | シナリオ11 |
| CancelPrepBagUseCase / CheckPrepBagUseCase / CreateSafeUseCase | 準備バッグの取消・準備バッグの有高チェック・金庫の作成 | シナリオ12 |
| GetPettyCashDashboardUseCase / GetVendorDashboardUseCase / GetSafesUseCase | タブを開く・金庫を切り替える | 画面表示の流れ |
| GetBagsUseCase / GetCashBagsUseCase / GetPrepBagsUseCase / GetPettyCashTransactionsUseCase / GetVendorTransactionsUseCase / GetVendorDenominationChecksUseCase / GetPettyCashDenominationChecksUseCase | 一覧の取得（ダッシュボードの中から呼ばれる） | 画面表示の流れ |

実体は [backend/PettyCash.Application/UseCases/](../../backend/PettyCash.Application/UseCases/) にある。使われているものの一覧は `Program.cs` の DI 登録でも確認できる。
`GetDenominationChecksUseCase.cs` と `UpdateDenominationCheckUseCase.cs` は、1ファイルに小口用・売上金用の2クラスが入っている。

---

## 売上金管理の流れ

### シナリオ1: 両替金バッグを作って金庫に入れる

```
【業務】レジに持っていく両替金を金庫に入金する

【画面】売上金タブ → 「両替金追加」ボタン → 金額・備考を入力 → 登録

【API】POST /api/bags/deposit

【コード】
BagsController.Deposit()
    ↓
DepositBagUseCase.ExecuteAsync()
    ├── ChangeBag.CreateDeposit()          … バッグと入金取引を同時生成（Domain層）
    ├── sequenceNumberService.AssignAsync() … 通し番号を採番
    ├── balanceService.AssignBalanceAsync() … 新しい売上金残高を計算して取引行にセット
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

【画面】売上金タブ → 両替金バッグ一覧の「移動」ボタン

【API】POST /api/bags/{id}/move

【コード】
BagsController.MoveToRegister()
    ↓
MoveBagToRegisterUseCase.ExecuteAsync()
    ├── bagRepository.GetByIdAsync()       … バッグを取得
    ├── bag.MoveToRegister()               … 状態変更＋出金取引生成（Domain層）
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync() … 出金後の売上金残高を計算して取引行にセット
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

【画面】売上金タブ → 「売上バッグ追加」ボタン → 金額・備考を入力 → 登録

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

【画面】売上金タブ → 売上バッグ一覧でチェック → 「CashBag→準備Bag」ボタン

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
【業務】準備バッグを銀行・業者に渡して売上金残高から差し引く

【画面】売上金タブ → 準備バッグ一覧の「引渡」ボタン

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

【画面】売上金タブ → バッグ行の「有高」ボタン → 金種を入力 → 登録

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
    ├── safeRepository.GetByIdAsync()      … 小口の帳簿残高を取得（売上金残高は含めない）
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

## 小口・売上金に共通する操作

### シナリオ9: 小口の取引を赤伝で打ち消す

```
【業務】登録済みの入出金を、記録を消さずに取り消す

【画面】小口タブ → 出納帳の行の「赤伝」ボタン → 摘要を確認 → 赤伝作成

【API】POST /api/petty-cash-transactions/{id}/reverse

【コード】
PettyCashTransactionsController.Reverse()
    ↓
ReversePettyCashTransactionUseCase.ExecuteAsync()
    ├── transactionRepository.GetByIdAsync()   … 元の取引を読む
    ├── PettyCashTransaction.CreateReversal()  … 逆向きの取引を作る（Domain層）
    ├── if (出金になる場合):
    │     ├── safeRepository.GetByIdAsync()
    │     └── safe.EnsureCanWithdrawPettyCash() … 小口残高で足りるか（Domain層）
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── transactionRepository.AddAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  petty_cash_transactions : 1行INSERT（元の取引は書き換えない）
```

入金の赤伝は出金になるため、小口残高が足りないと失敗する。出金の赤伝は入金になるので残高チェックはしない。

---

### シナリオ10: 売上金の取引を赤伝で打ち消す

```
【業務】バッグ操作で生まれた取引を、記録を消さずに取り消す

【画面】売上金タブ → 出納帳の行の「赤伝」ボタン

【API】POST /api/vendor-transactions/{id}/reverse

【コード】
VendorTransactionsController.Reverse()
    ↓
ReverseVendorTransactionUseCase.ExecuteAsync()
    ├── transactionRepository.GetByIdAsync()
    ├── VendorTransaction.CreateReversal()
    ├── if (出金になる場合): safe.EnsureCanWithdrawVendor() … 売上金残高で足りるか
    ├── sequenceNumberService.AssignAsync()
    ├── balanceService.AssignBalanceAsync()
    ├── transactionRepository.AddAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  vendor_transactions : 1行INSERT（バッグの状態は戻らない）
```

赤伝はバッグに紐づかず、バッグの状態も戻さない。制約と今後の方針は [spec/04-vendor.md](../spec/04-vendor.md#5-赤伝修正) を参照。

---

### シナリオ11: 有高チェックの結果を修正する

```
【業務】数え間違いを直す

【画面】出納帳の有高の行 → 「修正」→ 金種を入れ直して保存

【API】PUT /api/denominationchecks/safe/{id}     （小口）
       PUT /api/denominationchecks/vendor/{id}   （売上金）

【コード】
UpdatePettyCashDenominationCheckUseCase / UpdateVendorDenominationCheckUseCase
    ├── checkRepository.GetByIdAsync()     … チェック記録を読む
    ├── 帳簿額を取り直す                     … 小口は safe.PettyCashBalance、
    │                                        売上金は紐づくバッグの TotalAmount
    ├── check.Update()                     … 実数と差額を計算し直す（Domain層）
    ├── checkRepository.UpdateAsync()
    └── unitOfWork.SaveChangesAsync()

【DB変化】
  safe_denomination_checks / vendor_denomination_checks : 1行UPDATE
```

**調整取引は作り直さない。** 登録時に差額で作った調整取引はそのまま残るため、修正後の差額と帳簿がずれることがある。

---

### シナリオ12: その他（単純な操作）

| 業務 | 画面 | API | UseCase | 処理の流れ |
|---|---|---|---|---|
| 準備バッグの取消 | 売上金タブ → 準備バッグの「取消」 | POST /api/prepbags/{id}/cancel | CancelPrepBagUseCase | 読む → `bag.Cancel()`（引渡済みなら例外）→ 保存 |
| 準備バッグの有高チェック | 売上金タブ → 準備バッグの「有高」 | POST /api/denominationchecks/prepbag/{bagId} | CheckPrepBagUseCase | チェック記録を作って保存するだけ。**調整取引は作らない** |
| 金庫の作成 | 金庫の追加 | POST /api/safes | CreateSafeUseCase | `Safe.Create()`（名前必須）→ 保存 |


---

## 詳しく追う例: 小口の赤伝（シナリオ9）

上のシナリオは呼び出しの順番だけを示している。ここでは1つを選び、各層が何をしているかを実際のコードで追う。

### 全体像

```
[ブラウザ] 赤伝ボタンを押す
    ↓ POST /api/petty-cash-transactions/{id}/reverse
[Controller]  リクエストを受け取り UseCase を呼ぶだけ
    ↓
[UseCase]     手順を書く（何をどの順番でやるか）
    ↓ ↑
[Domain]      業務判断をする（赤伝をどう作るか、出金できるか）
    ↓ ↑
[Repository]  DB を読み書きする
    ↓
[DB]          PostgreSQL
```

---

### 各層のコードを追う

#### Controller（薄い）

```csharp
[HttpPost("{id}/reverse")]
public async Task<ActionResult<PettyCashTransactionDto>> Reverse(int id, ReverseTransactionRequestDto dto)
{
    var transaction = await reversePettyCashTransactionUseCase.ExecuteAsync(id, dto);
    return Ok(transaction);
}
```

Controller がやること：**UseCase を呼んで結果を返す**だけ。判断も処理もしない。

---

#### UseCase（手順を書く）

```csharp
public async Task<PettyCashTransactionDto> ExecuteAsync(int originalId, ReverseTransactionRequestDto dto)
{
    // 1. Repository: 元の取引を DB から読む
    var original = await transactionRepository.GetByIdAsync(originalId)
        ?? throw new KeyNotFoundException(...);

    // 2. Domain: 赤伝取引をどう作るか（業務判断）
    var reversal = PettyCashTransaction.CreateReversal(original, desc, DateTime.UtcNow);

    // 3. Repository: 出金になる場合は金庫を読んで残高確認
    if (reversal.Type == TransactionType.Withdrawal)
    {
        var safe = await safeRepository.GetByIdAsync(original.SafeId);
        safe.EnsureCanWithdrawPettyCash(reversal.Amount);  // Domain: 出金可能か判断
    }

    // 4. 採番・残高計算・保存（後述の付加機能を含む）
    await sequenceNumberService.AssignAsync(reversal);
    await balanceService.AssignBalanceAsync(reversal);
    await transactionRepository.AddAsync(reversal);   // Repository: 赤伝を保存
    await unitOfWork.SaveChangesAsync();              // ここで初めて DB に書き込む

    return new PettyCashTransactionDto(...);
}
```

UseCase がやること：**何をどの順番でやるか**を書く。判断は Domain に任せる。

---

#### Domain（業務判断する）

```csharp
// PettyCashTransaction.cs
public static PettyCashTransaction CreateReversal(PettyCashTransaction original, string description, DateTime date)
{
    // 「入金なら出金で打ち消す」という業務ルールがここにある
    var (reverseType, reverseAmount) = original.Type switch
    {
        TransactionType.Deposit    => (TransactionType.Withdrawal, original.Amount),
        TransactionType.Withdrawal => (TransactionType.Deposit,    original.Amount),
        TransactionType.Adjustment when original.Amount >= 0 => (TransactionType.Withdrawal, original.Amount),
        _                                                     => (TransactionType.Deposit,    Math.Abs(original.Amount)),
    };

    return new PettyCashTransaction
    {
        SafeId = original.SafeId,
        Type = reverseType,
        Amount = reverseAmount,
        Description = description,
        CreatedAt = date
    };
}
```

Domain がやること：**業務の判断**。UseCase は「作って」と依頼するだけで、どう作るかは知らない。

```csharp
// Safe.cs（別のドメインクラス）
public void EnsureCanWithdrawPettyCash(int amount)
    => EnsureCanWithdraw(amount, PettyCashBalance, "小口残高");   // 小口の出金は小口残高で判定
```

---

#### Repository（DB を読み書きする）

```csharp
// GetByIdAsync: DB から1件読む
public async Task<PettyCashTransaction?> GetByIdAsync(int id)
    => await context.PettyCashTransactions.FirstOrDefaultAsync(t => t.Id == id);
    // SQL: SELECT * FROM petty_cash_transactions WHERE id = @id LIMIT 1

// AddAsync: DB に書く予約（SaveChangesAsync で実行）
public Task AddAsync(PettyCashTransaction transaction)
{
    context.PettyCashTransactions.Add(transaction);
    // SQL: INSERT INTO petty_cash_transactions (...) VALUES (...)
    return Task.CompletedTask;
}
```

Repository がやること：**SQL を実行する**。業務の判断はしない。

---

### 採番・残高計算・コミット

UseCase の後半にある3つは、採番・残高計算・コミットの仕組み。
核の流れ（UseCase→Domain→Repository）とは独立しているので、別で理解する。

#### SequenceNumberService（採番）

```csharp
await sequenceNumberService.AssignAsync(reversal);
```

やること：出納帳の連番（1, 2, 3...）を取得して取引にセットする。

```sql
-- SQL の中身
SELECT MAX(sequence_number) FROM petty_cash_transactions WHERE safe_id = ? + 1
```

#### BalanceService（残高計算）

```csharp
await balanceService.AssignBalanceAsync(reversal);
```

やること：直前の取引行の残高を読み、今回の増減を足した新しい残高を取引にセットする。

```sql
-- SQL の中身
SELECT balance FROM petty_cash_transactions
WHERE safe_id = ? ORDER BY created_at DESC, id DESC LIMIT 1
```

残高は取引行の `balance` 列に保存される。画面の残高表示もこの値を読む。

#### unitOfWork.SaveChangesAsync（一括コミット）

```csharp
await unitOfWork.SaveChangesAsync();
```

やること：それまでの `Add()` や変更を **1トランザクションで DB に送る**。

```
AddAsync(reversal)  → BEGIN; INSERT INTO petty_cash_transactions ...; COMMIT;
```

この1行より前は「予約」、この1行で初めて DB に書き込まれる。

---

### まとめ：各層の責任

| 層 | 責任 | 判断するか |
|---|---|---|
| Controller | リクエストを受けて UseCase を呼ぶ | しない |
| UseCase | 手順を書く（何をどの順番で） | しない |
| Domain | 業務の判断（赤伝の作り方、残高チェック） | **する** |
| Repository | SQL を実行する（読む・書く） | しない |

イベントソーシング（ES+CQRS）を入れると、UseCase の後半にイベント記録と Read Model 更新が加わる。詳しくは [event-sourcing/changes-from-current.md](../event-sourcing/changes-from-current.md) を参照。

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

【売上金タブを開く】
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
    └── safes ＋ 各取引テーブルの最新行 → 各金庫の残高（売上金・小口）
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
