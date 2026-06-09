# 業者管理画面 — 表示データの出どころ

画面に表示される各要素が、どのソースファイル・DB テーブルから来ているかを追う。

---

## データ取得の起点

タブを開いたとき・金庫を切り替えたときに1回のAPIリクエストで全データを取得する。

```
frontend/src/components/VendorTab.tsx
  useEffect(() => { loadData(); }, [safeId])
      ↓
  api.getVendorDashboard(safeId)          // frontend/src/api/client.ts
      ↓
GET /api/vendor-dashboard?safeId=X
      ↓
backend/PettyCash.Api/Controllers/DashboardController.cs
  GetVendorDashboard([FromQuery] int safeId)
      ↓
backend/PettyCash.Application/UseCases/Dashboard/GetVendorDashboardUseCase.cs
  ExecuteAsync(safeId)
```

UseCase が並行して5種類のデータを取得し、`VendorDashboardDto` にまとめて返す。

```
VendorDashboardDto {
    Safe              ← 金庫情報 + 残高
    Bags              ← 釣り銭バッグ一覧
    CashBags          ← キャッシュバッグ一覧
    Transactions      ← 業者出納帳
    DenominationChecks← 有高チェック履歴
    PrepBags          ← 準備バッグ一覧
}
// backend/PettyCash.Application/Dtos/VendorDashboardDto.cs
```

フロントエンドは受け取ったデータを state に格納する。

```
VendorTab.tsx  loadData()
  setBags(dashboard.bags)
  setCashBags(dashboard.cashBags)
  setVendorTransactions(dashboard.transactions)
  setDenomChecks(dashboard.denominationChecks)
  setPrepBags(dashboard.prepBags)
  onSafeUpdate(dashboard.safe)   ← App.tsx のヘッダー残高を更新
```

---

## ヘッダー（金庫名・残高バッジ）

```
表示: 名古屋  業者: 261,000円  小口: 2,600円
```

| 表示 | フロント | バックエンド | DB |
|-----|---------|------------|-----|
| 金庫名「名古屋」 | `App.tsx` の `selectedSafe.name` | SafeDto.Name | `safes.name` |
| 業者: 261,000円 | `App.tsx` の `selectedSafe.vendorBalance` | SafeDto.VendorBalance | `safe_balances.vendor_balance` |
| 小口: 2,600円 | `App.tsx` の `selectedSafe.pettyCashBalance` | SafeDto.PettyCashBalance | `safe_balances.petty_cash_balance` |

**取得経路（残高）:**

```
SafeRepository.GetByIdAsync()
    └── context.Safes.FirstOrDefaultAsync()        → SELECT FROM safes
    └── LoadBalances(safe)
          └── context.SafeBalances.FirstOrDefaultAsync()
                → SELECT FROM safe_balances WHERE safe_id = X
          └── safe.SetBalances(vendorBalance, pettyCashBalance)
// backend/PettyCash.Infrastructure/Repositories/SafeRepository.cs
```

---

## 釣り銭バッグ一覧

```
表示: CA-001  200,000円  (備考なし)  金庫内  2026/6/3 9:00:00  -  [移動][有高]
```

| 表示 | フロント | バックエンド | DB |
|-----|---------|------------|-----|
| CA-001 | `"CA-" + bag.id.padStart(3,"0")` | ChangeBagDto.Id | `change_bags.id` |
| 200,000円 | `bag.totalAmount` | ChangeBagDto.TotalAmount | `change_bags.total_amount` |
| 備考 | `bag.description` | ChangeBagDto.Description | `change_bags.description` |
| 金庫内 / レジへ移動済 | `bag.status` | ChangeBagDto.Status | `change_bags.status` |
| 入金日時 | `bag.createdAt` | ChangeBagDto.CreatedAt | `change_bags.created_at` |
| 移動日時 | `bag.movedAt` | ChangeBagDto.MovedAt | `change_bags.moved_at` |

**取得経路:**

```
GetBagsUseCase.ExecuteAsync(safeId)
    └── ChangeBagRepository.GetBySafeIdAsync(safeId)
          └── context.ChangeBags.Where(b => b.SafeId == safeId)
                → SELECT FROM change_bags WHERE safe_id = X
// backend/PettyCash.Infrastructure/Repositories/ChangeBagRepository.cs
```

**期間フィルタ（フロントエンド側）:**

```
VendorTab.tsx
  const filteredBags = bags.filter(b => inRange(b.createdAt))
  // currentRange = getMonthRange(monthOffset) で計算した from〜to の範囲
  // BagList に filteredBags を渡す
```

---

## キャッシュバッグ一覧

```
表示: BAG-XXX  金額  備考  入金日時  操作
```

| 表示 | フロント | DB |
|-----|---------|-----|
| BAG-XXX | `"BAG-" + bag.id.padStart(3,"0")` | `cash_bags.id` |
| 金額 | `bag.totalAmount` | `cash_bags.total_amount` |
| 備考 | `bag.description` | `cash_bags.description` |
| 入金日時 | `bag.createdAt` | `cash_bags.created_at` |

