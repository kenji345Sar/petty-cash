using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
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

    // ── 小口 ──────────────────────────────────────────────────────
    [Fact]
    public void EnsureCanWithdrawPettyCash_小口残高内なら例外なし()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        safe.EnsureCanWithdrawPettyCash(5000); // 例外が出なければOK
    }

    [Fact]
    public void EnsureCanWithdrawPettyCash_売上金があっても小口残高超過は例外()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000); // 合計35000だが小口は5000

        var ex = Assert.Throws<InvalidOperationException>(() =>
            safe.EnsureCanWithdrawPettyCash(5001));
        Assert.Contains("小口残高", ex.Message);
    }

    [Fact]
    public void EnsureCanWithdrawPettyCash_ゼロ以下は例外()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        Assert.Throws<ArgumentException>(() =>
            safe.EnsureCanWithdrawPettyCash(0));
    }

    // ── 売上金 ────────────────────────────────────────────────────
    [Fact]
    public void EnsureCanWithdrawVendor_売上金残高内なら例外なし()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        safe.EnsureCanWithdrawVendor(30000);
    }

    [Fact]
    public void EnsureCanWithdrawVendor_小口があっても売上金残高超過は例外()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            safe.EnsureCanWithdrawVendor(30001));
        Assert.Contains("売上金残高", ex.Message);
    }

    [Fact]
    public void EnsureCanWithdrawVendor_ゼロ以下は例外()
    {
        var safe = Safe.Create("テスト金庫", "", DateTime.UtcNow);
        safe.SetBalances(30000, 5000);

        Assert.Throws<ArgumentException>(() =>
            safe.EnsureCanWithdrawVendor(0));
    }
}
