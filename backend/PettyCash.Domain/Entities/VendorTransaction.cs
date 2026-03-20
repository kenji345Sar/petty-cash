using PettyCash.Domain.Enums;
using PettyCash.Domain.ValueObjects;

namespace PettyCash.Domain.Entities;

public class VendorTransaction
{
    public int Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public int SafeId { get; private set; }
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

    public Denomination? Denomination { get; private set; }

    private VendorTransaction() { }

    internal void SetSequenceNumber(int sequenceNumber)
    {
        if (SequenceNumber != 0)
            throw new InvalidOperationException("採番済みのため、番号を変更できません。");
        if (sequenceNumber <= 0)
            throw new ArgumentException("採番は1以上である必要があります。");
        SequenceNumber = sequenceNumber;
    }

    internal static VendorTransaction CreateDeposit(ChangeBag bag, int amount, string description, DateTime date, Denomination? denomination = null)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            ChangeBag = bag,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(description) ? "釣り銭バッグ入金" : description,
            Denomination = denomination,
            CreatedAt = date
        };
    }

    internal static VendorTransaction CreateWithdrawal(ChangeBag bag, int amount, DateTime date)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            ChangeBag = bag,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Description = "釣り銭バッグ出金（レジへ移動）",
            CreatedAt = date
        };
    }

    internal static VendorTransaction CreateCashBagDeposit(CashBag bag, int amount, DateTime date, Denomination? denomination = null)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            CashBag = bag,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(bag.Description) ? "キャッシュバッグ入金（レジから金庫へ）" : bag.Description,
            Denomination = denomination,
            CreatedAt = date
        };
    }

    internal static VendorTransaction CreatePrepBagWithdrawal(PrepBag bag, int amount, DateTime date)
    {
        var ids = string.Join(", ", bag.CashBags.Select(cb => $"#{cb.Id}"));
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            PrepBag = bag,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Description = $"準備バッグ#{bag.Id} 引渡（{ids}）",
            CreatedAt = date
        };
    }
}
