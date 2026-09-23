# テスト仕様書

> テストコードを読まなくても「どの業務仕様を、どのテストで保証しているか」が分かることを目的とした仕様書。
>
> - ★ マークは今回（2026-06-24）新規追加したテスト
> - 仕様の詳細は [common.md](./spec/02-common.md) / [vendor.md](./spec/04-vendor.md) / [petty-cash.md](./spec/03-petty-cash.md) を参照

---

## 1. テスト概要

| 項目 | 値 |
|---|---|
| 対象範囲 | Domain層（Entity / ValueObject） + Application層（UseCase） |
| Domainテスト総数 | **64件** |
| Applicationテスト総数 | **57件** |
| 合計 | **121件** |
| 今回追加 | **+46件**（Domain +11 / Application +35） |
| バグ修正 | PrepBag.MarkHandedOver — Cancelled 状態から引渡できた不具合を修正（[4.1](#41-prepbagmarkhandedover--cancelled-状態からの引渡)）<br>小口の有高チェックが合計残高と比べ、調整を売上金取引に記録していた不具合を修正（4.2）<br>出金の残高チェックが合計残高だった不具合を修正（4.3） |
| 仕様未確定として残した項目 | 2件（詳細は [Section 5](#5-仕様未確定として残した項目)） |

---

## 2. 業務シナリオ別テスト仕様

### 2.1 PrepBag作成

> 複数の売上バッグ（CashBag）をまとめて業者引渡の単位（PrepBag）を作る操作。

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| CashBag複数枚の合計 | TotalAmount = CashBagの合計金額 | `Create_売上バッグの合計が金額になる` | Domain | 正常系 | ✅ | |
| 空リスト禁止 | CashBag 0枚で例外 | `Create_空リストは例外` | Domain | 異常系 | ✅ | |
| 初期ステータス | 作成直後はPreparing | `Create_売上バッグの合計が金額になる` | Domain | 状態遷移 | ✅ | StatusをAssert |
| 正常保存 ★ | prepRepo.Add + UoW.Save が呼ばれる | `正常作成でprepRepoAddとunitOfWorkSaveが呼ばれる` | App | 正常系/保存有無 | ✅ | |
| CashBag未存在 ★ | KeyNotFoundException、保存されない | `CashBag未存在でKeyNotFoundExceptionでprepRepoは呼ばれない` | App | 異常系/保存有無 | ✅ | |
| IDs空リスト ★ | ArgumentException、保存されない | `CashBagIds空でArgumentExceptionでprepRepoは呼ばれない` | App | 異常系/保存有無 | ✅ | |

**保証できないこと（要インテグレーションテスト）**
- 既に別PrepBagに含まれているCashBagを使うと例外になること。Domain側チェックあり（`cashBag.PrepBagId != null`）だが、PrepBagIdはEF Core保存後にFKとして設定されるためユニットテストで再現不可。

---

### 2.2 PrepBagキャンセル

> Preparing 状態の PrepBag を取消し、含まれる CashBag を再割当可能にする操作。

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| Preparing→Cancelled | ステータスがCancelledに変わる | `Cancel_準備中のバッグを取消できる` | Domain | 状態遷移 | ✅ | |
| CashBag解除 | キャンセル後 CashBags が空になる | `Cancel_準備中のバッグを取消できる` | Domain | 正常系 | ✅ | |
| HandedOver→Cancel禁止 | 引渡済みのキャンセルは例外 | `Cancel_引渡済みは例外` | Domain | 異常系/状態遷移 | ✅ | |
| 二重キャンセル禁止 | 同じPrepBagを2回キャンセルすると例外 | `Cancel_二重取消は例外` | Domain | 異常系 | ✅ | |
| **Cancelled→HandedOver禁止 ★** | **キャンセル後の引渡は例外** | **`MarkHandedOver_取消済みのバッグから呼ぶと例外`** | Domain | 状態遷移 | ✅ | **バグ修正対応テスト** |
| 正常保存 ★ | Cancelledステータス確認 | `正常キャンセルでステータスがCancelledになる` | App | 正常系 | ✅ | |
| 正常保存 ★ | Update + UoW.Save が呼ばれる | `正常キャンセルでprepRepoUpdateとunitOfWorkSaveが呼ばれる` | App | 正常系/保存有無 | ✅ | |
| 未存在ID ★ | KeyNotFoundException、Updateは呼ばれない | `存在しないIDでKeyNotFoundExceptionでUpdateは呼ばれない` | App | 異常系/保存有無 | ✅ | |
| 引渡済みの例外時 ★ | InvalidOperationException、Saveは呼ばれない | `引渡済みはキャンセルできずSaveは呼ばれない` | App | 異常系/保存有無 | ✅ | |

---

### 2.3 PrepBag引渡

> Preparing 状態の PrepBag を業者へ引き渡し、出金取引を生成する操作。

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| Preparing→HandedOver | ステータスとHandedOverAtが更新される | `MarkHandedOver_ステータスが変わり出金取引が生成される` | Domain | 状態遷移 | ✅ | |
| 出金取引自動生成 | Withdrawal取引がTotalAmount金額で生成される | `MarkHandedOver_ステータスが変わり出金取引が生成される` | Domain | 正常系 | ✅ | |
| 二重引渡禁止 | 引渡済みへの再引渡は例外 | `MarkHandedOver_二重引渡は例外` | Domain | 異常系 | ✅ | |
| Cancelled→引渡禁止 ★ | キャンセル済みへの引渡は例外 | `MarkHandedOver_取消済みのバッグから呼ぶと例外` | Domain | 状態遷移 | ✅ | バグ修正対応 |
| 出金取引保存 | txRepo.Add が呼ばれる | `引渡で出金取引が保存される` | App | 正常系/保存有無 | ✅ | |
| 未存在ID | KeyNotFoundException | `準備バッグが見つからない場合は例外` | App | 異常系 | ✅ | |
| 二重引渡（App） | InvalidOperationException | `二重引渡は例外` | App | 異常系 | ✅ | |
| 正常時Save ★ | UoW.SaveChanges が呼ばれる | `正常時はunitOfWorkSaveが呼ばれる` | App | 保存有無 | ✅ | |
| Domain例外時保存なし ★ | txRepo.Add / UoW.Save が呼ばれない | `Domain例外時はtxRepoAddが呼ばれない` | App | 異常系/保存有無 | ✅ | |

---

### 2.4 CashBag入金

> レジの売上金を金庫へ入金し、売上バッグ（CashBag）として記録する操作。

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| 金種指定で作成 | TotalAmount = 金種合計、MovedToSafe状態 | `CreateDeposit_金種指定で作成できる` | Domain | 正常系/状態遷移 | ✅ | |
| 入金取引自動生成 | Deposit取引が自動生成される | `CreateDeposit_金種指定で作成できる` | Domain | 正常系 | ✅ | bag.Transaction確認 |
| 金種なし禁止 | denomination=nullは例外 | `CreateDeposit_金種なしは例外` | Domain | 異常系 | ✅ | |
| 金種合計ゼロ禁止 ★ | 全金種ゼロ枚は例外 | `CreateDeposit_金種合計ゼロは例外` | Domain | 境界値 | ✅ | ChangeBagにはあったが未対応だったため追加 |
| 金額更新 | UpdateAmountで金額変更できる | `UpdateAmount_金額が更新される` | Domain | 正常系 | ✅ | |
| 正常保存 ★ | TotalAmount/Status確認、Add+Save呼ばれる | `金種指定で正常作成されAddとSaveが呼ばれる` | App | 正常系/保存有無 | ✅ | |
| 金種なし（App）★ | 例外、Addは呼ばれない | `金種なしで例外でAddは呼ばれない` | App | 異常系/保存有無 | ✅ | |
| 金種合計ゼロ（App）★ | 例外、Addは呼ばれない | `金種合計ゼロで例外でAddは呼ばれない` | App | 境界値/保存有無 | ✅ | |

---

### 2.5 ChangeBagレジ移動

> 金庫内の両替金バッグ（ChangeBag）をレジへ移動し、出金取引を生成する操作。

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| 金種指定で入金 | TotalAmount = 金種合計、InSafe状態 | `CreateDeposit_金種指定で作成できる` | Domain | 正常系 | ✅ | |
| 金種なし禁止 | denomination=nullは例外 | `CreateDeposit_金種なしは例外` | Domain | 異常系 | ✅ | |
| 金額ゼロ禁止 | 金種合計0円は例外 | `CreateDeposit_金額ゼロは例外` | Domain | 境界値 | ✅ | |
| InSafe→MovedToRegister | ステータス変化と出金取引生成 | `MoveToRegister_ステータスが変わり出金取引が生成される` | Domain | 状態遷移 | ✅ | |
| 二重移動禁止 | 移動済みへの再移動は例外 | `MoveToRegister_二重移動は例外` | Domain | 異常系 | ✅ | |
| UpdateAmount後の状態（仕様確認）★ | MovedToRegister後もUpdateAmountが呼べる（ガードなし） | `UpdateAmount_MovedToRegister後も変更できる_仕様確認` | Domain | 状態遷移 | ✅ | ⚠️仕様未確定。[Section 5](#52-changebagsの-movedtoregister-後の-updateamount)参照 |
| 正常移動 | Withdrawal取引生成、Update呼ばれる | `レジ移動で出金取引が生成される` | App | 正常系/保存有無 | ✅ | |
| 未存在ID | KeyNotFoundException | `バッグが見つからない場合は例外` | App | 異常系 | ✅ | |
| 移動済み（App） | InvalidOperationException | `既にレジ移動済みなら例外` | App | 異常系 | ✅ | |
| 正常時Save ★ | UoW.SaveChanges が呼ばれる | `正常時はunitOfWorkSaveが呼ばれる` | App | 保存有無 | ✅ | |
| Domain例外時保存なし ★ | Update / UoW.Save が呼ばれない | `Domain例外時はbagRepoUpdateが呼ばれない` | App | 異常系/保存有無 | ✅ | |

---

### 2.6 VendorTransaction赤伝

> 売上金出納帳の過去取引を打ち消す逆取引を追記する操作。元取引は変更しない。

#### 2.6.1 逆転ロジック（Domain）

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| Deposit → Withdrawal ★ | 入金の赤伝は出金（同額） | `CreateReversal_入金の赤伝は出金になる` | Domain | 正常系 | ✅ | |
| Withdrawal → Deposit ★ | 出金の赤伝は入金（同額） | `CreateReversal_出金の赤伝は入金になる` | Domain | 正常系 | ✅ | |
| Adjustment(≥0) → Withdrawal ★ | 調整プラスの赤伝は出金（同額） | `CreateReversal_調整プラスの赤伝は出金になる` | Domain | 正常系 | ✅ | |
| Adjustment(<0) → Deposit ★ | 調整マイナスの赤伝は入金（**絶対値**） | `CreateReversal_調整マイナスの赤伝は入金で絶対値金額になる` | Domain | 正常系 | ✅ | |
| SafeId引継 ★ | SafeIdが元取引から引き継がれる | `CreateReversal_SafeIdが元取引から引き継がれる` | Domain | 正常系 | ✅ | |

#### 2.6.2 UseCase（Application）

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| 入金赤伝の保存 ★ | Withdrawal取引がtxRepo.Addで保存される | `入金取引の赤伝_出金取引が保存される` | App | 正常系/保存有無 | ✅ | |
| 出金赤伝の保存 ★ | Deposit取引がtxRepo.Addで保存される | `出金取引の赤伝_入金取引が保存される` | App | 正常系/保存有無 | ✅ | |
| 摘要（指定あり）★ | 指定した摘要がそのまま使われる | `摘要が指定された場合はその摘要が使われる` | App | 正常系 | ✅ | |
| 摘要（指定なし）★ | デフォルト摘要「#N の修正（赤伝）」 | `摘要が空の場合はデフォルト摘要になる` | App | 正常系 | ✅ | |
| 入金赤伝→残高チェックあり ★ | safeRepo.GetById が呼ばれる | `入金赤伝時はsafeRepoGetByIdが呼ばれる` | App | 正常系/保存有無 | ✅ | Deposit→Withdrawal→残高確認が必要 |
| 出金赤伝→残高チェックなし ★ | safeRepo.GetById が呼ばれない | `出金赤伝時はsafeRepoGetByIdが呼ばれない` | App | 正常系/保存有無 | ✅ | Withdrawal→Depositは残高不要 |
| 元取引未存在 ★ | KeyNotFoundException、Addは呼ばれない | `元取引未存在でKeyNotFoundExceptionでAddは呼ばれない` | App | 異常系/保存有無 | ✅ | |
| 残高不足 ★ | InvalidOperationException、Addは呼ばれない | `入金赤伝で残高不足なら例外でAddは呼ばれない` | App | 異常系/保存有無 | ✅ | |
| 正常時Save ★ | UoW.SaveChanges が呼ばれる | `正常時はunitOfWorkSaveが呼ばれる` | App | 保存有無 | ✅ | |
| 残高不足例外時Save ★ | UoW.SaveChanges が呼ばれない | `残高不足例外時はunitOfWorkSaveが呼ばれない` | App | 異常系/保存有無 | ✅ | |
| 未存在例外時Save ★ | UoW.SaveChanges が呼ばれない | `元取引未存在例外時はunitOfWorkSaveが呼ばれない` | App | 異常系/保存有無 | ✅ | |

---

### 2.7 PettyCashTransaction赤伝

> 小口出納帳の過去取引を打ち消す逆取引を追記する操作。元取引は変更しない。

#### 2.7.1 逆転ロジック（Domain）

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| Deposit → Withdrawal ★ | 入金の赤伝は出金（同額）、SafeId引継 | `CreateReversal_入金の赤伝は出金になる` | Domain | 正常系 | ✅ | |
| Withdrawal → Deposit ★ | 出金の赤伝は入金（同額） | `CreateReversal_出金の赤伝は入金になる` | Domain | 正常系 | ✅ | |
| Adjustment(>0) → Withdrawal ★ | 調整プラスの赤伝は出金 | `CreateReversal_調整プラスの赤伝は出金になる` | Domain | 正常系 | ✅ | |
| Adjustment(<0) → Deposit | 調整マイナスの赤伝は入金（**絶対値**） | `CreateReversal_調整マイナスの赤伝は入金で絶対値金額になる` | Domain | 正常系 | ✅ | 調整取引は `CreateAdjustment()` でのみ作れる（マイナス可） |

#### 2.7.2 UseCase（Application）

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| 入金赤伝の保存 ★ | Withdrawal取引がtxRepo.Addで保存される | `入金取引の赤伝_出金取引が保存される` | App | 正常系/保存有無 | ✅ | |
| 出金赤伝の保存 ★ | Deposit取引がtxRepo.Addで保存される | `出金取引の赤伝_入金取引が保存される` | App | 正常系/保存有無 | ✅ | |
| 出金赤伝→残高チェックなし ★ | safeRepo.GetById が呼ばれない | `出金赤伝時はsafeRepoGetByIdが呼ばれない` | App | 正常系/保存有無 | ✅ | |
| 元取引未存在 ★ | KeyNotFoundException、Addは呼ばれない | `元取引未存在でKeyNotFoundExceptionでAddは呼ばれない` | App | 異常系/保存有無 | ✅ | |
| 残高不足 ★ | InvalidOperationException、Addは呼ばれない | `入金赤伝で残高不足なら例外でAddは呼ばれない` | App | 異常系/保存有無 | ✅ | |
| 正常時Save ★ | UoW.SaveChanges が呼ばれる | `正常時はunitOfWorkSaveが呼ばれる` | App | 保存有無 | ✅ | |
| 残高不足例外時Save ★ | UoW.SaveChanges が呼ばれない | `残高不足例外時はunitOfWorkSaveが呼ばれない` | App | 異常系/保存有無 | ✅ | |

---

## 3. 参考：今回追加対象ではないシナリオのテスト

> 今回の追加対象には含まれていないが、既存テストで保証されているシナリオ。

### Safe（金庫）

| 保証する仕様 | テストケース名 | 層 | 結果 |
|---|---|---|---|
| 名前は必須 | `Create_名前が必須` | Domain | ✅ |
| VendorBalance + PettyCashBalance = CurrentBalance | `SetBalances_合計残高が正しい` | Domain | ✅ |
| 小口残高以内の出金は通る | `EnsureCanWithdrawPettyCash_小口残高内なら例外なし` | Domain | ✅ |
| 売上金があっても小口残高超過は例外 | `EnsureCanWithdrawPettyCash_売上金があっても小口残高超過は例外` | Domain | ✅ |
| 売上金残高以内の出金は通る | `EnsureCanWithdrawVendor_売上金残高内なら例外なし` | Domain | ✅ |
| 小口があっても売上金残高超過は例外 | `EnsureCanWithdrawVendor_小口があっても売上金残高超過は例外` | Domain | ✅ |
| 0円以下の出金は例外 | `EnsureCanWithdrawPettyCash_ゼロ以下は例外` / `EnsureCanWithdrawVendor_ゼロ以下は例外` | Domain | ✅ |

### 棚卸チェック（CheckSafe / CheckCashBag / CheckChangeBag）

| 保証する仕様 | テストケース名 | 層 | 結果 |
|---|---|---|---|
| 差額なし → 調整取引を作らない | `差額ゼロなら調整取引は作成されない` | App | ✅ |
| 差額あり → 調整取引(Adjustment)を保存（CheckSafe は小口取引に保存） | `差額ありなら調整取引が保存される` / `差額ありなら小口取引に調整が保存される` | App | ✅ |
| 金庫未存在 → KeyNotFoundException | `金庫が見つからない場合は例外` | App | ✅ |
| DenominationCheck が保存される | `有高チェックが保存される` | App | ✅ |

### 小口取引作成（CreatePettyCashTransaction）

| 保証する仕様 | テストケース名 | 層 | 結果 |
|---|---|---|---|
| 入金が作成される | `入金が正常に作成される` | App | ✅ |
| 出金時に残高チェックが行われる | `出金時に残高チェックが行われる` | App | ✅ |
| 出金時に残高不足なら例外 | `出金時に残高不足なら例外` | App | ✅ |
| 金種指定で金額が自動計算される | `金種指定で金額が自動計算される` | App | ✅ |
| 入金時は残高チェックなし | `入金時は残高チェックが行われない` | App | ✅ |

---

## 4. バグ修正に対応するテスト

### 4.1 PrepBag.MarkHandedOver — Cancelled 状態からの引渡

#### 不具合の内容

`PrepBag.MarkHandedOver` は `HandedOver` 状態のチェックのみを実施しており、`Cancelled` 状態のチェックが欠けていた。
これにより `Cancelled → HandedOver` という業務上ありえない状態遷移が例外なく通ってしまっていた。

```
// 修正前（PrepBag.cs）
if (Status == PrepBagStatus.HandedOver)   // ← Cancelled は検査していない
    throw new InvalidOperationException("...");
```

#### 追加した再現テスト

```
テストケース: MarkHandedOver_取消済みのバッグから呼ぶと例外
層: Domain（PrepBagTests.cs）
種別: 状態遷移 / バグ再現
```

このテストは修正前に **FAIL**（例外が発生しない）し、修正後に **PASS** することを確認済み。

#### 修正内容

```csharp
// PrepBag.cs — MarkHandedOver に Cancelled チェックを追加
if (Status == PrepBagStatus.Cancelled)
    throw new InvalidOperationException("取消済みの準備バッグは引渡できません。");
```

#### 修正後に保証される仕様

| 遷移 | 操作 | 結果 |
|---|---|---|
| Preparing → HandedOver | MarkHandedOver | ✅ 正常 |
| HandedOver → HandedOver | MarkHandedOver | ✅ 例外（二重引渡禁止） |
| **Cancelled → HandedOver** | **MarkHandedOver** | **✅ 例外（修正済み）** |

### 4.2 CheckSafe — 小口の有高チェックが売上金取引を調整していた

#### 不具合の内容

小口タブの「有高チェック」（`CheckSafeUseCase`）は、帳簿額に**合計残高**（売上金＋小口）を使い、差額の調整を**売上金取引**（`VendorTransaction.CreateSafeAdjustment`）に記録していた。
画面は小口残高を帳簿額として表示するため、画面どおりに数えて登録しても売上金残高分の差額が出て、売上金残高が減っていた。

```
例: 業者 48,000円 / 小口 17,100円 の金庫で 15,000円 を数えて登録
  帳簿 65,100円（合計）→ 差額 −50,100円 → 売上金取引に調整 → 売上金残高 −2,100円
```

修正内容の編集（`UpdatePettyCashDenominationCheckUseCase`）も、帳簿額に合計残高を使っていた。

#### 修正内容

- 帳簿額を `safe.PettyCashBalance`（小口残高）に変更（登録・修正の両方）
- 調整は `PettyCashTransaction.CreateAdjustment` で**小口取引**に記録。`VendorTransaction.CreateSafeAdjustment` は削除
- 画面タイトルを「有高チェック（金庫全体）」から「有高チェック（小口）」に変更

#### 追加・変更したテスト

| テスト観点 | テストケース名 | 層 |
|---|---|---|
| 帳簿額は小口残高（売上金残高を含まない） | `帳簿額は小口残高で売上金残高を含まない` | App |
| 差額は小口取引に調整として保存 | `差額ありなら小口取引に調整が保存される` | App |
| 修正時の帳簿額も小口残高 | `修正時の帳簿額も小口残高になる` | App |
| 調整取引の生成（プラス / マイナス / ゼロは例外） | `CreateAdjustment_プラス差額` / `CreateAdjustment_マイナス差額` / `CreateAdjustment_差額ゼロは例外` | Domain |

`VendorTransaction` の `CreateSafeAdjustment_*` テスト2件は、メソッドの削除に合わせて削除した。

### 4.3 Safe.EnsureCanWithdraw — 出金チェックが合計残高で判定していた

#### どの業務が、どう変わったか

| 業務 | 画面・操作 | 修正前の動き | 修正後の動き |
|---|---|---|---|
| 小口現金の出金 | 小口タブ →「入出金登録」→ 出金 | 小口 2,000円でも、売上金が 48,000円あれば 3,000円の出金が通り、小口残高が −1,000円になった | 小口残高を超える出金は「残高不足です。小口残高: 2,000円」で弾かれる |
| 小口の赤伝 | 小口タブ → 入金行の「赤伝」 | 取り消しは出金になるため、同じく売上金の残高で通っていた | 小口残高の範囲内でのみ赤伝できる |
| 売上金の赤伝 | 売上金タブ → 入金行の「赤伝」 | 小口残高を当てにして通ることがあった | 売上金残高の範囲内でのみ赤伝できる |
| バッグの移動・引渡 | 売上金タブ → バッグの「移動」「引渡」 | 残高チェックなし | 変えていない（下の「残した課題」） |

修正後の仕様は [spec/03-petty-cash.md](./spec/03-petty-cash.md) の「6. 業務ルール」と [spec/04-vendor.md](./spec/04-vendor.md) の「6.0」に記載。

#### 不具合の内容

出金できるかどうかの判定（`Safe.EnsureCanWithdraw`）が、小口残高でも売上金残高でもなく、**合計残高**（売上金＋小口）と比べていた。
小口現金と売上金は別の出納なので、片方の残高でもう片方の出金が通ってしまっていた。

```
例: 売上金 48,000円 / 小口 2,000円 の金庫
  小口から 3,000円 出金 → 合計 50,000円 と比べるので通る → 小口残高がマイナスになる
```

#### 修正内容

- `EnsureCanWithdraw` を `EnsureCanWithdrawPettyCash`（小口残高で判定）と `EnsureCanWithdrawVendor`（売上金残高で判定）に分けた。合計残高を見るメソッドは残していない
- 呼び出し側: 小口の入出金登録・小口の赤伝 → 小口用、売上金の赤伝 → 売上金用
- エラーメッセージを「小口残高」「売上金残高」と書き分けた

#### 追加・変更したテスト

上記「Safe（金庫）」の表のとおり、小口・売上金それぞれで「残高内は通る」「もう片方の残高があっても超過は例外」「0円以下は例外」を確認している。
Application 層の `出金時に小口残高を超えるなら例外_売上金は算入しない` でも、UseCase 経由で同じことを保証している。

#### 残した課題

バッグの操作（両替金バッグのレジ移動、準備バッグの引渡）には、今も残高チェックがない。バッグの金額分しか動かないため帳簿上は残高を超えない前提だが、確認はしていない。

---

## 5. 仕様未確定として残した項目

### 5.1 同一取引への二重赤伝

| 項目 | 内容 |
|---|---|
| 現状の実装 | Domain に「赤伝済み」フラグがなく、同一取引IDに何度でも赤伝を打てる |
| 今後決めるべき仕様 | 「赤伝済み取引への再赤伝を禁止する」か「許容する」か |
| 禁止とした場合に追加するテスト | `ReverseVendorTransaction_赤伝済み取引に再赤伝すると例外` |
| 禁止とした場合に必要な実装 | VendorTransaction / PettyCashTransaction に `IsReversed` フラグを追加し、CreateReversal 前にチェック |

> 参照: [common.md — 二重赤伝（仕様未確定）](./spec/02-common.md#二重赤伝仕様未確定)

---

### 5.2 ChangeBag の MovedToRegister 後の UpdateAmount

| 項目 | 内容 |
|---|---|
| 現状の実装 | `UpdateAmount` に状態ガードがなく、`MovedToRegister` 後でも金額変更できる |
| 確認済みの挙動 | `UpdateAmount_MovedToRegister後も変更できる_仕様確認`（テストが Pass = 変更できてしまう） |
| 今後決めるべき仕様 | 「MovedToRegister後はUpdateAmountを禁止する」か「有高チェック差額更新のために許容する」か |
| 禁止とした場合に追加するテスト | `UpdateAmount_MovedToRegister後は例外` |
| 禁止とした場合に必要な実装 | `UpdateAmount` に `if (Status == BagStatus.MovedToRegister) throw ...` を追加 |

> 参照: [vendor.md — 2.1 両替金バッグ](./spec/04-vendor.md#21-両替金バッグchangebag-表示-ca-xxx)

---

## 6. 今回のテストで保証できること／できないこと

### 保証できること

| カテゴリ | 内容 |
|---|---|
| ドメイン業務ルール | 各バッグの生成・状態遷移・取引自動生成ルール |
| 状態遷移 | PrepBag: Preparing/HandedOver/Cancelled の正当な遷移とすべての禁止遷移 |
| 赤伝ロジック | Deposit/Withdrawal/Adjustment±の全4パターンの逆転計算 |
| 残高チェック | 入金赤伝時の残高不足で例外、出金赤伝は残高チェックなし |
| 保存の有無 | 正常時: Repository.Add/Update + UoW.Save が1回呼ばれる |
| 保存の有無 | 例外時: Repository.Add/Update / UoW.Save が呼ばれない |
| 境界値 | 金額ゼロ・金種合計ゼロは例外、残高ちょうどは通る |
| バグ修正 | PrepBag の Cancelled→HandedOver を修正・テストで固定 |

### まだ保証できないこと

| カテゴリ | 内容 | 理由 |
|---|---|---|
| インテグレーション | DB保存後の残高計算の正確性 | BalanceServiceはMockのためDB不使用 |
| インテグレーション | CashBag重複PrepBag割当の防止 | PrepBagIdはEF Core FK保存後に設定されるためユニットテスト不可 |
| インテグレーション | 連番（SequenceNumber）の一意性 | SequenceNumberServiceはMock |
| 仕様未確定 | 同一取引への二重赤伝 | 仕様が決まっていない |
| 仕様未確定 | MovedToRegister後のUpdateAmount | 仕様が決まっていない |
| フロントエンド | 赤伝モーダルの表示・送信 | Reactコンポーネントのテストなし |
| フロントエンド | バッグ一覧のステータス表示 | 同上 |
| フロントエンド | PrepBag作成時のCashBag選択操作 | 同上 |
