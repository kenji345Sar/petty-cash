# フロントエンド API 呼び出しフロー詳細

## 概要

現在のフロントエンドは、アクション実行後にタブ内の全データを再取得する設計になっている。
ここでは「両替金バッグのレジへ移動」を例に、実際のHTTPリクエストの流れを解説する。

---

## 例：両替金バッグをレジへ移動する場合

### ユーザー操作
業者タブ → 両替金バッグ一覧 → 「レジへ移動」ボタンをクリック

### 発生するHTTPリクエスト（合計6回）

```
[ユーザーが「レジへ移動」をクリック]
       │
       ▼
  ① POST /api/bags/{id}/move          ← アクション実行（1回）
       │
       │  成功後、onUpdate() が呼ばれる
       │  → VendorTab.handleUpdate() → loadData() + loadSafes()
       ▼
  ② GET /api/bags?safeId=1            ← 両替金バッグ一覧の再取得
  ③ GET /api/cashbags?safeId=1        ← 売上バッグ一覧の再取得
  ④ GET /api/vendor-transactions?safeId=1  ← 業者出納帳の再取得
  ⑤ GET /api/denominationchecks/vendor?safeId=1  ← 有高チェック履歴の再取得
  ⑥ GET /api/prepbags?safeId=1        ← 準備バッグ一覧の再取得
       │
       │  さらに App.tsx の loadSafes() も実行
       ▼
  ⑦ GET /api/safes                    ← 金庫一覧（残高更新のため）
```

実際には **合計7回** のHTTPリクエストが発生する（②〜⑥は `Promise.all` で並列実行）。

### コードの流れ

**1. BagList.tsx — アクション実行**
```typescript
// BagList.tsx:57-64
const handleMove = async (id: number) => {
  if (!confirm("このバッグをレジへ移動しますか？")) return;
  await api.moveBagToRegister(id);  // ① POST /api/bags/{id}/move
  onUpdate();  // → VendorTab の handleUpdate() を呼ぶ
};
```

**2. VendorTab.tsx — タブ内データの全再取得**
```typescript
// VendorTab.tsx:75-88
const loadData = async () => {
  const [bagsData, cashBagsData, txData, checksData, prepBagsData] = await Promise.all([
    api.getBags(safeId),                    // ② GET /api/bags
    api.getCashBags(safeId),                // ③ GET /api/cashbags
    api.getVendorTransactions(safeId),      // ④ GET /api/vendor-transactions
    api.getVendorDenominationChecks(safeId),// ⑤ GET /api/denominationchecks/vendor
    api.getPrepBags(safeId),                // ⑥ GET /api/prepbags
  ]);
  // ... state更新
};

// VendorTab.tsx:92
const handleUpdate = () => { loadData(); onUpdate(); };
//                           ~~~~~~~~    ~~~~~~~~~~
//                           ②〜⑥        → App.tsx の loadSafes() → ⑦
```

**3. App.tsx — 金庫残高の更新**
```typescript
// App.tsx:15-17（VendorTab の onUpdate prop に渡されている）
const loadSafes = async () => {
  const data = await api.getSafes();  // ⑦ GET /api/safes
  setSafes(data);
};
```

**4. バックエンド — MoveBagToRegisterUseCase**
```csharp
// MoveBagToRegisterUseCase.cs
public async Task<VendorTransactionDto> ExecuteAsync(int bagId)
{
    var bag = await bagRepository.GetByIdAsync(bagId);  // DBからバッグ取得
    var transaction = bag.MoveToRegister(DateTime.UtcNow);  // ドメインロジック実行
    await sequenceNumberService.AssignAsync(transaction);    // 採番
    await bagRepository.UpdateAsync(bag);                    // 更新
    await unitOfWork.SaveChangesAsync();                     // DB保存
    return new VendorTransactionDto(...);  // レスポンス返却
}
```

---

## 他のアクションも同じパターン

| アクション | API呼び出し | 再取得 |
|-----------|-----------|--------|
| 両替金バッグ入金 | POST /api/bags/deposit | 5 + 1 = 6回 |
| レジへ移動 | POST /api/bags/{id}/move | 5 + 1 = 6回 |
| 売上バッグ入金 | POST /api/cashbags/deposit | 5 + 1 = 6回 |
| 準備バッグ作成 | POST /api/prepbags | 5 + 1 = 6回 |
| 準備バッグ引渡 | POST /api/prepbags/{id}/handover | 5 + 1 = 6回 |
| 有高チェック | POST /api/denominationchecks/... | 5 + 1 = 6回 |
| 小口取引登録 | POST /api/petty-cash-transactions | 2 + 1 = 3回 |

小口タブは取得データが少ない（取引 + 有高チェックの2つ）ため3回で済む。

---

## 現状の評価

**メリット:**
- 実装がシンプルで、データの整合性が常に保たれる
- `Promise.all` で並列実行しているため、体感速度への影響は小さい

**デメリット:**
- バッグ移動1つで7回のHTTPリクエストが発生する
- 変更のないデータ（例：有高チェック履歴）も毎回再取得している

**改善案（将来的に必要な場合）:**
1. バックエンドに「ダッシュボードAPI」を作り、1回のリクエストで全データを返す
2. アクションのレスポンスに更新後のデータを含め、差分更新する
3. WebSocket / Server-Sent Events でリアルタイム同期する

現時点ではデータ量が少なく、ローカルネットワーク内での利用が前提のため、パフォーマンス上の問題は発生していない。
