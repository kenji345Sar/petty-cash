using PettyCash.Domain.ValueObjects;

namespace PettyCash.Domain.Entities;

public class DenominationCheck
{
    public int Id { get; private set; }
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

    public static DenominationCheck CreateForChangeBag(ChangeBag bag, Denomination denomination)
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
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(Denomination denomination, int expectedAmount)
    {
        Denomination = denomination;
        CheckedAmount = denomination.TotalAmount;
        ExpectedAmount = expectedAmount;
        Difference = CheckedAmount - expectedAmount;
    }

    public static DenominationCheck CreateForCashBag(CashBag bag, Denomination denomination)
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
            CreatedAt = DateTime.UtcNow
        };
    }

    public static DenominationCheck CreateForPrepBag(PrepBag bag, Denomination denomination)
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
            CreatedAt = DateTime.UtcNow
        };
    }
}
