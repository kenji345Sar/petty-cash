using PettyCash.Domain.Enums;

namespace PettyCash.Domain.Entities;

public class CashBag
{
    public int Id { get; private set; }
    public int SafeId { get; private set; }
    public Safe? Safe { get; private set; }
    public int TotalAmount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public CashBagStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? MovedAt { get; private set; }

    public int? PrepBagId { get; private set; }
    public PrepBag? PrepBag { get; private set; }

    public Transaction? Transaction { get; private set; }

    private readonly List<DenominationCheck> _denominationChecks = [];
    public IReadOnlyCollection<DenominationCheck> DenominationChecks => _denominationChecks.AsReadOnly();

    private CashBag() { }

    public static CashBag CreateDeposit(int safeId, int amount, string description, DateTime date)
    {
        if (amount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        var bag = new CashBag
        {
            SafeId = safeId,
            TotalAmount = amount,
            Description = description,
            Status = CashBagStatus.MovedToSafe,
            CreatedAt = date,
            MovedAt = date
        };

        bag.Transaction = Transaction.CreateCashBagDeposit(bag, amount);

        return bag;
    }
}
