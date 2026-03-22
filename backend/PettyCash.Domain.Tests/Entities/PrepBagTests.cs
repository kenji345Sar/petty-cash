using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;

namespace PettyCash.Domain.Tests.Entities;

public class PrepBagTests
{
    private static CashBag CreateCashBag(int amount) =>
        CashBag.CreateDeposit(1, amount, "テスト", DateTime.UtcNow);

    [Fact]
    public void Create_キャッシュバッグの合計が金額になる()
    {
        var cb1 = CreateCashBag(5000);
        var cb2 = CreateCashBag(3000);

        var prep = PrepBag.Create(1, [cb1, cb2], DateTime.UtcNow);

        Assert.Equal(8000, prep.TotalAmount);
        Assert.Equal(PrepBagStatus.Preparing, prep.Status);
    }

    [Fact]
    public void Create_空リストは例外()
    {
        Assert.Throws<ArgumentException>(() =>
            PrepBag.Create(1, [], DateTime.UtcNow));
    }

    [Fact]
    public void MarkHandedOver_ステータスが変わり出金取引が生成される()
    {
        var cb = CreateCashBag(5000);
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);

        var tx = prep.MarkHandedOver(DateTime.UtcNow);

        Assert.Equal(PrepBagStatus.HandedOver, prep.Status);
        Assert.Equal(TransactionType.Withdrawal, tx.Type);
        Assert.Equal(5000, tx.Amount);
    }

    [Fact]
    public void MarkHandedOver_二重引渡は例外()
    {
        var cb = CreateCashBag(5000);
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);
        prep.MarkHandedOver(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            prep.MarkHandedOver(DateTime.UtcNow));
    }
}
