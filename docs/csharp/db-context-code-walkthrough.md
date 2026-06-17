# DbContext — 実際のコードを追う

`context.Safes.FirstOrDefaultAsync(s => s.Id == id)` が動くまでの4ステップを、実際のファイルで追う。

---

## ステップ1: 接続文字列の定義

```json
// backend/PettyCash.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=petty_cash;Username=postgres;Password=postgres"
  }
}
```

`"DefaultConnection"` というキー名で接続先を定義している。Docker 環境では `Host=db` に変わる。

---

## ステップ2: DIコンテナへの登録

```csharp
// backend/PettyCash.Api/Program.cs (27行目)
builder.Services.AddDbContext<PettyCashDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
//                   ↑ステップ1の "DefaultConnection" を読む
```

アプリ起動時に1回だけ実行される。これにより：
- 「`PettyCashDbContext` が必要なクラスには自動で渡す」とDIコンテナが記憶する
- PostgreSQL ドライバ（`UseNpgsql`）を使うことを設定する

同じファイルで Repository とインターフェースの紐づけも行われている：

```csharp
// backend/PettyCash.Api/Program.cs (31行目)
builder.Services.AddScoped<ISafeRepository, SafeRepository>();
// 「ISafeRepository が要求されたら SafeRepository を渡す」
```

---

## ステップ3: PettyCashDbContext — テーブルの窓口を定義

```csharp
// backend/PettyCash.Infrastructure/Data/PettyCashDbContext.cs (13行目)
public class PettyCashDbContext(DbContextOptions<PettyCashDbContext> options)
    : DbContext(options), IUnitOfWork
```

ステップ2で渡した `options`（接続先情報）を受け取って `DbContext` に渡している。

### テーブルの窓口（DbSet）

```csharp
// PettyCashDbContext.cs (20〜27行目)
public DbSet<Safe> Safes => Set<Safe>();
public DbSet<ChangeBag> ChangeBags => Set<ChangeBag>();
public DbSet<CashBag> CashBags => Set<CashBag>();
public DbSet<VendorTransaction> VendorTransactions => Set<VendorTransaction>();
// ...
```

`context.Safes` と書いたとき、ここの `Safes` プロパティが返される。

### C#クラスと列名の対応（OnModelCreating）

```csharp
// PettyCashDbContext.cs (37〜48行目)
modelBuilder.Entity<Safe>(entity =>
{
    entity.ToTable("safes");                              // → safes テーブル
    entity.HasKey(e => e.Id);                             // → PRIMARY KEY
    entity.Property(e => e.Id).HasColumnName("id");       // Id プロパティ → id 列
    entity.Property(e => e.Name).HasColumnName("name");   // Name → name 列
    entity.Ignore(e => e.VendorBalance);                  // DBに存在しないので除外
});
```

C# のプロパティ名（PascalCase）と DB 列名（snake_case）が違うため、ここで対応を宣言している。

---

## ステップ4: Repository — context を受け取って使う

```csharp
// backend/PettyCash.Infrastructure/Repositories/SafeRepository.cs (10行目)
public class SafeRepository(PettyCashDbContext context) : ISafeRepository
```

コンストラクタに `PettyCashDbContext context` と書くだけで、DIコンテナがステップ2で登録した context を渡してくれる。

```csharp
// SafeRepository.cs (12〜19行目)
public async Task<Safe?> GetByIdAsync(int id)
{
    var safe = await context.Safes               // ← ステップ3の Safes プロパティ
        .FirstOrDefaultAsync(s => s.Id == id);   // → SELECT * FROM safes WHERE id = @id LIMIT 1
    if (safe != null)
        await LoadBalances(safe);                 // 残高を safe_balances から追加取得
    return safe;
}
```

---

## 4ステップのつながり

```
appsettings.Development.json
  "DefaultConnection": "Host=localhost;Port=5432;Database=petty_cash;..."
          ↓ GetConnectionString("DefaultConnection") で読む
Program.cs
  AddDbContext<PettyCashDbContext>(options => options.UseNpgsql(...))
          ↓ DIコンテナが context を作る
PettyCashDbContext
  DbSet<Safe> Safes => Set<Safe>()        テーブルの窓口
  OnModelCreating: Safe → safes テーブル  列名の対応
          ↓ コンストラクタ注入
SafeRepository(PettyCashDbContext context)
  context.Safes.FirstOrDefaultAsync(...)
          ↓ EF Core が SQL に変換
PostgreSQL
  SELECT * FROM safes WHERE id = @id LIMIT 1
```

---

## アプリ起動時に何が起きるか（Program.cs の後半）

起動時に `EnsureCreated()` と手動 SQL マイグレーションも実行される。

```csharp
// Program.cs (94〜97行目)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PettyCashDbContext>();
    db.Database.EnsureCreated();
    // ↑ DbContext の OnModelCreating を元にテーブルが存在しない場合は作成する
```

その後、テーブルの存在確認 → なければ `CREATE TABLE` → データの追加、という初期化処理が続く。これがこのプロジェクトでのマイグレーション方法（`dotnet ef migrations` は使っていない）。
