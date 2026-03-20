using PettyCash.Domain.Entities;
using PettyCash.Domain.Enums;
using PettyCash.Domain.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class CashBagTests
{
    [Fact]
    public void CreateDeposit_金額指定で作成できる()
    {
        var bag = CashBag.CreateDeposit(1, 8000, "レジ売上", DateTime.UtcNow);

        Assert.Equal(8000, bag.TotalAmount);
        Assert.Equal(CashBagStatus.MovedToSafe, bag.Status);
        Assert.NotNull(bag.Transaction);
        Assert.Equal(TransactionType.Deposit, bag.Transaction!.Type);
    }

    [Fact]
    public void CreateDeposit_金種指定なら金種合計が金額になる()
    {
        var denom = new Denomination(0, 1, 3, 0, 0, 0, 0, 0, 0); // 8000
        var bag = CashBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom);

        Assert.Equal(8000, bag.TotalAmount);
    }

    [Fact]
    public void AdjustByCheck_差額があれば金額が更新される()
    {
        var bag = CashBag.CreateDeposit(1, 8000, "テスト", DateTime.UtcNow);

        var adjustment = bag.AdjustByCheck(7500, DateTime.UtcNow);

        Assert.Equal(7500, bag.TotalAmount);
        Assert.NotNull(adjustment);
        Assert.Equal(TransactionType.Adjustment, adjustment!.Type);
        Assert.Equal(-500, adjustment.Amount);
    }

    [Fact]
    public void AdjustByCheck_差額ゼロならnull()
    {
        var bag = CashBag.CreateDeposit(1, 8000, "テスト", DateTime.UtcNow);

        var adjustment = bag.AdjustByCheck(8000, DateTime.UtcNow);

        Assert.Null(adjustment);
    }
}
