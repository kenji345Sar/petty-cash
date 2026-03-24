# UnitOfWork パターン

## 概要

複数のリポジトリ操作を1つのDBトランザクションとしてまとめて保存する仕組み。
リポジトリはエンティティの追跡（Add/変更検知）のみを行い、`SaveChangesAsync` は呼ばない。
UseCaseの最後に `IUnitOfWork.SaveChangesAsync()` を1回だけ呼ぶことで、全操作が一括コミットされる。

## なぜ必要か

例えば有高チェックで差額がある場合、以下の3操作が必要：

1. 有高チェック結果を保存
2. 調整取引を作成
3. バッグの金額を更新

これらが個別に `SaveChanges` されると、2の途中で失敗した場合に1だけ保存されてデータ不整合になる。
UnitOfWorkなら全部成功 or 全部失敗（アトミック）になる。

## 使用例: CheckSafeUseCase

```csharp
public class CheckSafeUseCase(
    ISafeRepository safeRepository,
    IPettyCashDenominationCheckRepository checkRepository,
    IVendorTransactionRepository transactionRepository,
    ISequenceNumberService sequenceNumberService,
    IUnitOfWork unitOfWork)  // <-- UnitOfWorkを注入
{
    public async Task<DenominationCheckDto> ExecuteAsync(int safeId, DenominationCheckRequestDto dto)
    {
        var safe = await safeRepository.GetByIdAsync(safeId)
            ?? throw new KeyNotFoundException(...);

        var denomination = new Denomination(...);
        var check = PettyCashDenominationCheck.CreateForSafe(safeId, denomination, safe.CurrentBalance, now);

        // (1) チェック結果を追跡に追加（まだDBには保存されない）
        await sequenceNumberService.AssignAsync(check);
        await checkRepository.AddAsync(check);

        // (2) 差額があれば調整取引も追跡に追加（まだDBには保存されない）
        if (check.Difference != 0)
        {
            var adjustment = VendorTransaction.CreateSafeAdjustment(safeId, check.Difference, now);
            await sequenceNumberService.AssignAsync(adjustment);
            await transactionRepository.AddAsync(adjustment);
        }

        // (3) ここで初めてDBに一括保存（1トランザクション）
        await unitOfWork.SaveChangesAsync();

        return new DenominationCheckDto(...);
    }
}
```

## SaveChangesAsync とは

EF Core（Entity Framework Core）の `DbContext` が持つメソッドで、変更追跡されたエンティティをまとめてDBに書き込む操作。

```csharp
context.ChangeBags.Add(bag);     // メモリ上で「追加予定」としてマーク（DB未反映）
context.SaveChangesAsync();      // ここで初めてINSERT文がDBに発行される
```

EF Coreは内部でエンティティの変更状態を追跡している：

| 操作 | 追跡状態 | SaveChanges時に発行されるSQL |
|---|---|---|
| `Add(entity)` | Added | INSERT |
| プロパティ変更 | Modified | UPDATE |
| `Remove(entity)` | Deleted | DELETE |

`SaveChangesAsync()` を呼ぶまでDBには何も起きない。
呼んだ瞬間に溜まった変更がまとめて **1トランザクション** としてDBに反映される。

これがUnitOfWorkと相性が良い理由で、リポジトリで個別に `SaveChanges` せず、
UseCaseの最後に1回だけ呼ぶことで「全部成功 or 全部失敗」を実現している。

## 実装

- `IUnitOfWork` インターフェース: `PettyCash.Domain.Shared.Services`
- 実装: `PettyCashDbContext` が `IUnitOfWork` を実装（`DbContext.SaveChangesAsync()` に委譲）
- DI登録: `builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<PettyCashDbContext>())`
