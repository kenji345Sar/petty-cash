# DbContext — DB接続の仕組み

`context` の正体と、どうやって PostgreSQL に繋がっているかを追う。

---

## 全体の流れ

```
appsettings.json          接続文字列（ホスト・DB名・パスワード）
    ↓
Program.cs                AddDbContext で接続文字列を渡してDIに登録
    ↓
PettyCashDbContext        DbContext を継承したクラス（テーブル定義を持つ）
    ↓
SafeRepository 等         コンストラクタ注入で context を受け取り使う
```

---

## ① 接続文字列 — appsettings.json

```json
// backend/PettyCash.Api/appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=petty_cash;Username=postgres;Password=postgres"
  }
}
```

| 項目 | 値 |
|-----|----|
| `Host` | DBサーバのホスト名 |
| `Port` | PostgreSQL のポート（デフォルト 5432） |
| `Database` | データベース名 |
| `Username` / `Password` | 認証情報 |

Docker 環境ではホスト名が `localhost` ではなく `db`（docker-compose のサービス名）に変わる。

---

## ② DIへの登録 — Program.cs

```csharp
// backend/PettyCash.Api/Program.cs
builder.Services.AddDbContext<PettyCashDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

| コード | 意味 |
|-------|------|
| `AddDbContext<PettyCashDbContext>` | `PettyCashDbContext` をDIコンテナに登録する |
| `UseNpgsql(...)` | PostgreSQL ドライバを使うよう指定（MySQL なら `UseMySql`） |
| `GetConnectionString("DefaultConnection")` | `appsettings.json` の接続文字列を読む |

これにより「`PettyCashDbContext` が必要なクラスにはDIコンテナが自動で渡す」という状態になる。

---

## ③ PettyCashDbContext — テーブル定義の場所

```csharp
// backend/PettyCash.Infrastructure/Data/PettyCashDbContext.cs
public class PettyCashDbContext(DbContextOptions<PettyCashDbContext> options)
    : DbContext(options), IUnitOfWork
```

`DbContext` を継承したクラス。2つの役割を持つ。

### 役割1: テーブルの窓口（DbSet）

```csharp
public DbSet<Safe> Safes => Set<Safe>();
public DbSet<ChangeBag> ChangeBags => Set<ChangeBag>();
public DbSet<CashBag> CashBags => Set<CashBag>();
// ... 全テーブル分
```

`DbSet<T>` が「そのテーブル全体を表すオブジェクト」になる。

### 役割2: C#クラスとテーブル列の対応定義（OnModelCreating）

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Safe>(entity =>
    {
        entity.ToTable("safes");                          // テーブル名
        entity.HasKey(e => e.Id);                         // 主キー
        entity.Property(e => e.Name).HasColumnName("name"); // 列名の対応
        entity.Ignore(e => e.VendorBalance);              // DBに存在しないプロパティを除外
    });
```

C# のプロパティ名（`Name`）と DB の列名（`name`）が違う場合にここで対応付ける。

---

## ④ Repository での受け取り方

```csharp
// backend/PettyCash.Infrastructure/Repositories/SafeRepository.cs
public class SafeRepository(PettyCashDbContext context) : ISafeRepository
//                          ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//                          DIコンテナから自動的に注入される
```

`PettyCashDbContext context` と書くだけで、DIコンテナがアプリ起動時に登録した接続済みの context を渡してくれる。

---

## リクエスト1回ごとに context は作り直される

`AddDbContext` はデフォルトで **Scoped**（リクエスト単位）で登録される。

```
HTTPリクエスト開始
    → PettyCashDbContext が新規作成（DB接続確立）
    → Repository / UseCase が context を共有して使う
    → HTTPレスポンス返却
    → PettyCashDbContext が破棄（DB接続を解放）
```

1リクエスト内では同じ context を使いまわすため、複数のリポジトリが同じトランザクションに参加できる。

---

## PHP との対応

| C# | PHP（Laravel） |
|----|--------------|
| `appsettings.json` の ConnectionStrings | `.env` の `DB_HOST` / `DB_DATABASE` 等 |
| `AddDbContext<PettyCashDbContext>` | `config/database.php` + サービスプロバイダ登録 |
| `DbContext` | `Eloquent\Model` / `DB` ファサード |
| `DbSet<Safe>` | `Safe::query()` / `DB::table('safes')` |
| `OnModelCreating` | `$table->string('name')` （マイグレーション側） |

---

## このプロジェクトのテーブル一覧

| DbSet プロパティ | テーブル名 | 分類 |
|----------------|----------|------|
| `Safes` | `safes` | 金庫 |
| `ChangeBags` | `change_bags` | 釣り銭バッグ |
| `CashBags` | `cash_bags` | キャッシュバッグ |
| `PrepBags` | `prep_bags` | 準備バッグ |
| `VendorTransactions` | `vendor_transactions` | 業者取引 |
| `PettyCashTransactions` | `petty_cash_transactions` | 小口取引 |
| `VendorDenominationChecks` | `vendor_denomination_checks` | 業者有高チェック |
| `PettyCashDenominationChecks` | `safe_denomination_checks` | 小口有高チェック |
| `SafeBalances` | `safe_balances` | 残高（Read Model） |
| `VendorLedgerEntries` | `vendor_ledger_view` | 業者出納帳（Read Model） |
| `PettyCashLedgerEntries` | `petty_cash_ledger_view` | 小口出納帳（Read Model） |
| `DomainEvents` | `domain_events` | イベントストア |
