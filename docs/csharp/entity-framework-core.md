# Entity Framework Core — テーブル操作の仕組み

C# の ORM（Object-Relational Mapper）。コードを書くと自動で SQL に変換して実行する。

---

## 基本例：1件取得

```csharp
// backend/PettyCash.Infrastructure/Repositories/SafeRepository.cs
var safe = await context.Safes
    .FirstOrDefaultAsync(s => s.Id == id);
```

**発行される SQL（PostgreSQL）:**

```sql
SELECT * FROM safes WHERE id = @id LIMIT 1
```

### 各部分の意味

| コード | 意味 |
|-------|------|
| `context` | DBへの接続（`PettyCashDbContext`） |
| `.Safes` | `safes` テーブルに対応するプロパティ |
| `.FirstOrDefaultAsync(...)` | 条件に合う最初の1件を取得。なければ `null` を返す |
| `s => s.Id == id` | `WHERE id = @id` に変換されるラムダ式 |
| `await` | 非同期でDBの応答を待つ |

---

## `context.Safes` がテーブルと紐づく仕組み

`DbSet<T>` がテーブルの窓口になる。

```csharp
// backend/PettyCash.Infrastructure/Data/PettyCashDbContext.cs
public DbSet<Safe> Safes { get; set; }
//              ^^^^  ^^^^^^
//              モデル  テーブル名（safes）
```

`DbSet<Safe>` は「`safes` テーブル全体を表すオブジェクト」で、ここにメソッドを繋げて絞り込みや取得を行う。

---

## よく使うメソッド一覧

| メソッド | SQL相当 | 用途 |
|---------|--------|------|
| `.FirstOrDefaultAsync(条件)` | `WHERE ... LIMIT 1`（なければnull） | 1件取得 |
| `.Where(条件)` | `WHERE ...` | 絞り込み |
| `.OrderByDescending(キー)` | `ORDER BY ... DESC` | 降順ソート |
| `.ToListAsync()` | 全件取得してリストに変換 | 複数件取得 |
| `.Add(entity)` | `INSERT INTO ...` | 追加 |

### 複数件取得の例

```csharp
var safes = await context.Safes
    .OrderByDescending(s => s.CreatedAt)
    .ToListAsync();
// → SELECT * FROM safes ORDER BY created_at DESC
```

---

## PHP（Laravel Eloquent）との比較

| 処理 | C# Entity Framework | PHP Eloquent |
|-----|--------------------|--------------------|
| 1件取得 | `.FirstOrDefaultAsync(s => s.Id == id)` | `Safe::find($id)` |
| 条件で1件 | `.FirstOrDefaultAsync(s => s.Name == name)` | `Safe::where('name', $name)->first()` |
| 全件取得 | `.ToListAsync()` | `Safe::all()` |
| 追加 | `context.Safes.Add(safe)` | `$safe->save()` |

---

## このプロジェクトでの使われ方

| ファイル | 対応テーブル |
|---------|-----------|
| `SafeRepository.cs` | `safes`, `safe_balances` |
| `ChangeBagRepository.cs` | `change_bags` |
| `CashBagRepository.cs` | `cash_bags` |
| `PrepBagRepository.cs` | `prep_bags` |
| `VendorLedgerQueryService.cs` | `vendor_ledger_view`（Read Model） |

すべて `PettyCashDbContext` 経由でアクセスする。
