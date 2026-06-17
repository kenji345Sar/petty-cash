# ASP.NET Core ルーティング規則

URL とコントローラーメソッドの対応がどう決まるかを、PHP との比較で整理する。

---

## 基本の仕組み

アトリビュートをクラスとメソッドに付けることで URL を定義する。

```csharp
// backend/PettyCash.Api/Controllers/DashboardController.cs

[Route("api")]                       // ① クラスに付ける → URL の先頭部分
public class DashboardController

    [HttpGet("vendor-dashboard")]    // ② メソッドに付ける → HTTP メソッド + パス
    public async Task<...> GetVendorDashboard([FromQuery] int safeId)
```

**① + ② を組み合わせた結果:**

```
GET /api/vendor-dashboard?safeId=1
    ^^^^ ^^^^^^^^^^^^^^^^^ ^^^^^^^^
    │    │                 └ [FromQuery] int safeId が受け取る
    │    └ [Route("api")] + [HttpGet("vendor-dashboard")]
    └ GET リクエストであることを [HttpGet] が指定
```

---

## PHP との比較

### ルート定義の場所

| | C# ASP.NET Core | PHP Laravel | PHP Symfony |
|-|----------------|-------------|-------------|
| 定義場所 | コントローラーファイル内にアトリビュートで書く | `routes/api.php` に外部ファイルでまとめて書く | コントローラーファイル内にアトリビュートで書く |
| スタイル | コードとルートが同じファイル | ルートとコードが分離 | コードとルートが同じファイル |

### 書き方の比較

**GET リクエスト（一覧取得）**

```csharp
// C# ASP.NET Core
[Route("api")]
public class DashboardController

    [HttpGet("vendor-dashboard")]
    public async Task<VendorDashboardDto> GetVendorDashboard([FromQuery] int safeId)
    // → GET /api/vendor-dashboard?safeId=1
```

```php
// Laravel
Route::get('/api/vendor-dashboard', [DashboardController::class, 'getVendorDashboard']);

// Symfony
#[Route('/api/vendor-dashboard', methods: ['GET'])]
public function getVendorDashboard(#[MapQueryParameter] int $safeId): Response
```

**POST リクエスト（登録）**

```csharp
// C# ASP.NET Core
[HttpPost("bags")]
public async Task<IActionResult> DepositBag([FromBody] DepositBagRequest request)
// → POST /api/bags
```

```php
// Laravel
Route::post('/api/bags', [BagController::class, 'deposit']);

// Symfony
#[Route('/api/bags', methods: ['POST'])]
public function deposit(#[MapRequestPayload] DepositBagRequest $request): Response
```

**URLパラメーター（ID指定）**

```csharp
// C# ASP.NET Core
[HttpPost("bags/{id}/move")]
public async Task<IActionResult> MoveBag(int id)
// → POST /api/bags/1/move
```

```php
// Laravel
Route::post('/api/bags/{id}/move', [BagController::class, 'move']);

// Symfony
#[Route('/api/bags/{id}/move', methods: ['POST'])]
public function move(int $id): Response
```

---

## HTTP メソッドのアトリビュート一覧

| アトリビュート | HTTP メソッド | 用途 |
|-------------|-------------|------|
| `[HttpGet("path")]` | GET | データ取得 |
| `[HttpPost("path")]` | POST | 新規登録 |
| `[HttpPut("path")]` | PUT | 全体更新 |
| `[HttpPatch("path")]` | PATCH | 部分更新 |
| `[HttpDelete("path")]` | DELETE | 削除 |

---

## URL からコードを探す方法

Network タブで URL が分かったら `Ctrl+Shift+F` でキーワード検索する。

```
例: vendor-dashboard が見つかったら

Ctrl+Shift+F → "vendor-dashboard" と入力
→ DashboardController.cs の [HttpGet("vendor-dashboard")] がヒット
→ そのメソッドに飛ぶ
```

---

## このプロジェクトのルート一覧

| URL | メソッド | コントローラー |
|-----|---------|--------------|
| `GET /api/safes` | GetSafes | SafesController |
| `POST /api/safes` | CreateSafe | SafesController |
| `GET /api/vendor-dashboard` | GetVendorDashboard | DashboardController |
| `GET /api/pettycash-dashboard` | GetPettyCashDashboard | DashboardController |
| `POST /api/bags` | DepositBag | BagsController |
| `POST /api/bags/{id}/move` | MoveBagToRegister | BagsController |
| `POST /api/cashbags` | DepositCashBag | CashBagsController |
| `POST /api/prepbags` | CreatePrepBag | PrepBagsController |
| `POST /api/prepbags/{id}/handover` | HandOverPrepBag | PrepBagsController |
