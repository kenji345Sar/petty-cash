using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class VendorTransactionTests
{
    private static Denomination MakeDenom(int yen10000 = 0, int yen5000 = 0, int yen1000 = 0) =>
        new(yen10000, yen5000, yen1000, 0, 0, 0, 0, 0, 0);

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
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, MakeDenom(1));
        var tx = bag.DepositTransaction!;

        tx.SetSequenceNumber(1);

        Assert.Equal(1, tx.SequenceNumber);
    }

    [Fact]
    public void SetSequenceNumber_二重採番は例外()
    {
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, MakeDenom(1));
        var tx = bag.DepositTransaction!;
        tx.SetSequenceNumber(1);

        Assert.Throws<InvalidOperationException>(() => tx.SetSequenceNumber(2));
    }

    // ── CreateReversal（逆転ロジック）──────────────────────────────
    [Fact]
    public void CreateReversal_入金の赤伝は出金になる()
    {
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, MakeDenom(1)); // 10000
        var original = bag.DepositTransaction!; // Type=Deposit, Amount=10000, SafeId=1

        var reversal = VendorTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(TransactionType.Withdrawal, reversal.Type);
        Assert.Equal(10000, reversal.Amount);
    }

    [Fact]
    public void CreateReversal_出金の赤伝は入金になる()
    {
        var bag = ChangeBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, MakeDenom(1));
        var original = bag.MoveToRegister(DateTime.UtcNow); // Type=Withdrawal, Amount=10000

        var reversal = VendorTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(TransactionType.Deposit, reversal.Type);
        Assert.Equal(10000, reversal.Amount);
    }

    [Fact]
    public void CreateReversal_調整プラスの赤伝は出金になる()
    {
        var original = VendorTransaction.CreateSafeAdjustment(1, 2000, DateTime.UtcNow); // Amount=+2000

        var reversal = VendorTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(TransactionType.Withdrawal, reversal.Type);
        Assert.Equal(2000, reversal.Amount);
    }

    [Fact]
    public void CreateReversal_調整マイナスの赤伝は入金で絶対値金額になる()
    {
        var original = VendorTransaction.CreateSafeAdjustment(1, -1500, DateTime.UtcNow); // Amount=-1500

        var reversal = VendorTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(TransactionType.Deposit, reversal.Type);
        Assert.Equal(1500, reversal.Amount); // 絶対値
    }

    [Fact]
    public void CreateReversal_SafeIdが元取引から引き継がれる()
    {
        var bag = ChangeBag.CreateDeposit(42, 0, "テスト", DateTime.UtcNow, MakeDenom(1));
        var original = bag.DepositTransaction!;

        var reversal = VendorTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(42, reversal.SafeId);
    }
}
