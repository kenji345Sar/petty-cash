# テスト仕様書

> テストコードを読まなくても「どの業務仕様を、どのテストで保証しているか」が分かることを目的とした仕様書。
>
> - ★ マークは今回（2026-06-24）新規追加したテスト
> - 仕様の詳細は [common.md](./spec/common.md) / [vendor.md](./spec/vendor.md) / [petty-cash.md](./spec/petty-cash.md) を参照

---

## 1. テスト概要

| 項目 | 値 |
|---|---|
| 対象範囲 | Domain層（Entity / ValueObject） + Application層（UseCase） |
| Domainテスト総数 | **59件** |
| Applicationテスト総数 | **55件** |
| 合計 | **114件** |
| 今回追加 | **+46件**（Domain +11 / Application +35） |
| バグ修正 | PrepBag.MarkHandedOver — Cancelled 状態から引渡できた不具合を修正 |
| 仕様未確定として残した項目 | 2件（詳細は [Section 5](#5-仕様未確定として残した項目)） |

---

## 2. 業務シナリオ別テスト仕様

### 2.1 PrepBag作成

> 複数の売上バッグ（CashBag）をまとめて業者引渡の単位（PrepBag）を作る操作。

| テスト観点 | 保証する仕様 | テストケース名 | 層 | 種別 | 結果 | 備考 |
|---|---|---|---|---|---|---|
| CashBag複数枚の合計 | TotalAmount = CashBagの合計金額 | `Create_キャッシュバッグの合計が金額になる` | Domain | 正常系 | ✅ | |
| 空リスト禁止 | CashBag 0枚で例外 | `Create_空リストは例外` | Domain | 異常系 | ✅ | |
| 初期ステータス | 作成直後はPreparing | `Create_キャッシュバッグの合計が金額になる` | Domain | 状態遷移 | ✅ | StatusをAssert |
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

> 金庫内の釣銭バッグ（ChangeBag）をレジへ移動し、出金取引を生成する操作。

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

> 業者出納帳の過去取引を打ち消す逆取引を追記する操作。元取引は変更しない。

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
| Adjustment(<0) パターン | 現実装ではAmount<0の小口取引は作成不可のため到達しない | — | — | — | — | `Create()`でAmount≤0は例外 |

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
| 残高以内の出金は通る | `EnsureCanWithdraw_残高内なら例外なし` | Domain | ✅ |
| 残高超過は例外 | `EnsureCanWithdraw_残高超過は例外` | Domain | ✅ |
| 0円以下の出金は例外 | `EnsureCanWithdraw_ゼロ以下は例外` | Domain | ✅ |

### 棚卸チェック（CheckSafe / CheckCashBag / CheckChangeBag）

| 保証する仕様 | テストケース名 | 層 | 結果 |
|---|---|---|---|
| 差額なし → 調整取引を作らない | `差額ゼロなら調整取引は作成されない` | App | ✅ |
| 差額あり → 調整取引(Adjustment)を保存 | `差額ありなら調整取引が保存される` | App | ✅ |
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

---

## 5. 仕様未確定として残した項目

### 5.1 同一取引への二重赤伝

| 項目 | 内容 |
|---|---|
| 現状の実装 | Domain に「赤伝済み」フラグがなく、同一取引IDに何度でも赤伝を打てる |
| 今後決めるべき仕様 | 「赤伝済み取引への再赤伝を禁止する」か「許容する」か |
| 禁止とした場合に追加するテスト | `ReverseVendorTransaction_赤伝済み取引に再赤伝すると例外` |
| 禁止とした場合に必要な実装 | VendorTransaction / PettyCashTransaction に `IsReversed` フラグを追加し、CreateReversal 前にチェック |

> 参照: [common.md — 二重赤伝（仕様未確定）](./spec/common.md#二重赤伝仕様未確定)

---

### 5.2 ChangeBag の MovedToRegister 後の UpdateAmount

| 項目 | 内容 |
|---|---|
| 現状の実装 | `UpdateAmount` に状態ガードがなく、`MovedToRegister` 後でも金額変更できる |
| 確認済みの挙動 | `UpdateAmount_MovedToRegister後も変更できる_仕様確認`（テストが Pass = 変更できてしまう） |
| 今後決めるべき仕様 | 「MovedToRegister後はUpdateAmountを禁止する」か「有高チェック差額更新のために許容する」か |
| 禁止とした場合に追加するテスト | `UpdateAmount_MovedToRegister後は例外` |
| 禁止とした場合に必要な実装 | `UpdateAmount` に `if (Status == BagStatus.MovedToRegister) throw ...` を追加 |

> 参照: [vendor.md — 2.1 釣銭バッグ](./spec/vendor.md#21-釣銭バッグchangebag-表示-ca-xxx)

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
