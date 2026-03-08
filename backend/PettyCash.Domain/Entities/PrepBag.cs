using PettyCash.Domain.Enums;

namespace PettyCash.Domain.Entities;

public class PrepBag
{
    public int Id { get; private set; }
    public int TotalAmount { get; private set; }
    public PrepBagStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? HandedOverAt { get; private set; }

    public Transaction? Transaction { get; private set; }

    private readonly List<DenominationCheck> _denominationChecks = [];
    public IReadOnlyCollection<DenominationCheck> DenominationChecks => _denominationChecks.AsReadOnly();

    private readonly List<CashBag> _cashBags = [];
    public IReadOnlyCollection<CashBag> CashBags => _cashBags.AsReadOnly();

    private PrepBag() { }

    public static PrepBag Create(IReadOnlyList<CashBag> cashBags, DateTime date)
    {
        if (cashBags.Count == 0)
            throw new ArgumentException("キャッシュバッグを1つ以上選択してください。");

        var bag = new PrepBag
        {
            TotalAmount = cashBags.Sum(b => b.TotalAmount),
            Status = PrepBagStatus.Preparing,
            CreatedAt = date
        };

        foreach (var cashBag in cashBags)
        {
            bag._cashBags.Add(cashBag);
        }

        return bag;
    }

    public Transaction MarkHandedOver()
    {
        if (Status == PrepBagStatus.HandedOver)
            throw new InvalidOperationException("この準備バッグは既に引渡済みです。");

        Status = PrepBagStatus.HandedOver;
        HandedOverAt = DateTime.UtcNow;

        var tx = Transaction.CreatePrepBagWithdrawal(this, TotalAmount);
        Transaction = tx;
        return tx;
    }
}
