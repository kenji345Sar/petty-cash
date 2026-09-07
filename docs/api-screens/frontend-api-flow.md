# フロントエンド → API 呼び出しフロー

## 業者タブ（VendorTab）の例

### タブ表示時（データ取得）

業者タブを開いた瞬間に `Promise.all` で5つのGETを**並列**に呼び出す。
全部返ってきたら画面を描画する。

```
業者タブ表示 (VendorTab.loadData)
  │
  ├─→ GET /api/bags?safeId=           → BagsController        → GetBagsUseCase
  ├─→ GET /api/cashbags?safeId=       → CashBagsController    → GetCashBagsUseCase
  ├─→ GET /api/vendor-transactions?safeId= → VendorTransactionsController → GetVendorTransactionsUseCase
  ├─→ GET /api/denominationchecks/vendor?safeId= → DenominationChecksController → GetVendorDenominationChecksUseCase
  └─→ GET /api/prepbags?safeId=       → PrepBagsController    → GetPrepBagsUseCase
  │
  全部返ってきたら画面描画
```

### ユーザー操作時（各ボタン押下で呼ばれるAPI）

#### 両替金バッグ（BagList コンポーネント）

| 操作 | API | コントローラー |
|---|---|---|
| 両替金追加 → 入金する | POST `/api/bags/deposit` | BagsController |
| 移動ボタン | POST `/api/bags/{id}/move` | BagsController |
| 有高ボタン（新規チェック） | POST `/api/denominationchecks/changebag/{id}` | DenominationChecksController |
| 有高行クリック（修正） | PUT `/api/denominationchecks/vendor/{id}` | DenominationChecksController |

#### 売上バッグ（CashBagList コンポーネント）

| 操作 | API | コントローラー |
|---|---|---|
| 売上バッグ追加 → 入金する | POST `/api/cashbags/deposit` | CashBagsController |
| 有高ボタン（新規チェック） | POST `/api/denominationchecks/cashbag/{id}` | DenominationChecksController |
| 有高行クリック（修正） | PUT `/api/denominationchecks/vendor/{id}` | DenominationChecksController |

#### 準備バッグ（CashBagList コンポーネント内）

| 操作 | API | コントローラー |
|---|---|---|
| CashBag→準備Bag → 作成 | POST `/api/prepbags` | PrepBagsController |
| 引渡ボタン | POST `/api/prepbags/{id}/handover` | PrepBagsController |
| 戻すボタン | POST `/api/prepbags/{id}/cancel` | PrepBagsController |
| 有高ボタン（新規チェック） | POST `/api/denominationchecks/prepbag/{id}` | DenominationChecksController |
| 有高行クリック（修正） | PUT `/api/denominationchecks/vendor/{id}` | DenominationChecksController |

#### 業者出納帳（VendorTab 内）

| 操作 | API | コントローラー |
|---|---|---|
| 有高行クリック（修正） | PUT `/api/denominationchecks/vendor/{id}` | DenominationChecksController |

### データ更新後のリロード

入金・移動・有高チェックなどの操作後は `handleUpdate` → `loadData` が呼ばれ、
上記5つのGETが再度並列実行されて画面全体が最新状態に更新される。

## コントローラー × タブ対応表

| コントローラー | 小口タブ | 業者タブ |
|---|---|---|
| SafesController | (ヘッダー) | (ヘッダー) |
| PettyCashTransactionsController | o | - |
| BagsController | - | o |
| CashBagsController | - | o |
| PrepBagsController | - | o |
| VendorTransactionsController | - | o |
| DenominationChecksController | o | o |
