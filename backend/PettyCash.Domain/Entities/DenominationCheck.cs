using PettyCash.Domain.ValueObjects;

namespace PettyCash.Domain.Entities;

public class DenominationCheck
{
    public int Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public int SafeId { get; private set; }
    public Safe? Safe { get; private set; }
    public int? ChangeBagId { get; private set; }
    public int? CashBagId { get; private set; }
    public int? PrepBagId { get; private set; }
    public Denomination Denomination { get; private set; } = null!;
    public int CheckedAmount { get; private set; }
    public int ExpectedAmount { get; private set; }
    public int Difference { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ChangeBag? ChangeBag { get; private set; }
    public CashBag? CashBag { get; private set; }
    public PrepBag? PrepBag { get; private set; }

    private DenominationCheck() { }

    internal void SetSequenceNumber(int sequenceNumber)
    {
        if (SequenceNumber != 0)
            throw new InvalidOperationException("採番済みのため、番号を変更できません。");
        if (sequenceNumber <= 0)
            throw new ArgumentException("採番は1以上である必要があります。");
        SequenceNumber = sequenceNumber;
    }

    public static DenominationCheck CreateForChangeBag(ChangeBag bag, Denomination denomination, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new DenominationCheck
        {
            SafeId = bag.SafeId,
            ChangeBagId = bag.Id,
            ChangeBag = bag,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = bag.TotalAmount,
            Difference = checkedAmount - bag.TotalAmount,
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

    public static DenominationCheck CreateForCashBag(CashBag bag, Denomination denomination, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new DenominationCheck
        {
            SafeId = bag.SafeId,
            CashBagId = bag.Id,
            CashBag = bag,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = bag.TotalAmount,
            Difference = checkedAmount - bag.TotalAmount,
            CreatedAt = checkedAt
        };
    }

    public static DenominationCheck CreateForPrepBag(PrepBag bag, Denomination denomination, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new DenominationCheck
        {
            SafeId = bag.SafeId,
            PrepBagId = bag.Id,
            PrepBag = bag,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = bag.TotalAmount,
            Difference = checkedAmount - bag.TotalAmount,
            CreatedAt = checkedAt
        };
    }

    public static DenominationCheck CreateForSafe(int safeId, Denomination denomination, int expectedAmount, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new DenominationCheck
        {
            SafeId = safeId,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = expectedAmount,
            Difference = checkedAmount - expectedAmount,
            CreatedAt = checkedAt
        };
    }
}
