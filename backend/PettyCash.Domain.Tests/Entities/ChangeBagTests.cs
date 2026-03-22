using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class ChangeBagTests
{
    [Fact]
    public void CreateDeposit_金額指定で作成できる()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);

        Assert.Equal(10000, bag.TotalAmount);
        Assert.Equal(BagStatus.InSafe, bag.Status);
        Assert.NotNull(bag.DepositTransaction);
        Assert.Equal(TransactionType.Deposit, bag.DepositTransaction!.Type);
    }

    [Fact]
    public void CreateDeposit_金種指定なら金種合計が金額になる()
    {
        var denom = new Denomination(1, 0, 0, 0, 0, 0, 0, 0, 0); // 1万円
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, denom);

        Assert.Equal(10000, bag.TotalAmount);
    }

    [Fact]
    public void CreateDeposit_金額ゼロは例外()
    {
        Assert.Throws<ArgumentException>(() =>
            ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow));
    }

    [Fact]
    public void MoveToRegister_ステータスが変わり出金取引が生成される()
    {
        var bag = ChangeBag.CreateDeposit(1, 5000, "テスト", DateTime.UtcNow);

        var tx = bag.MoveToRegister(DateTime.UtcNow);

        Assert.Equal(BagStatus.MovedToRegister, bag.Status);
        Assert.Equal(TransactionType.Withdrawal, tx.Type);
        Assert.Equal(5000, tx.Amount);
    }

    [Fact]
    public void MoveToRegister_二重移動は例外()
    {
        var bag = ChangeBag.CreateDeposit(1, 5000, "テスト", DateTime.UtcNow);
        bag.MoveToRegister(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            bag.MoveToRegister(DateTime.UtcNow));
    }

    [Fact]
    public void UpdateAmount_金額が更新される()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);

        bag.UpdateAmount(9500);

        Assert.Equal(9500, bag.TotalAmount);
    }
}
