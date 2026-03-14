using PettyCash.Domain.Enums;

namespace PettyCash.Domain.Entities;

public class Transaction
{
    public int Id { get; private set; }
    public int SafeId { get; private set; }
    public Safe? Safe { get; private set; }
    public int? ChangeBagId { get; private set; }
    public int? CashBagId { get; private set; }
    public int? PrepBagId { get; private set; }
    public TransactionType Type { get; private set; }
    public int Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public ChangeBag? ChangeBag { get; private set; }
    public CashBag? CashBag { get; private set; }
    public PrepBag? PrepBag { get; private set; }

    private Transaction() { }

    internal static Transaction CreateDeposit(ChangeBag bag, int amount, string description, DateTime date)
    {
        return new Transaction
        {
            SafeId = bag.SafeId,
            ChangeBag = bag,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(description) ? "釣り銭バッグ入金" : description,
            CreatedAt = date
        };
    }

    internal static Transaction CreateWithdrawal(ChangeBag bag, int amount)
    {
        return new Transaction
        {
            SafeId = bag.SafeId,
            ChangeBag = bag,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Description = "釣り銭バッグ出金（レジへ移動）",
            CreatedAt = DateTime.UtcNow
        };
    }

    internal static Transaction CreateCashBagDeposit(CashBag bag, int amount)
    {
        return new Transaction
        {
            SafeId = bag.SafeId,
            CashBag = bag,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(bag.Description) ? "キャッシュバッグ入金（レジから金庫へ）" : bag.Description,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Transaction CreateStandalone(int safeId, TransactionType type, int amount, string description, DateTime date)
    {
        return new Transaction
        {
            SafeId = safeId,
            Type = type,
            Amount = amount,
            Description = description,
            CreatedAt = date
        };
    }

    internal static Transaction CreatePrepBagWithdrawal(PrepBag bag, int amount)
    {
        var ids = string.Join(", ", bag.CashBags.Select(cb => $"#{cb.Id}"));
        return new Transaction
        {
            SafeId = bag.SafeId,
            PrepBag = bag,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Description = $"準備バッグ#{bag.Id} 引渡（{ids}）",
            CreatedAt = DateTime.UtcNow
        };
    }
}
