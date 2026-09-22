# ドメイン — 業務ルールの在り処

このシステムの主役はドメイン層。業務ルールはすべて `PettyCash.Domain` に集中している。
UseCase やインフラ層はドメインを「使う側」に過ぎない。

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

## Safe（金庫）の業務ルール

```csharp
// backend/PettyCash.Domain/Safe/Safe.cs
```

| ルール | コード |
|-------|-------|
| 金庫名は必須 | `if (string.IsNullOrWhiteSpace(name)) throw` |
| 残高は業者・小口に分かれる | `VendorBalance` / `PettyCashBalance` の2プロパティ |
| 残高を超える出金はできない | `EnsureCanWithdraw()` で `amount > CurrentBalance` を弾く |

```csharp
public void EnsureCanWithdraw(int amount)
{
    if (amount > CurrentBalance)
        throw new InvalidOperationException(
            $"残高不足です。現在残高: {CurrentBalance}円、出金額: {amount}円");
}
```

UseCase は `safe.EnsureCanWithdraw(amount)` を呼ぶだけ。
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

## ドメインが「守っているもの」の全体像

```
業務で「やってはいけないこと」がすべて throw になっている

ChangeBag.CreateDeposit()    → 金種なしの入金を弾く
ChangeBag.MoveToRegister()   → 二重移動を弾く
PrepBag.Create()             → バッグなし・重複バッグを弾く
PrepBag.Cancel()             → 引渡済みの取消を弾く
PrepBag.MarkHandedOver()     → 二重引渡を弾く
PettyCashTransaction.CreateAdjustment() → 差額ゼロの調整取引を弾く
Safe.EnsureCanWithdraw()     → 残高超過の出金を弾く
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

`DenominationInput.tsx` は金種の定義を `frontend/src/shared/denomination.ts`（`DENOM_ITEMS`）から読むが、`DenominationCheckForm.tsx` は同じ一覧をファイル内に持っている（重複。[issue.md](../issue.md) の 3.）。

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
