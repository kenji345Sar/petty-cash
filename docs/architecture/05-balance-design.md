# 残高の設計

残高を「金庫」ではなく「取引の各行」に持たせている理由と、その変遷をまとめる。
業務上の仕様（残高は取引が持つ・書き込み時に1回だけ計算する）は [spec/02-common.md](../spec/02-common.md) の「4. 残高」を参照。

---

## 残高計算方式の変遷

### 変更前（475b4a9以前）— SUM毎回計算方式

残高はDBに保存せず、**読み込みのたびにSUM関数で全取引を集計**して算出していた。

```
SafeRepository.LoadBalances():

SELECT
  COALESCE(
    (SELECT SUM(
      CASE WHEN type = 2 THEN amount    -- Adjustment: +amount
           WHEN type = 0 THEN amount    -- Deposit:    +amount
           ELSE -amount                  -- Withdrawal: -amount
      END
    ) FROM vendor_transactions WHERE safe_id = {0}),
  0) AS "VendorBalance",

  COALESCE(
    (SELECT SUM(
      CASE WHEN type = 2 THEN amount
           WHEN type = 0 THEN amount
           ELSE -amount
      END
    ) FROM petty_cash_transactions WHERE safe_id = {0}),
  0) AS "PettyCashBalance"
```

**特徴:**
- balance列なし — 取引テーブルにはamountのみ
- 読み込みのたびに全取引をSUMで集計
- 任意時点の残高を知りたい場合もSUMが必要
- 取引が増えるほどクエリが遅くなる

**呼び出し箇所:** 金庫を取得する全ての場所（GetSafes, GetByIdAsync, Dashboard等）で毎回実行

### 変更後（475b4a9）— ランニングバランス方式

各取引テーブルにbalance列を追加。取引ごとにその時点の残高を保持。

```
SafeRepository.LoadBalances():

SELECT
  COALESCE(
    (SELECT balance FROM vendor_transactions
     WHERE safe_id = {0} ORDER BY created_at DESC, id DESC LIMIT 1),
  0) AS "VendorBalance",

  COALESCE(
    (SELECT balance FROM petty_cash_transactions
     WHERE safe_id = {0} ORDER BY created_at DESC, id DESC LIMIT 1),
  0) AS "PettyCashBalance"
```

**特徴:**
- balance列あり — 各取引にその時点の残高を保持
- 最新の1行を読むだけで現在残高がわかる
- 任意時点の残高もその行を見るだけ
- 取引数に関係なく一定速度
- 時系列順に追記する前提（→ 下の「注意」）

---

## 残高を読む場所

金庫を取得するときに、`SafeRepository.LoadBalances()` が業者・小口それぞれの取引テーブルから**最新1行の `balance`** を読み、`safe.SetBalances()` でセットする。

| 呼び出し元 | 用途 |
|---|---|
| `GetSafesUseCase` | ヘッダーの金庫一覧（各金庫の業者・小口残高） |
| `GetPettyCashDashboardUseCase` / `GetVendorDashboardUseCase` | タブを開いたときの残高表示 |
| `CheckSafeUseCase` | 小口の有高チェックの帳簿額（小口残高） |
| `CreatePettyCashTransactionUseCase` / `Reverse*TransactionUseCase` | 出金前の残高確認（`EnsureCanWithdraw`） |

書き込み時は `BalanceService.AssignBalanceAsync()` が同じく最新1行の `balance` を読み、今回の増減を足して新しい取引行にセットする。

---

## 注意: 時系列順に追記する前提

ランニングバランス方式は「最新の行 = 現在の残高」を前提にしている。
過去の日付で取引を登録すると、その行は並び順では途中に入るのに、残高は「登録時点の最新残高 + 増減」で計算されるため、前後の行と残高がずれる。
現在、サーバー側で過去日付の登録を弾く処理はない（[issue.md](../issue.md) の 4.）。

---

イベントソーシング（ES+CQRS）を入れた場合、残高の読み込み先は Read Model（`safe_balances`）に変わる。詳しくは [event-sourcing/](../event-sourcing/README.md) を参照。
