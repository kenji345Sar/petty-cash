using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class CashBagTests
{
    private static Denomination MakeDenom(int yen10000 = 0, int yen5000 = 0, int yen1000 = 0) =>
        new(yen10000, yen5000, yen1000, 0, 0, 0, 0, 0, 0);

    [Fact]
    public void CreateDeposit_金種指定で作成できる()
    {
        var denom = MakeDenom(0, 1, 3); // 8000
        var bag = CashBag.CreateDeposit(1, 0, "レジ売上", DateTime.UtcNow, denom);

        Assert.Equal(8000, bag.TotalAmount);
        Assert.Equal(CashBagStatus.MovedToSafe, bag.Status);
        Assert.NotNull(bag.Transaction);
        Assert.Equal(TransactionType.Deposit, bag.Transaction!.Type);
    }

    [Fact]
    public void CreateDeposit_金種なしは例外()
    {
        Assert.Throws<ArgumentException>(() =>
            CashBag.CreateDeposit(1, 8000, "テスト", DateTime.UtcNow));
    }

    [Fact]
    public void UpdateAmount_金額が更新される()
    {
        var denom = MakeDenom(0, 1, 3); // 8000
        var bag = CashBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom);

        bag.UpdateAmount(7500);

        Assert.Equal(7500, bag.TotalAmount);
    }
}
