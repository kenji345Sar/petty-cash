using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class ChangeBagTests
{
    private static Denomination MakeDenom(int yen10000 = 0, int yen5000 = 0, int yen1000 = 0) =>
        new(yen10000, yen5000, yen1000, 0, 0, 0, 0, 0, 0);

    [Fact]
    public void CreateDeposit_金種指定で作成できる()
    {
        var denom = MakeDenom(1); // 10000
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom);

        Assert.Equal(10000, bag.TotalAmount);
        Assert.Equal(BagStatus.InSafe, bag.Status);
        Assert.NotNull(bag.DepositTransaction);
        Assert.Equal(TransactionType.Deposit, bag.DepositTransaction!.Type);
    }

    [Fact]
    public void CreateDeposit_金種なしは例外()
    {
        Assert.Throws<ArgumentException>(() =>
            ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow));
    }

    [Fact]
    public void CreateDeposit_金額ゼロは例外()
    {
        var denom = new Denomination(0, 0, 0, 0, 0, 0, 0, 0, 0);
        Assert.Throws<ArgumentException>(() =>
            ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom));
    }

    [Fact]
    public void MoveToRegister_ステータスが変わり出金取引が生成される()
    {
        var denom = MakeDenom(0, 1); // 5000
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom);

        var tx = bag.MoveToRegister(DateTime.UtcNow);

        Assert.Equal(BagStatus.MovedToRegister, bag.Status);
        Assert.Equal(TransactionType.Withdrawal, tx.Type);
        Assert.Equal(5000, tx.Amount);
    }

    [Fact]
    public void MoveToRegister_二重移動は例外()
    {
        var denom = MakeDenom(0, 1); // 5000
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom);
        bag.MoveToRegister(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            bag.MoveToRegister(DateTime.UtcNow));
    }

    [Fact]
    public void UpdateAmount_金額が更新される()
    {
        var denom = MakeDenom(1); // 10000
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom);

        bag.UpdateAmount(9500);

        Assert.Equal(9500, bag.TotalAmount);
    }
}
