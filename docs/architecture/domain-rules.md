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
Safe.EnsureCanWithdraw()     → 残高超過の出金を弾く
Safe.Create()                → 名前なしの金庫作成を弾く
```

UseCase がこれらを呼ぶことで、業務ルール違反は自動的にエラーになる。
インフラ層やコントローラーはこれらのルールを知らなくてよい。

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
