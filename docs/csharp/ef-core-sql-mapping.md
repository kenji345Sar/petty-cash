# EF Core の文法と SQL の対応

EF Core の構文が「何の SQL に相当するか」を対応させた早見表。
Repository のコードを読むときに参照する。

## DbSet とは

```csharp
public DbSet<PettyCashTransaction> PettyCashTransactions => Set<PettyCashTransaction>();
```

`DbSet<T>` = **テーブルそのもの**を表すオブジェクト。
`context.PettyCashTransactions` と書くと `petty_cash_transactions` テーブルを指している。

---

## 読み取り

### 1件取得（見つからなければ null）

```csharp
// EF Core
context.PettyCashTransactions.FirstOrDefaultAsync(t => t.Id == id)
```

```sql
-- SQL
SELECT * FROM petty_cash_transactions WHERE id = @id LIMIT 1
```

### 複数件取得

```csharp
// EF Core
context.PettyCashTransactions
    .Where(t => t.SafeId == safeId)
    .OrderByDescending(t => t.CreatedAt)
    .ToListAsync()
```

```sql
-- SQL
SELECT * FROM petty_cash_transactions
WHERE safe_id = @safeId
ORDER BY created_at DESC
```

`t => t.SafeId == safeId` はラムダ式（TypeScript の `t => t.safeId === safeId` と同じ書き方）。
EF Core がこれを SQL の `WHERE` 句に変換する。

---

## 書き込み

### INSERT の予約

```csharp
// EF Core（この時点ではまだ SQL は実行されない）
context.PettyCashTransactions.Add(transaction);
```

```sql
-- SaveChangesAsync() 後に実行される SQL
INSERT INTO petty_cash_transactions (safe_id, type, amount, ...) VALUES (...)
```

`Add()` は予約だけで、`SaveChangesAsync()` を呼んで初めて DB に送られる。

### UPDATE

```csharp
// EF Core（EF Core が変更を自動検知する）
safe.UpdateName("新しい名前");  // Domainのメソッドでプロパティを変更
// context.SaveChangesAsync() で UPDATE が実行される
```

```sql
UPDATE safes SET name = '新しい名前' WHERE id = @id
```

---

## UnitOfWork（SaveChangesAsync）の正体

```csharp
// PettyCashDbContext.cs
async Task IUnitOfWork.SaveChangesAsync()
{
    await base.SaveChangesAsync();
}
```

UseCase の最後で呼ぶ `unitOfWork.SaveChangesAsync()` の実体はこれ。
DbContext に貯まった `Add()` / 変更を **1トランザクションで DB に送る**。

```
Add(A)  ─┐
Add(B)  ─┤  SaveChangesAsync() → BEGIN; INSERT A; INSERT B; COMMIT;
Update(C) ┘
```

---

## 対応表まとめ

| EF Core | SQL 相当 |
|---|---|
| `context.テーブル名` | テーブル |
| `.Where(t => t.X == y)` | `WHERE x = y` |
| `.OrderByDescending(t => t.X)` | `ORDER BY x DESC` |
| `.OrderBy(t => t.X)` | `ORDER BY x ASC` |
| `.FirstOrDefaultAsync(...)` | `SELECT ... LIMIT 1`（null あり） |
| `.FirstAsync(...)` | `SELECT ... LIMIT 1`（null なら例外） |
| `.ToListAsync()` | `SELECT ...`（リスト） |
| `.Add(entity)` | INSERT の予約 |
| プロパティ変更 | UPDATE の予約 |
| `SaveChangesAsync()` | 予約した INSERT/UPDATE を1トランザクションで実行 |
