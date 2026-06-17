# 画面からコードを調査する手順

業者管理画面を例に、画面表示 → React → API → バックエンド → DB まで一本で追う。

---

## 全体マップ

```
【画面】localhost:5174
    ↓ ① DevTools Network タブ（今回の実際の手順）
    ↓    または 文字列検索 / React DevTools / App.tsx
【Reactコンポーネント】VendorTab.tsx
    ↓ ② useEffect → loadData() を探す
【APIクライアント】api/client.ts
    ↓ ③ URL のパス部分を Ctrl+Shift+F で検索
【コントローラー】DashboardController.cs
    ↓ ④ UseCase を呼んでいる行を見る
【UseCase】GetVendorDashboardUseCase.cs
    ↓ ⑤ Repository を呼んでいる行を見る
【Repository】SafeRepository.cs 等
    ↓ ⑥ context.テーブル名 を見る
【DB】PostgreSQL（safes, change_bags 等）
```

---

## ① 画面 → React コンポーネント

### 方法A: DevTools Network タブから入る（今回の実際の手順）

何もわからないときに一番確実な方法。API の URL を先に特定し、そこからコードを逆引きする。

```
1. F12 → Network タブを開く
2. Ctrl+R でリロード ← DevTools を開いたままリロードするのがポイント
3. Fetch/XHR フィルターをクリック
4. vendor-dashboard?safeId=1 が一覧に表示される
5. クリック → Headers タブ → Request URL を確認
   http://localhost:5141/api/vendor-dashboard?safeId=1
                        ^^^^^^^^^^^^^^^^^^^^^^^^^^
                        このパス部分がコードのキー

6. Ctrl+Shift+F → vendor-dashboard で検索
   → DashboardController.cs: [HttpGet("vendor-dashboard")] がヒット
```

**Network タブでよくあるトラブル:**

| 症状 | 原因 | 対処 |
|-----|------|------|
| 「0 / 78 requests」で一覧が空 | フィルター欄に文字が残っている | フィルター欄の `×` をクリックして解除 |
| リロードしても出てこない | DevTools を閉じた状態でリロードした | DevTools を開いたまま `Ctrl+R` |

### 方法B: 画面上の文字列で検索

```
Ctrl+Shift+F → 「業者管理」と入力
→ frontend/src/components/VendorTab.tsx がヒット
```

### 方法C: React Developer Tools（Chrome拡張）

F12 → `Components` タブ → 画面の要素をクリック → 右側にコンポーネント名が表示される。

### 方法D: App.tsx から辿る

```tsx
// frontend/src/App.tsx
{activeTab === "vendor" && <VendorTab safeId={...} />}
//                          ↑ 業者タブ = VendorTab.tsx
```

---

## ② React コンポーネント → API クライアント

`VendorTab.tsx` を開き、データ取得している箇所を探す。

```tsx
// frontend/src/components/VendorTab.tsx (78〜86行目)
const loadData = async () => {
    const dashboard = await api.getVendorDashboard(safeId);
    //                          ↑ api/client.ts の関数名
```

`api.getVendorDashboard` を `Ctrl+クリック` すると `client.ts` に飛ぶ。

---

## ③ API クライアント → コントローラー

`client.ts` で URL を確認する。

```typescript
// frontend/src/api/client.ts (254行目)
getVendorDashboard: (safeId: number) =>
    fetchJson<VendorDashboard>(`${API_BASE}/vendor-dashboard?safeId=${safeId}`)
//                               ^^^^^^^^  ^^^^^^^^^^^^^^^^^
//                               /api      /vendor-dashboard  ← このパスがキー
```

`API_BASE = "http://localhost:5141/api"` なので実際の URL は：

```
GET http://localhost:5141/api/vendor-dashboard?safeId=1
```

パス部分 `vendor-dashboard` を `Ctrl+Shift+F` で検索：

```
→ DashboardController.cs: [HttpGet("vendor-dashboard")]
```

### ASP.NET Core のルーティング規則

```csharp
[Route("api")]                    // /api
    [HttpGet("vendor-dashboard")] // /api/vendor-dashboard
    public async Task<...> GetVendorDashboard([FromQuery] int safeId)
//                                            ↑ ?safeId=1 を受け取る
```

---

## ④ コントローラー → UseCase

```csharp
// DashboardController.cs (16行目)
var dashboard = await getVendorDashboard.ExecuteAsync(safeId);
//                     ↑ GetVendorDashboardUseCase を Ctrl+クリック
```

---

## ⑤ UseCase → Repository

```csharp
// GetVendorDashboardUseCase.cs
var safe = await safeRepository.GetByIdAsync(safeId);
//               ↑ ISafeRepository → 実体は SafeRepository.cs
```

`ISafeRepository` を `Ctrl+クリック` → インターフェース定義へ。
`F12`（実装へジャンプ）→ `SafeRepository.cs` へ。

---

## ⑥ Repository → DB

```csharp
// SafeRepository.cs (14〜15行目)
var safe = await context.Safes
    .FirstOrDefaultAsync(s => s.Id == id);
//          ↑ Safes = safes テーブル（PettyCashDbContext で定義）
// → SELECT * FROM safes WHERE id = @id LIMIT 1
```

---

## ブラウザで実際に確認する方法

Network タブを使うとどの API が呼ばれているか実際に見える。

1. `F12` → `Network` タブを開く
2. `Ctrl+R` でリロード（DevTools を開いたままリロードするのがポイント）
3. `Fetch/XHR` フィルターをクリック
4. `vendor-dashboard?safeId=1` をクリック
5. `Response` タブで実際のJSONデータを確認

```
Response の中身:
{
  "safe": { "name": "名古屋", "vendorBalance": 261000 }  ← 画面の「業者: 261,000円」
  "bags": [{ "totalAmount": 200000, "status": "InSafe" }] ← 画面の「CA-001 200,000円 金庫内」
}
```

---

## 調査ツールまとめ

| やりたいこと | 手段 |
|------------|------|
| 画面の文字列 → コンポーネント | `Ctrl+Shift+F` で文字列検索 |
| コンポーネント名を直接確認 | React Developer Tools（Chrome拡張） |
| どの API を呼んでいるか | F12 → Network → Fetch/XHR |
| URL → コントローラー | URL のパスを `Ctrl+Shift+F` で検索 |
| 定義元に飛ぶ | `Ctrl+クリック` |
| インターフェースの実装に飛ぶ | `F12`（Go to Implementation） |
