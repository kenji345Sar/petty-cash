using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class PettyCashTransactionTests
{
    [Fact]
    public void Create_金額指定で作成できる()
    {
        var tx = PettyCashTransaction.Create(1, TransactionType.Deposit, 5000, "小口入金", DateTime.UtcNow);

        Assert.Equal(5000, tx.Amount);
        Assert.Equal(TransactionType.Deposit, tx.Type);
    }

    [Fact]
    public void Create_金種指定なら金種合計が金額になる()
    {
        var denom = new Denomination(0, 1, 0, 0, 0, 0, 0, 0, 0); // 5000
        var tx = PettyCashTransaction.Create(1, TransactionType.Withdrawal, 0, "テスト", DateTime.UtcNow, denom);

        Assert.Equal(5000, tx.Amount);
    }

    [Fact]
    public void Create_金額ゼロは例外()
    {
        Assert.Throws<ArgumentException>(() =>
            PettyCashTransaction.Create(1, TransactionType.Deposit, 0, "テスト", DateTime.UtcNow));
    }

    // ── CreateReversal（逆転ロジック）──────────────────────────────
    [Fact]
    public void CreateReversal_入金の赤伝は出金になる()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Deposit, 5000, "小口入金", DateTime.UtcNow);

        var reversal = PettyCashTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(TransactionType.Withdrawal, reversal.Type);
        Assert.Equal(5000, reversal.Amount);
        Assert.Equal(1, reversal.SafeId);
    }

    [Fact]
    public void CreateReversal_出金の赤伝は入金になる()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Withdrawal, 3000, "小口出金", DateTime.UtcNow);

        var reversal = PettyCashTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(TransactionType.Deposit, reversal.Type);
        Assert.Equal(3000, reversal.Amount);
    }

    [Fact]
    public void CreateReversal_調整プラスの赤伝は出金になる()
    {
        var original = PettyCashTransaction.Create(1, TransactionType.Adjustment, 2000, "有高調整", DateTime.UtcNow);

        var reversal = PettyCashTransaction.CreateReversal(original, "赤伝", DateTime.UtcNow);

        Assert.Equal(TransactionType.Withdrawal, reversal.Type);
        Assert.Equal(2000, reversal.Amount);
    }
}
