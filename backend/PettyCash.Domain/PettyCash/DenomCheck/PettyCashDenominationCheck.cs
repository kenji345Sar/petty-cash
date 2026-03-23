using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.PettyCash.DenomCheck;

public class PettyCashDenominationCheck
{
    public int Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public int SafeId { get; private set; }
    public Denomination Denomination { get; private set; } = null!;
    public int CheckedAmount { get; private set; }
    public int ExpectedAmount { get; private set; }
    public int Difference { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PettyCashDenominationCheck() { }

    internal void SetSequenceNumber(int sequenceNumber)
    {
        if (SequenceNumber != 0)
            throw new InvalidOperationException("採番済みのため、番号を変更できません。");
        if (sequenceNumber <= 0)
            throw new ArgumentException("採番は1以上である必要があります。");
        SequenceNumber = sequenceNumber;
    }

    public static PettyCashDenominationCheck CreateForSafe(int safeId, Denomination denomination, int expectedAmount, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new PettyCashDenominationCheck
        {
            SafeId = safeId,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = expectedAmount,
            Difference = checkedAmount - expectedAmount,
            CreatedAt = checkedAt
        };
    }

    public void Update(Denomination denomination, int expectedAmount)
    {
        Denomination = denomination;
        CheckedAmount = denomination.TotalAmount;
        ExpectedAmount = expectedAmount;
        Difference = CheckedAmount - expectedAmount;
    }
}
