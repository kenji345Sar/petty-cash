namespace PettyCash.Domain.SafeAggregate;

/// <summary>
/// 金庫。店舗ごとに1つ存在し、業者残高と小口残高を管理する。
///
/// 【業務ルール】
///   - 金庫名は必須
///   - 残高は業者残高（VendorBalance）と小口残高（PettyCashBalance）に分かれる
///   - 合計残高（CurrentBalance）は両者の合算
///   - 残高を超える出金はできない
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

    public void EnsureCanWithdraw(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("出金額は1以上である必要があります。");
        if (amount > CurrentBalance)
            throw new InvalidOperationException(
                $"残高不足です。現在残高: {CurrentBalance}円、出金額: {amount}円");
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
