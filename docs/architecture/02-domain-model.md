# ドメインモデル

このシステムの主役はドメイン層。業務ルールはすべて `PettyCash.Domain` に集中している。
UseCase やインフラ層はドメインを「使う側」に過ぎない。

このページは、何というモデルがあるか（一覧・関係・ないもの）と、どの業務ルールがどこにあるかの2本立て。

---

## ドメインが主役である理由

```
Controller  → HTTP の受け口。業務ルールは知らない
UseCase     → ドメインを組み合わせて業務行為を実現する「指揮者」
Domain      → 業務ルールそのもの。「何が許されて何が許されないか」を持つ  ← 主役
Infrastructure → DBへの読み書き。業務ルールは知らない
```

UseCase が長くなっても業務ルールは Domain に書く。
Domain を読めば「このシステムが何を守っているか」がわかる。

---

## モデル一覧

### エンティティ（同一性を持つ。IDで識別する）

| クラス | 何を表すか | 置き場所 |
|---|---|---|
| `Safe` | 拠点（名古屋・梅田・銀座）。名前と説明だけを持つ | `Safe/` |
| `PettyCashTransaction` | 小口の取引1件（小口出納帳の1行） | `PettyCash/Ledger/` |
| `PettyCashDenominationCheck` | 小口の有高チェック1件 | `PettyCash/DenomCheck/` |
| `VendorTransaction` | 売上金の取引1件（売上金出納帳の1行） | `Vendor/Ledger/` |
| `VendorDenominationCheck` | 売上金の有高チェック1件（バッグを数えた記録） | `Vendor/DenomCheck/` |
| `ChangeBag` | 両替金バッグ | `Vendor/BagManagement/` |
| `CashBag` | 売上バッグ | `Vendor/BagManagement/` |
| `PrepBag` | 準備バッグ | `Vendor/BagManagement/` |

### 値オブジェクト（IDを持たない。中身が同じなら同じもの）

| クラス | 何を表すか |
|---|---|
| `Denomination` | 金種ごとの枚数と合計金額。`record` で不変。枚数は0以上（→ 下の「設計判断」） |

**値オブジェクトはこの1つだけ。** 金額・残高・通し番号はすべて `int` のまま扱っている。

### 列挙型

| クラス | 値 |
|---|---|
| `TransactionType` | 入金（Deposit）/ 出金（Withdrawal）/ 調整（Adjustment） |
| `BagStatus` | 金庫内（InSafe）/ レジへ移動済み（MovedToRegister） … 両替金バッグ |
| `CashBagStatus` | レジにあり（AtRegister）/ 金庫へ移動済み（MovedToSafe） … 売上バッグ |
| `PrepBagStatus` | 準備中（Preparing）/ 引渡済（HandedOver）/ 取消（Cancelled） |

### ドメインが持つインターフェース（実装はインフラ層）

`ISafeRepository` などのリポジトリ7つと、`IBalanceService`（残高計算）・`ISequenceNumberService`（採番）・`IUnitOfWork`（コミット）。

---

## モデルの関係

```
Safe（拠点）
 │  ※ 残高は持たない。読み込み時に取引テーブルから詰められる（05-balance-design.md）
 │
 ├── 小口 ────────────────────────────────────────────
 │     PettyCashTransaction（入金・出金・調整）
 │     PettyCashDenominationCheck（金庫の小口現金を数えた記録）
 │
 └── 売上金 ──────────────────────────────────────────
       ChangeBag（両替金バッグ）
         ├─ 入金時と移動時に VendorTransaction を1件ずつ生成
         └─ VendorDenominationCheck で数える
       CashBag（売上バッグ）
         ├─ 入金時に VendorTransaction を1件生成
         ├─ PrepBag に属する（1つの準備バッグにのみ）
         └─ VendorDenominationCheck で数える
       PrepBag（準備バッグ）
         ├─ CashBag を複数まとめる
         ├─ 引渡時に VendorTransaction を1件生成
         └─ VendorDenominationCheck で数える
```

- 小口と売上金は、エンティティもテーブルも完全に分かれている。共有しているのは `Safe`（どの拠点か）と、金種の `Denomination` だけ。
- 売上金の取引は、**バッグ操作から自動で生まれる**（直接作るのは調整取引だけ）。小口の取引は、画面から直接作る。

