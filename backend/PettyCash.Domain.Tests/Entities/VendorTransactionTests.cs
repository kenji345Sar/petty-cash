using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;

namespace PettyCash.Domain.Tests.Entities;

public class VendorTransactionTests
{
    [Fact]
    public void CreateSafeAdjustment_プラス差額()
    {
        var tx = VendorTransaction.CreateSafeAdjustment(1, 2000, DateTime.UtcNow);

        Assert.Equal(TransactionType.Adjustment, tx.Type);
        Assert.Equal(2000, tx.Amount);
        Assert.Contains("+2,000", tx.Description);
    }

    [Fact]
    public void CreateSafeAdjustment_マイナス差額()
    {
        var tx = VendorTransaction.CreateSafeAdjustment(1, -1500, DateTime.UtcNow);

        Assert.Equal(TransactionType.Adjustment, tx.Type);
        Assert.Equal(-1500, tx.Amount);
        Assert.Contains("-1,500", tx.Description);
    }

    [Fact]
    public void SetSequenceNumber_採番できる()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var tx = bag.DepositTransaction!;

        tx.SetSequenceNumber(1);

        Assert.Equal(1, tx.SequenceNumber);
    }

    [Fact]
    public void SetSequenceNumber_二重採番は例外()
    {
        var bag = ChangeBag.CreateDeposit(1, 10000, "テスト", DateTime.UtcNow);
        var tx = bag.DepositTransaction!;
        tx.SetSequenceNumber(1);

        Assert.Throws<InvalidOperationException>(() => tx.SetSequenceNumber(2));
    }
}
