namespace PettyCash.Domain.SafeAggregate;

/// <summary>
/// 金庫。店舗ごとに1つ存在し、業者残高と小口残高を管理する。
///
/// 【業務ルール】
///   - 金庫名は必須
///   - 残高は売上金残高（VendorBalance）と小口残高（PettyCashBalance）に分かれる
///   - 合計残高（CurrentBalance）は両者の合算。表示にのみ使い、出金の判定には使わない
///   - 出金は、その出納の残高を超えられない（小口の出金は小口残高、売上金の出金は売上金残高で判定）
/// </summary>
public class Safe
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public int CurrentBalance { get; private set; }
    public int VendorBalance { get; private set; }
    public int PettyCashBalance { get; private set; }

    private Safe() { }

    public static Safe Create(string name, string description, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("金庫名は必須です。");

        return new Safe
        {
            Name = name,
            Description = description,
            CreatedAt = date
        };
    }

    public void SetBalances(int vendorBalance, int pettyCashBalance)
    {
        VendorBalance = vendorBalance;
        PettyCashBalance = pettyCashBalance;
        CurrentBalance = vendorBalance + pettyCashBalance;
    }

    /// <summary>
    /// 小口の出金が可能か判定する。小口現金と売上金は別の出納なので、売上金残高は算入しない。
    /// </summary>
    public void EnsureCanWithdrawPettyCash(int amount)
    {
        EnsureCanWithdraw(amount, PettyCashBalance, "小口残高");
    }

    /// <summary>
    /// 売上金の出金が可能か判定する。小口残高は算入しない。
    /// </summary>
    public void EnsureCanWithdrawVendor(int amount)
    {
        EnsureCanWithdraw(amount, VendorBalance, "売上金残高");
    }

    private static void EnsureCanWithdraw(int amount, int balance, string balanceName)
    {
        if (amount <= 0)
            throw new ArgumentException("出金額は1以上である必要があります。");
        if (amount > balance)
            throw new InvalidOperationException(
                $"残高不足です。{balanceName}: {balance}円、出金額: {amount}円");
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("金庫名は必須です。");
        Name = name;
    }

    public void UpdateDescription(string description)
    {
        Description = description;
    }
}
