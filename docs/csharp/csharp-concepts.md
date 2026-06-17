# C# 基本概念メモ

このプロジェクトのコードを読む際に出てくる C# 固有の概念をまとめる。

---

## `[FromQuery]` — アトリビュート（属性）

```csharp
// backend/PettyCash.Api/Controllers/DashboardController.cs
public async Task<VendorDashboardDto> GetVendorDashboard([FromQuery] int safeId)
```

`[FromQuery]` は**型定義ではない**。メタ情報（アノテーション）をコードに付加する「アトリビュート」という仕組み。

### 意味

`safeId` の値を **URLのクエリ文字列**から取得することを ASP.NET Core フレームワークに伝えている。

```
GET /api/vendor-dashboard?safeId=1
                           ^^^^^^^^ ここから取得
```

### なくても動くか

コントローラーのパラメータが1つの場合、`[FromQuery]` を省略しても ASP.NET Core が自動推論するため**動作する**。
複数のパラメータがある場合や明示したい場合に書く。

### PHP との対応

PHP 8.x のアトリビュートと同じ概念：

```php
// PHP 8.x
#[Route('/vendor-dashboard', methods: ['GET'])]
public function getVendorDashboard(#[MapQueryParameter] int $safeId): Response
```

Symfony の `#[MapQueryParameter]`、Laravel の `Request::query()` に相当する処理を、アノテーションで宣言的に書いている。

### このプロジェクトの他のアトリビュート例

| アトリビュート | 意味 |
|-------------|------|
| `[HttpGet]` | GETリクエストに対応するメソッドだと宣言 |
| `[HttpPost]` | POSTリクエストに対応 |
| `[FromQuery]` | クエリ文字列からバインド |
| `[FromBody]` | リクエストボディ（JSON）からバインド |

---

## `Task<T>` — 非同期の戻り値型

```csharp
// backend/PettyCash.Application/UseCases/Dashboard/GetVendorDashboardUseCase.cs
public async Task<VendorDashboardDto> ExecuteAsync(int safeId)
```

`Task<VendorDashboardDto>` は**型定義**。C# における非同期処理の戻り値の型。

### TypeScript との対応

```typescript
// TypeScript
async function executeAsync(safeId: number): Promise<VendorDashboardDto>

// C#
async Task<VendorDashboardDto> ExecuteAsync(int safeId)
```

`Task<T>` は TypeScript の `Promise<T>` と同じ概念。

| C# | TypeScript |
|----|-----------|
| `Task<T>` | `Promise<T>` |
| `Task`（戻り値なし）| `Promise<void>` |
| `async` キーワード | `async` キーワード |
| `await` | `await` |

### 読み方

```csharp
public async Task<VendorDashboardDto> ExecuteAsync(int safeId)
//     ^^^^^  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//     非同期   「VendorDashboardDto を返す Promise」
```

`async` がついているメソッドは内部で `await` が使える。`await` で待った結果が `Task<T>` の `T` 部分に入る。

---

## 依存性の逆転（DIP）— インターフェースによる注入

```csharp
// backend/PettyCash.Application/UseCases/Dashboard/GetVendorDashboardUseCase.cs
public class GetVendorDashboardUseCase(
    ISafeRepository safeRepository,   // ← インターフェース（抽象）に依存
    ...
)
```

### どこで逆転しているか

```
GetVendorDashboardUseCase
    ↓ 依存するのは
ISafeRepository          ← インターフェース（抽象）
    ↑ 実装するのは
SafeRepository           ← 具体クラス（DBアクセスの実装）
```

UseCase は「DBが PostgreSQL だろうと何だろうと知らない、`ISafeRepository` が使えればいい」という状態。

### 注入は Program.cs で行われる

```csharp
// backend/PettyCash.Api/Program.cs
builder.Services.AddScoped<ISafeRepository, SafeRepository>();
//               ↑ 「ISafeRepository が要求されたら SafeRepository を渡す」
```

UseCase 側はこの登録を知らない。

### PHP との対応

```php
// インターフェース定義
interface SafeRepositoryInterface {
    public function findById(int $id): ?Safe;
}

// UseCase はインターフェースに依存
class GetVendorDashboardUseCase {
    public function __construct(
        private SafeRepositoryInterface $safeRepository
    ) {}
}

// DIコンテナで具体クラスを登録
// Symfony: services.yaml
// Laravel: AppServiceProvider
```

構造は同じ。Laravel の `bind()` / Symfony の `services.yaml` に相当するのが C# の `Program.cs`。

### このプロジェクトでのインターフェース一覧

| インターフェース | 具体クラス | 役割 |
|---------------|---------|------|
| `ISafeRepository` | `SafeRepository` | 金庫のDB操作 |
| `IVendorLedgerQueryService` | `VendorLedgerQueryService` | 業者出納帳の取得 |