### どこから触るか（集約の入口）

| 入口 | まとめて面倒をみるもの |
|---|---|
| `ChangeBag` / `CashBag` | 自分と、それが生む `VendorTransaction` |
| `PrepBag` | 自分と、含まれる `CashBag`、引渡時の `VendorTransaction` |
| `PettyCashTransaction` | 自分だけ |
| `Safe` | 自分だけ（＋読み込み時に詰められた残高） |

バッグと取引は必ずセットで生まれる。だから UseCase が取引を別々に作るのではなく、バッグのメソッドが取引を作って返す。

---

## モデルに「ないもの」

今の設計に存在しないもの。コードを読んで探しても見つからないので、先に挙げておく。

| ないもの | 今どうなっているか |
|---|---|
| 小口勘定 / 売上金勘定（残高の持ち主） | `Safe` が2つの残高を読み込み時に持つ。出金の判定も `Safe` にある（`EnsureCanWithdrawPettyCash` / `EnsureCanWithdrawVendor`） |
| 店舗・場所 | ない。`Safe.Name` に店舗名が入っているだけ。「1拠点に金庫が複数」は表現できない |
| 締め処理・繰越残高 | ない。残高は開店以来の累計（最新の取引行の `balance`）。画面の月送りは表示の絞り込みだけ |
| 金額・残高の型 | `int`。小口残高と売上金残高を型で区別できないため、取り違えをコンパイル時に防げない |
| 取引の取消フラグ | ない。同じ取引に何度でも赤伝を打てる（[test-specification.md](../test-specification.md) の 5.1） |

---

## Safe（金庫）の業務ルール

```csharp
// backend/PettyCash.Domain/Safe/Safe.cs
```

| ルール | コード |
|-------|-------|
| 金庫名は必須 | `if (string.IsNullOrWhiteSpace(name)) throw` |
| 残高は売上金・小口に分かれる | `VendorBalance` / `PettyCashBalance` の2プロパティ |
| その出納の残高を超える出金はできない | `EnsureCanWithdrawPettyCash()` / `EnsureCanWithdrawVendor()` で、それぞれの残高を超える額を弾く |

```csharp
// 小口の出金は小口残高で判定する（売上金残高は算入しない）
public void EnsureCanWithdrawPettyCash(int amount)
    => EnsureCanWithdraw(amount, PettyCashBalance, "小口残高");

// 売上金の出金は売上金残高で判定する
public void EnsureCanWithdrawVendor(int amount)
    => EnsureCanWithdraw(amount, VendorBalance, "売上金残高");
```

合計残高（`CurrentBalance`）は画面の表示にだけ使い、出金の判定には使わない。小口現金と売上金は別の出納なので、片方の残高でもう片方の出金を通してはいけない。

UseCase は `safe.EnsureCanWithdrawPettyCash(amount)` のように、その出納に合うほうを呼ぶだけ。
「残高チェックのロジック」はドメインが持っている。

---

## ChangeBag（両替金バッグ）の業務ルール

```csharp
// backend/PettyCash.Domain/Vendor/BagManagement/ChangeBag.cs
```

| ルール | コード |
|-------|-------|
| 入金時は金種表の入力が必須 | `if (denomination == null) throw` |
| 金額は金種の合計から自動算出 | `denomination.TotalAmount` を使う（直接入力不可） |
| レジへの移動は1回のみ | `if (Status == MovedToRegister) throw` |
| 入金・移動時に取引が自動生成 | `CreateDeposit()` / `MoveToRegister()` の中で `VendorTransaction` を生成 |

```csharp
public static ChangeBag CreateDeposit(..., Denomination? denomination = null)
{
    if (denomination == null)
        throw new ArgumentException("金種表の入力が必須です。");
    // 金種の合計を金額として使う（直接入力させない）
    var finalAmount = denomination.TotalAmount;
}

public VendorTransaction MoveToRegister(DateTime movedAt)
{
    if (Status == BagStatus.MovedToRegister)
        throw new InvalidOperationException("このバッグは既にレジへ移動済みです。");
    // 状態変更と出金取引の生成を同時に行う
    Status = BagStatus.MovedToRegister;
    return VendorTransaction.CreateWithdrawal(this, TotalAmount, movedAt);
}
```

