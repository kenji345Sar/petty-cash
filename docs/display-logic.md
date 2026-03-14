# 表示ロジック

## 全体構成

```
App.tsx（メインコンテナ）
├── 金庫セレクター（ヘッダー）
└── TransactionList.tsx（メインコンテンツ）
    ├── 出納帳テーブル
    ├── BagList.tsx（釣り銭バッグ管理）
    ├── CashBagList.tsx（キャッシュバッグ管理）
    └── DenominationCheckPage.tsx（有高チェック）
```

## 1. 金庫セレクターと残高表示

### フロントエンド（App.tsx）

```
起動 → GET /api/safes → safes state にセット → 最初の金庫を自動選択
```

- `loadSafes()` で全金庫を取得
- `selectedSafeId === null` のとき、先頭の金庫IDを `selectedSafeId` にセット
- セレクトボックスで金庫名を表示、横に「残高: X円」を表示

### バックエンド（残高計算）

`Safe.cs` の `CurrentBalance` プロパティ（計算プロパティ）:

```
CurrentBalance = 全Deposit取引の合計額 − 全Withdrawal取引の合計額
```

- DBに残高カラムは持たない
- `SafeRepository` が `Include(s => s.Transactions)` でTransactionをロード
- Transactionの合計から毎回リアルタイム算出

## 2. 金庫切替 → データ再取得

### トリガー

```tsx
useEffect(() => {
  if (selectedSafeId !== null) {
    loadData(selectedSafeId);
  }
}, [selectedSafeId]);
```

`selectedSafeId` が変わるたびに `loadData(safeId)` が発火。

### API呼び出し（並列）

`loadData(safeId)` 内で `Promise.all` により5つのAPIを同時呼び出し:

| API | 取得データ | 用途 |
|-----|-----------|------|
| `GET /api/bags?safeId=X` | ChangeBag[] | 釣り銭バッグ一覧 |
| `GET /api/cashbags?safeId=X` | CashBag[] | キャッシュバッグ一覧 |
| `GET /api/transactions?safeId=X` | Transaction[] | 出納帳の取引一覧 |
| `GET /api/denominationchecks?safeId=X` | DenominationCheck[] | 金種チェック履歴 |
| `GET /api/prepbags?safeId=X` | PrepBag[] | 準備バッグ一覧 |

### バックエンドのフィルタリング

全APIで同じパターン:

```
Controller([FromQuery] int safeId)
  → UseCase.ExecuteAsync(safeId)
    → Repository.GetBySafeIdAsync(safeId)
      → _context.Entity.Where(e => e.SafeId == safeId).ToListAsync()
```

SafeIdは各エンティティの外部キーとしてDB上に存在し、WHERE句でフィルタ。

## 3. 出納帳テーブル（TransactionList.tsx）

### 表示カラム

| カラム | データソース |
|--------|-------------|
| 種別 | `transaction.type`（Deposit=入金 / Withdrawal=出金） |
| 金額 | `transaction.amount` |
| 摘要 | `transaction.description` |
| バッグ | `changeBagId` / `cashBagId` から対応バッグを検索して表示 |
| 日時 | `transaction.createdAt` |

### バッグ列の表示ロジック

```
changeBagId != null → 釣り銭バッグから検索 → 「釣り銭#ID 金額円」
cashBagId != null   → キャッシュバッグから検索 → 「キャッシュ#ID 金額円」
prepBagId != null   → 準備バッグから検索 → 「準備#ID 金額円」
いずれもnull        → 「-」を表示（手動入出金）
```

## 4. アクションバー（TransactionList.tsx）

ボタン一覧と遷移先:

| ボタン | 表示するコンポーネント |
|--------|----------------------|
| 小口登録 | 入出金フォーム（TransactionList内） |
| 業者登録 | （未実装 or 別コンポーネント） |
| 有高チェック | DenominationCheckPage.tsx |
| 金種表一覧 | 金種チェック履歴テーブル |
| バッグ管理 | BagList.tsx + CashBagList.tsx |

## 5. データ更新フロー

操作（入金、バッグ作成、チェック保存等）の後:

```
onUpdate() 呼び出し
  → loadData(selectedSafeId)  // 取引・バッグ等を再取得
  → loadSafes()              // 残高を再計算して表示更新
```

残高はSafeエンティティのTransactionから算出するため、`loadSafes()`を再実行しないと残高が更新されない。

## 6. データ分離の保証

```
フロントエンド: safeId をクエリパラメータとして送信
バックエンド:   SafeId 外部キー + WHERE句でフィルタ
DB:            各テーブルに safe_id カラム（NOT NULL + FK制約）
```

金庫間のデータ混在はDBレベルの外部キー制約で防止される。
