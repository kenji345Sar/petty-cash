using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.Tests.Entities;

public class PrepBagTests
{
    private static Denomination MakeDenom(int yen10000 = 0, int yen5000 = 0, int yen1000 = 0) =>
        new(yen10000, yen5000, yen1000, 0, 0, 0, 0, 0, 0);

    private static CashBag CreateCashBag(int yen5000 = 0, int yen1000 = 0) =>
        CashBag.CreateDeposit(1, 0, "テスト", DateTime.UtcNow, MakeDenom(0, yen5000, yen1000));

    [Fact]
    public void Create_キャッシュバッグの合計が金額になる()
    {
        var cb1 = CreateCashBag(1, 0); // 5000
        var cb2 = CreateCashBag(0, 3); // 3000

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
        var cb = CreateCashBag(1, 0); // 5000
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);

        var tx = prep.MarkHandedOver(DateTime.UtcNow);

        Assert.Equal(PrepBagStatus.HandedOver, prep.Status);
        Assert.Equal(TransactionType.Withdrawal, tx.Type);
        Assert.Equal(5000, tx.Amount);
    }

    [Fact]
    public void MarkHandedOver_二重引渡は例外()
    {
        var cb = CreateCashBag(1, 0); // 5000
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);
        prep.MarkHandedOver(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            prep.MarkHandedOver(DateTime.UtcNow));
    }

    [Fact]
    public void Cancel_準備中のバッグを取消できる()
    {
        var cb = CreateCashBag(1, 0); // 5000
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);

        prep.Cancel();

        Assert.Equal(PrepBagStatus.Cancelled, prep.Status);
        Assert.Empty(prep.CashBags);
    }

    [Fact]
    public void Cancel_引渡済みは例外()
    {
        var cb = CreateCashBag(1, 0);
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);
        prep.MarkHandedOver(DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => prep.Cancel());
    }

    [Fact]
    public void Cancel_二重取消は例外()
    {
        var cb = CreateCashBag(1, 0);
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);
        prep.Cancel();

        Assert.Throws<InvalidOperationException>(() => prep.Cancel());
    }

    // ── バグ再現 ──────────────────────────────────────────────────
    // PrepBag.MarkHandedOver は HandedOver チェックのみで Cancelled を見ていない。
    // Cancelled 状態から MarkHandedOver を呼ぶと例外になるべき。
    [Fact]
    public void MarkHandedOver_取消済みのバッグから呼ぶと例外()
    {
        var cb = CreateCashBag(1, 0);
        var prep = PrepBag.Create(1, [cb], DateTime.UtcNow);
        prep.Cancel();

        Assert.Throws<InvalidOperationException>(() => prep.MarkHandedOver(DateTime.UtcNow));
    }
}
