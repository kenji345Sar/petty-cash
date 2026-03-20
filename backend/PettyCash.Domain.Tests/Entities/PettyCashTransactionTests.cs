using PettyCash.Domain.Entities;
using PettyCash.Domain.Enums;
using PettyCash.Domain.ValueObjects;

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
}
