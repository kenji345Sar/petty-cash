using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

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
    public void UpdateAmount_金額が更新される()
    {
        var bag = CashBag.CreateDeposit(1, 8000, "テスト", DateTime.UtcNow);

        bag.UpdateAmount(7500);

        Assert.Equal(7500, bag.TotalAmount);
    }
}
