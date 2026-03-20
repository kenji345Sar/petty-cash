using PettyCash.Domain.Entities;
using PettyCash.Domain.Enums;
using PettyCash.Domain.ValueObjects;

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
    public void AdjustByCheck_差額があれば金額が更新され調整取引が返る()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);

        var adjustment = bag.AdjustByCheck(9500, DateTime.UtcNow);

        Assert.Equal(9500, bag.TotalAmount);
        Assert.NotNull(adjustment);
        Assert.Equal(TransactionType.Adjustment, adjustment!.Type);
        Assert.Equal(-500, adjustment.Amount); // 500円不足
    }

    [Fact]
    public void AdjustByCheck_差額がプラスの場合()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);

        var adjustment = bag.AdjustByCheck(10500, DateTime.UtcNow);

        Assert.Equal(10500, bag.TotalAmount);
        Assert.NotNull(adjustment);
        Assert.Equal(500, adjustment!.Amount);
    }

    [Fact]
    public void AdjustByCheck_差額ゼロならnull()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);

        var adjustment = bag.AdjustByCheck(10000, DateTime.UtcNow);

        Assert.Null(adjustment);
        Assert.Equal(10000, bag.TotalAmount);
    }
}