**取得経路:**

```
GetCashBagsUseCase.ExecuteAsync(safeId)
    └── CashBagRepository.GetBySafeIdAsync(safeId)
          → SELECT FROM cash_bags WHERE safe_id = X
// backend/PettyCash.Infrastructure/Repositories/CashBagRepository.cs
```

---

## 準備バッグ一覧

```
表示: 1  61,000円  #1  [有高][戻す]  2026/6/5 9:13:01
```

| 表示 | フロント | DB |
|-----|---------|-----|
| 番号 | `prepBag.id` | `prep_bags.id` |
| 合計金額 | `prepBag.totalAmount` | `prep_bags.total_amount` |
| 含むバッグ | `prepBag.cashBagIds.map(id => "#"+id).join(", ")` | `prep_bags` の cashBagIds（JSON配列） |
| 作成日時 | `prepBag.createdAt` | `prep_bags.created_at` |

**取得経路:**

```
GetPrepBagsUseCase.ExecuteAsync(safeId)
    └── PrepBagRepository.GetBySafeIdAsync(safeId)
          → SELECT FROM prep_bags WHERE safe_id = X
// backend/PettyCash.Infrastructure/Repositories/PrepBagRepository.cs
```

---

## 業者出納帳

```
表示: 番号  種別  金額  残高  摘要  バッグ  日時
```

| 表示 | フロント | DB |
|-----|---------|-----|
| 番号 | `tx.sequenceNumber` | `vendor_ledger_view.sequence_number` |
| 種別（入金/出金/調整） | `tx.type` を日本語変換 | `vendor_ledger_view.type` |
| 金額 | `tx.amount` | `vendor_ledger_view.amount` |
| 残高 | `tx.balance` | `vendor_ledger_view.balance` |
| 摘要 | `tx.description` | `vendor_ledger_view.description` |
| バッグ | `tx.changeBagId` / `cashBagId` / `prepBagId` で名称変換 | `vendor_ledger_view.change_bag_id` 等 |
| 日時 | `tx.createdAt` | `vendor_ledger_view.created_at` |

**取得経路:**

```
IVendorLedgerQueryService.GetBySafeIdAsync(safeId)
    └── VendorLedgerQueryService.GetBySafeIdAsync()
          └── context.VendorLedgerEntries
                .Where(e => e.SafeId == safeId)
                .OrderByDescending(e => e.CreatedAt)
                → SELECT FROM vendor_ledger_view WHERE safe_id = X
// backend/PettyCash.Infrastructure/Queries/VendorLedgerQueryService.cs
```

**期間フィルタ（フロントエンド側）:**

```
VendorTab.tsx
  // APIは全件返す。期間絞り込みはフロントエンドで行う。
  const rows = [
    ...transactions.filter(t => inRange(t.createdAt)),
    ...denomChecks.filter(c => inRange(c.createdAt)),
  ].sort((a, b) => a.data.sequenceNumber - b.data.sequenceNumber)
```

> 出納帳には有高チェック行（DenominationCheck）も混在して表示される。

---

## 有高チェック行（出納帳内）

出納帳テーブルに薄青背景で差し込まれる行。

**取得経路:**

```
GetVendorDenominationChecksUseCase.ExecuteAsync(safeId)
    └── VendorDenominationCheckRepository.GetBySafeIdAsync(safeId)
          → SELECT FROM vendor_denomination_checks WHERE safe_id = X
// backend/PettyCash.Infrastructure/Repositories/VendorDenominationCheckRepository.cs
```

---

## ファイルの場所まとめ

| レイヤー | ファイル |
|---------|---------|
| 画面コンポーネント | `frontend/src/components/VendorTab.tsx` |
| バッグ一覧コンポーネント | `frontend/src/components/BagList.tsx` |
| キャッシュ/準備バッグコンポーネント | `frontend/src/components/CashBagList.tsx` |
| APIクライアント | `frontend/src/api/client.ts` |
| コントローラー | `backend/PettyCash.Api/Controllers/DashboardController.cs` |
| UseCase（データ収集） | `backend/PettyCash.Application/UseCases/Dashboard/GetVendorDashboardUseCase.cs` |
| DTO定義 | `backend/PettyCash.Application/Dtos/VendorDashboardDto.cs` |
| 残高取得 | `backend/PettyCash.Infrastructure/Repositories/SafeRepository.cs` |
| 出納帳取得 | `backend/PettyCash.Infrastructure/Queries/VendorLedgerQueryService.cs` |
| バッグ取得 | `backend/PettyCash.Infrastructure/Repositories/ChangeBagRepository.cs` |
| キャッシュバッグ取得 | `backend/PettyCash.Infrastructure/Repositories/CashBagRepository.cs` |
| 準備バッグ取得 | `backend/PettyCash.Infrastructure/Repositories/PrepBagRepository.cs` |