---

## PrepBag（準備バッグ）の業務ルール

```csharp
// backend/PettyCash.Domain/Vendor/BagManagement/PrepBag.cs
```

| ルール | コード |
|-------|-------|
| 売上バッグを1つ以上必要 | `if (cashBags.Count == 0) throw` |
| 他の準備バッグに含まれているバッグは追加不可 | `if (cashBag.PrepBagId != null) throw` |
| 合計金額はバッグの合算で自動算出 | `cashBags.Sum(b => b.TotalAmount)` |
| 引渡済みは取消できない | `if (Status == HandedOver) throw` |
| 引渡時に出金取引が自動生成 | `MarkHandedOver()` の中で `VendorTransaction` を生成 |

---

## PettyCashTransaction（小口の取引）の業務ルール

小口出納帳の1行。画面の「入出金登録」から直接作られる。

| ルール | コード |
|---|---|
| 金額は1円以上 | `Create()` で `finalAmount <= 0` を弾く |
| 金種を入れたら、その合計が金額になる | `Create()` で `denomination?.TotalAmount ?? amount` |
| 調整取引は差額そのものを金額にする（マイナスもある） | `CreateAdjustment()`。差額ゼロは例外 |
| 赤伝は元取引の逆になる | `CreateReversal()`。入金→出金、出金→入金、調整(≥0)→出金、調整(<0)→入金（絶対値） |
| 採番は1回だけ。二重採番は例外 | `SetSequenceNumber()` |
| 残高は外から入る（自分では計算しない） | `SetBalance()` は `internal`。計算するのは `BalanceService` |

```csharp
public static PettyCashTransaction CreateAdjustment(int safeId, int difference, DateTime date)
{
    if (difference == 0)
        throw new ArgumentException("差額がゼロの場合は調整取引を作成しません。");
    ...
}
```

---

## VendorTransaction（売上金の取引）の業務ルール

売上金出納帳の1行。**バッグ操作から自動で生まれる**のが小口との違い。画面から直接登録する口はない。

