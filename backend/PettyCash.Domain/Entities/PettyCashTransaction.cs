using PettyCash.Domain.Enums;
using PettyCash.Domain.ValueObjects;

namespace PettyCash.Domain.Entities;

public class PettyCashTransaction
{
    public int Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public int SafeId { get; private set; }
    public TransactionType Type { get; private set; }
    public int Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public Denomination? Denomination { get; private set; }

    private PettyCashTransaction() { }

    internal void SetSequenceNumber(int sequenceNumber)
    {
        if (SequenceNumber != 0)
            throw new InvalidOperationException("採番済みのため、番号を変更できません。");
        if (sequenceNumber <= 0)
            throw new ArgumentException("採番は1以上である必要があります。");
        SequenceNumber = sequenceNumber;
    }

    public static PettyCashTransaction Create(int safeId, TransactionType type, int amount, string description, DateTime date, Denomination? denomination = null)
    {
        var finalAmount = denomination?.TotalAmount ?? amount;
        if (finalAmount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        return new PettyCashTransaction
        {
            SafeId = safeId,
            Type = type,
            Amount = finalAmount,
            Description = description,
            Denomination = denomination,
            CreatedAt = date
        };
    }
}
