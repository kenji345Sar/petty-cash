using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Domain.Tests.Entities;

public class SafeTests
{
    [Fact]
    public void Create_名前が必須()
    {
        Assert.Throws<ArgumentException>(() =>
            Safe.Create("", "説明", DateTime.UtcNow));
    }

    [Fact]
    public void SetBalances_合計残高が正しい()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);

        safe.SetBalances(30000, 5000);

        Assert.Equal(30000, safe.VendorBalance);
        Assert.Equal(5000, safe.PettyCashBalance);
        Assert.Equal(35000, safe.CurrentBalance);
    }

    [Fact]
    public void EnsureCanWithdraw_残高内なら例外なし()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        safe.EnsureCanWithdraw(35000); // 例外が出なければOK
    }

    [Fact]
    public void EnsureCanWithdraw_残高超過は例外()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        Assert.Throws<InvalidOperationException>(() =>
            safe.EnsureCanWithdraw(35001));
    }

    [Fact]
    public void EnsureCanWithdraw_ゼロ以下は例外()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        Assert.Throws<ArgumentException>(() =>
            safe.EnsureCanWithdraw(0));
    }
}
