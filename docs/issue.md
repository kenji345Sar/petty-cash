# 未解決の問題

見つかったが、まだ直していない問題を記録する。直したら該当項目を削除し、コミットメッセージかテスト仕様書（[test-specification.md](test-specification.md)）に経緯を残す。

---

## 1. 1回の処理で複数の記録を採番すると番号が重複する

- **見つけた日**: 2026-09-21
- **状態**: 未対応

### 現象

小口タブの有高チェックで差額が出ると、有高チェックの記録と調整取引が**同じ番号**になる。

```
例（銀座 2026-09-21）
  15 | 調整 | -2,100円 | 小口有高調整（-2,100円）
  15 | 有高 | 15,000円 | 有高: 15,000円 / 帳簿: 17,100円
```

### 原因

[SequenceNumberService](../backend/PettyCash.Infrastructure/Services/SequenceNumberService.cs) は、4テーブル（業者取引・小口取引・業者有高チェック・小口有高チェック）の `MAX(sequence_number) + 1` を **DB から読んで**番号を決める。

有高チェックの UseCase は、チェック記録と調整取引を続けて採番してから、最後に `SaveChangesAsync` でまとめて保存する。
1つ目の番号はまだ DB に保存されていないので、2つ目も同じ番号を読んでしまう。

```
AssignAsync(check)       → DB の MAX = 14 → 15
AssignAsync(adjustment)  → DB の MAX = 14（check は未保存）→ 15  ★重複
SaveChangesAsync()
```

### 影響範囲

採番を2回以上してから保存する UseCase すべて。

- `CheckSafeUseCase`（小口の有高チェック、差額ありのとき）
- `CheckChangeBagUseCase` / `CheckCashBagUseCase`（業者のバッグの有高チェック、差額ありのとき）

同時に2人が登録した場合も、同じ番号を読む可能性がある（排他制御がない）。

### 直し方の候補

| 案 | 内容 |
|---|---|
| A | 1回の処理の中で振った番号を SequenceNumberService が覚えておき、次は DB の MAX と比べて大きいほう +1 にする（リクエスト単位のサービスなので保持できる） |
| B | 金庫ごとの採番テーブル、または PostgreSQL のシーケンスで採番する（同時登録にも対応できる） |

---

## 2. 小口の出金チェックが合計残高で判定している

- **見つけた日**: 2026-09-21
- **状態**: 未対応（仕様の確認が必要）

### 現象

小口の出金（入出金登録・赤伝）で残高不足を判定する `Safe.EnsureCanWithdraw` が、小口残高ではなく**合計残高**（業者＋小口）と比べている。
そのため、小口残高が 15,000円でも、業者残高が 48,000円あれば 60,000円の出金が通ってしまい、小口残高がマイナスになる。

```csharp
// backend/PettyCash.Domain/Safe/Safe.cs
if (amount > CurrentBalance)   // CurrentBalance = VendorBalance + PettyCashBalance
    throw new InvalidOperationException(...);
```

### 確認したいこと

- 小口の出金は、小口残高の範囲内に限るべきか（小口の有高チェックと同じ考え方なら、限るべき）
- 業者側の出金にも、同じ判定を入れるべきか

### 関連

小口の有高チェックが合計残高と比べていた不具合は、2026-09-21 に修正済み（[test-specification.md](test-specification.md) の 4.2）。これと同じ種類の問題。
