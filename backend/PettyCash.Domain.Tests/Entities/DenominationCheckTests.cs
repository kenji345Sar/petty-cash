using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class DenominationCheckTests
{
    [Fact]
    public void CreateForChangeBag_差額が正しく計算される()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var denom = new Denomination(0, 1, 4, 2, 0, 0, 0, 0, 0); // 5000+4000+1000 = 10000

        var check = DenominationCheck.CreateForChangeBag(bag, denom, DateTime.UtcNow);

        Assert.Equal(10000, check.CheckedAmount);
        Assert.Equal(10000, check.ExpectedAmount);
        Assert.Equal(0, check.Difference);
    }

    [Fact]
    public void CreateForChangeBag_不足の場合マイナス差額()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var denom = new Denomination(0, 1, 4, 0, 0, 0, 0, 0, 0); // 9000

        var check = DenominationCheck.CreateForChangeBag(bag, denom, DateTime.UtcNow);

        Assert.Equal(9000, check.CheckedAmount);
        Assert.Equal(10000, check.ExpectedAmount);
        Assert.Equal(-1000, check.Difference);
    }

    [Fact]
    public void CreateForChangeBag_過剰の場合プラス差額()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var denom = new Denomination(1, 0, 1, 0, 0, 0, 0, 0, 0); // 11000

        var check = DenominationCheck.CreateForChangeBag(bag, denom, DateTime.UtcNow);

        Assert.Equal(11000, check.CheckedAmount);
        Assert.Equal(10000, check.ExpectedAmount);
        Assert.Equal(1000, check.Difference);
    }

    [Fact]
    public void CreateForCashBag_差額が正しく計算される()
    {
        var bag = CashBag.CreateDeposit(1, 5000, "テスト", DateTime.UtcNow);
        var denom = new Denomination(0, 1, 0, 0, 0, 0, 0, 0, 0); // 5000

        var check = DenominationCheck.CreateForCashBag(bag, denom, DateTime.UtcNow);

        Assert.Equal(5000, check.CheckedAmount);
        Assert.Equal(5000, check.ExpectedAmount);
        Assert.Equal(0, check.Difference);
    }

    [Fact]
    public void CreateForSafe_差額が正しく計算される()
    {
        var denom = new Denomination(5, 0, 0, 0, 0, 0, 0, 0, 0); // 50000

        var check = DenominationCheck.CreateForSafe(1, denom, 48000, DateTime.UtcNow);

        Assert.Equal(50000, check.CheckedAmount);
        Assert.Equal(48000, check.ExpectedAmount);
        Assert.Equal(2000, check.Difference);
    }

    [Fact]
    public void Update_金種と期待額を更新できる()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var denom1 = new Denomination(1, 0, 0, 0, 0, 0, 0, 0, 0);
        var check = DenominationCheck.CreateForChangeBag(bag, denom1, DateTime.UtcNow);

        var denom2 = new Denomination(0, 1, 4, 1, 0, 0, 0, 0, 0); // 9500
        check.Update(denom2, 10000);

        Assert.Equal(9500, check.CheckedAmount);
        Assert.Equal(10000, check.ExpectedAmount);
        Assert.Equal(-500, check.Difference);
    }

    [Fact]
    public void SetSequenceNumber_採番できる()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var denom = new Denomination(1, 0, 0, 0, 0, 0, 0, 0, 0);
        var check = DenominationCheck.CreateForChangeBag(bag, denom, DateTime.UtcNow);

        check.SetSequenceNumber(1);

        Assert.Equal(1, check.SequenceNumber);
    }

    [Fact]
    public void SetSequenceNumber_二重採番は例外()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var denom = new Denomination(1, 0, 0, 0, 0, 0, 0, 0, 0);
        var check = DenominationCheck.CreateForChangeBag(bag, denom, DateTime.UtcNow);
        check.SetSequenceNumber(1);

        Assert.Throws<InvalidOperationException>(() => check.SetSequenceNumber(2));
    }

    [Fact]
    public void SetSequenceNumber_ゼロ以下は例外()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var denom = new Denomination(1, 0, 0, 0, 0, 0, 0, 0, 0);
        var check = DenominationCheck.CreateForChangeBag(bag, denom, DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => check.SetSequenceNumber(0));
    }
}
