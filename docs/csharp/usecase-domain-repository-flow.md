# UseCase・Domain・Repository の流れ

赤伝（小口）を具体例に、3つの層が何をしているかを追う。

---

## 全体像

```
[ブラウザ] 赤伝ボタンを押す
    ↓ POST /api/petty-cash-transactions/{id}/reverse
[Controller]  リクエストを受け取り UseCase を呼ぶだけ
    ↓
[UseCase]     手順を書く（何をどの順番でやるか）
    ↓ ↑
[Domain]      業務判断をする（赤伝をどう作るか、出金できるか）
    ↓ ↑
[Repository]  DB を読み書きする
    ↓
[DB]          PostgreSQL
```

---

## 各層のコードを追う

### Controller（薄い）

```csharp
[HttpPost("{id}/reverse")]
public async Task<ActionResult<PettyCashTransactionDto>> Reverse(int id, ReverseTransactionRequestDto dto)
{
    var transaction = await reversePettyCashTransactionUseCase.ExecuteAsync(id, dto);
    return Ok(transaction);
}
```

Controller がやること：**UseCase を呼んで結果を返す**だけ。判断も処理もしない。

---

### UseCase（手順を書く）

```csharp
public async Task<PettyCashTransactionDto> ExecuteAsync(int originalId, ReverseTransactionRequestDto dto)
{
    // 1. Repository: 元の取引を DB から読む
    var original = await transactionRepository.GetByIdAsync(originalId)
        ?? throw new KeyNotFoundException(...);

    // 2. Domain: 赤伝取引をどう作るか（業務判断）
    var reversal = PettyCashTransaction.CreateReversal(original, desc, DateTime.UtcNow);

    // 3. Repository: 出金になる場合は金庫を読んで残高確認
    if (reversal.Type == TransactionType.Withdrawal)
    {
        var safe = await safeRepository.GetByIdAsync(original.SafeId);
        safe.EnsureCanWithdraw(reversal.Amount);  // Domain: 出金可能か判断
    }

    // 4. 採番・残高計算・保存（後述の付加機能を含む）
    await sequenceNumberService.AssignAsync(reversal);
    await balanceService.AssignBalanceAsync(reversal);
    await transactionRepository.AddAsync(reversal);   // Repository: 赤伝を保存
    await unitOfWork.SaveChangesAsync();              // ここで初めて DB に書き込む

    return new PettyCashTransactionDto(...);
}
```

UseCase がやること：**何をどの順番でやるか**を書く。判断は Domain に任せる。

---

### Domain（業務判断する）

```csharp
// PettyCashTransaction.cs
public static PettyCashTransaction CreateReversal(PettyCashTransaction original, string description, DateTime date)
{
    // 「入金なら出金で打ち消す」という業務ルールがここにある
    var (reverseType, reverseAmount) = original.Type switch
    {
        TransactionType.Deposit    => (TransactionType.Withdrawal, original.Amount),
        TransactionType.Withdrawal => (TransactionType.Deposit,    original.Amount),
        TransactionType.Adjustment when original.Amount >= 0 => (TransactionType.Withdrawal, original.Amount),
        _                                                     => (TransactionType.Deposit,    Math.Abs(original.Amount)),
    };

    return new PettyCashTransaction
    {
        SafeId = original.SafeId,
        Type = reverseType,
        Amount = reverseAmount,
        Description = description,
        CreatedAt = date
    };
}
```

Domain がやること：**業務の判断**。UseCase は「作って」と依頼するだけで、どう作るかは知らない。

```csharp
// Safe.cs（別のドメインクラス）
public void EnsureCanWithdraw(int amount)
{
    if (amount > CurrentBalance)
        throw new InvalidOperationException(
            $"残高不足です。現在残高: {CurrentBalance}円、出金額: {amount}円");
}
```

---

### Repository（DB を読み書きする）

```csharp
// GetByIdAsync: DB から1件読む
public async Task<PettyCashTransaction?> GetByIdAsync(int id)
    => await context.PettyCashTransactions.FirstOrDefaultAsync(t => t.Id == id);
    // SQL: SELECT * FROM petty_cash_transactions WHERE id = @id LIMIT 1

// AddAsync: DB に書く予約（SaveChangesAsync で実行）
public Task AddAsync(PettyCashTransaction transaction)
{
    context.PettyCashTransactions.Add(transaction);
    // SQL: INSERT INTO petty_cash_transactions (...) VALUES (...)
    return Task.CompletedTask;
}
```

Repository がやること：**SQL を実行する**。業務の判断はしない。

---

## 付加機能（核の流れには影響しない）

UseCase の後半にある3つは、採番・残高計算・コミットの仕組み。
核の流れ（UseCase→Domain→Repository）とは独立しているので、別で理解する。

### SequenceNumberService（採番）

```csharp
await sequenceNumberService.AssignAsync(reversal);
```

やること：出納帳の連番（1, 2, 3...）を取得して取引にセットする。

```sql
-- SQL の中身
SELECT MAX(sequence_number) FROM petty_cash_transactions WHERE safe_id = ? + 1
```

### BalanceService（残高計算）

```csharp
await balanceService.AssignBalanceAsync(reversal);
```

やること：直前の取引行の残高を読み、今回の増減を足した新しい残高を取引にセットする。

```sql
-- SQL の中身
SELECT balance FROM petty_cash_transactions
WHERE safe_id = ? ORDER BY created_at DESC, id DESC LIMIT 1
```

残高は取引行の `balance` 列に保存される。画面の残高表示もこの値を読む。

### unitOfWork.SaveChangesAsync（一括コミット）

```csharp
await unitOfWork.SaveChangesAsync();
```

やること：それまでの `Add()` や変更を **1トランザクションで DB に送る**。

```
AddAsync(reversal)  → BEGIN; INSERT INTO petty_cash_transactions ...; COMMIT;
```

この1行より前は「予約」、この1行で初めて DB に書き込まれる。

---

## まとめ：各層の責任

| 層 | 責任 | 判断するか |
|---|---|---|
| Controller | リクエストを受けて UseCase を呼ぶ | しない |
| UseCase | 手順を書く（何をどの順番で） | しない |
| Domain | 業務の判断（赤伝の作り方、残高チェック） | **する** |
| Repository | SQL を実行する（読む・書く） | しない |

イベントソーシング（ES+CQRS）を入れると、UseCase の後半にイベント記録と Read Model 更新が加わる。詳しくは [event-sourcing/changes-from-current.md](../event-sourcing/changes-from-current.md) を参照。