| ルール | コード |
|---|---|
| バッグ操作から生成される | `ChangeBag.CreateDeposit()` / `MoveToRegister()`、`CashBag.CreateDeposit()`、`PrepBag.MarkHandedOver()` が内部で作る |
| どのバッグの取引かを持つ | `ChangeBagId` / `CashBagId` / `PrepBagId` のいずれか |
| 調整取引はバッグに紐づけて作る | `CreateAdjustment(ChangeBag, ...)` / `CreateAdjustment(CashBag, ...)` |
| 赤伝の逆転ルールは小口と同じ | `CreateReversal()` |
| 赤伝はバッグIDを引き継がない | `CreateReversal()` が `SafeId / Type / Amount / Description / CreatedAt` だけを写す（[spec/04-vendor.md](../spec/04-vendor.md#5-赤伝修正)） |
| 採番・残高の扱いは小口と同じ | `SetSequenceNumber()` / `SetBalance()` |

---

## CashBag（売上バッグ）の業務ルール

| ルール | コード |
|---|---|
| 入金時は金種表が必須（金額の直接入力は不可） | `CreateDeposit()` で `denomination == null` を弾く |
| 金額は金種の合計から決まる | `CreateDeposit()` |
| 入金時に入金取引が1件生まれる | `CreateDeposit()` が `VendorTransaction.CreateCashBagDeposit()` を呼ぶ |
| 1つの売上バッグは1つの準備バッグにしか入れない | `PrepBag.Create()` が `PrepBagId` の有無で弾く |

---

## 有高チェック（DenominationCheck）の業務ルール

小口用（`PettyCashDenominationCheck`）と売上金用（`VendorDenominationCheck`）に分かれている。

| ルール | 小口 | 売上金 |
|---|---|---|
| 帳簿額（ExpectedAmount） | 金庫の小口残高 | 数えたバッグの `TotalAmount` |
| 実数（CheckedAmount） | 入力された金種の合計 | 同左 |
| 差額（Difference） | 実数 − 帳簿（プラスは過剰、マイナスは不足） | 同左 |
| 対象 | 金庫（小口） | `ChangeBag` / `CashBag` / `PrepBag` のいずれか1つ |
| 差額が出たとき | UseCase が小口の調整取引を作る | UseCase が売上金の調整取引を作り、バッグ金額を実数に更新（準備バッグは調整しない） |
| 後から修正できる | `Update()` で実数と差額を計算し直す。**調整取引は作り直さない** | 同左 |

差額から調整取引を作るのは UseCase の仕事で、チェックのエンティティ自身は取引を作らない。

---

## ドメインが「守っているもの」の全体像

```
業務で「やってはいけないこと」がすべて throw になっている

ChangeBag.CreateDeposit()    → 金種なしの入金を弾く
ChangeBag.MoveToRegister()   → 二重移動を弾く
PrepBag.Create()             → バッグなし・重複バッグを弾く
PrepBag.Cancel()             → 引渡済みの取消を弾く
PrepBag.MarkHandedOver()     → 二重引渡を弾く
CashBag.CreateDeposit()      → 金種なしの入金を弾く
PettyCashTransaction.Create()→ 0円以下の取引を弾く
PettyCashTransaction.CreateAdjustment() → 差額ゼロの調整取引を弾く
Safe.EnsureCanWithdrawPettyCash() / EnsureCanWithdrawVendor() → その出納の残高を超える出金を弾く
Safe.Create()                → 名前なしの金庫作成を弾く
```

UseCase がこれらを呼ぶことで、業務ルール違反は自動的にエラーになる。
インフラ層やコントローラーはこれらのルールを知らなくてよい。

---

## 設計判断: 金種（値オブジェクト）と有高チェック（エンティティ）

金種と有高チェックの業務上の意味は [spec/02-common.md](../spec/02-common.md) の「1. 金種」「2. 有高チェック」を参照。ここではドメインでどう表現しているかを書く。

| クラス | 種類 | 表すもの |
|---|---|---|
| `Denomination` | 値オブジェクト | 紙幣・硬貨ごとの枚数と、その合計金額（`TotalAmount`） |
| `VendorDenominationCheck` / `PettyCashDenominationCheck` | エンティティ | 「実際に数えた」という記録。`Denomination` を持ち、帳簿との差額を計算する |

`Denomination` は2つの場面で使われる。

| 用途 | 使用箇所 |
|---|---|
| 有高チェックで数えた枚数 | `*DenominationCheck.Denomination` |
| 取引に含まれる金種の内訳 | `VendorTransaction.Denomination` / `PettyCashTransaction.Denomination` |

### なぜ金種を用途ごとに分けないのか

値オブジェクト `Denomination` は共通のまま、**意味の違いはそれを持つエンティティが担う**。

- データ構造（1万円○枚、千円○枚…）は同じ
- 計算ルール（`TotalAmount`）も同じ
- 違うのは「何の文脈で使われるか」だけで、それは入っている先（チェックか取引か）で決まる

用途ごとに分けると、同じ計算ロジックを2か所で保守することになり、片方だけ直して反映し忘れる事故が起きる。

### 画面側の分け方

UI の流れの違い（金種表で金額をセットするか、有高チェックとして保存するか）は、ドメインではなく画面側で分けている。

| コンポーネント | 用途 |
|---|---|
| `DenominationInput.tsx` | 金種表。合計を金額欄にセットして戻る |
| `DenominationCheckForm.tsx` | 有高チェック。帳簿との差額を表示し、API で保存する |

`DenominationInput.tsx` は金種の定義を `frontend/src/shared/denomination.ts`（`DENOM_ITEMS`）から読むが、`DenominationCheckForm.tsx` は同じ一覧をファイル内に持っている（重複。[issue.md](../issue.md) の 2.）。

---

## 「ドメインが主役」の確認方法

ドメインのファイルを開くと、クラスのコメント（`///`）に業務ルールが書いてある。

```csharp
/// <summary>
/// 両替金バッグ。業者から受け取った両替金を金庫で保管し、必要に応じてレジへ移動する。
///
/// 【業務ルール】
///   - 入金時は金種表の入力が必須（金額直接入力は不可）
///   - レジへの移動は1回のみ（二重移動は不可）
/// </summary>
```

新しい業務ルールが生まれたとき → まずドメインに書く、が設計の基本方針。
