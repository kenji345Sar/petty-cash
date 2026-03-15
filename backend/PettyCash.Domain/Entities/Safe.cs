namespace PettyCash.Domain.Entities;

public class Safe
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<Transaction> _transactions = [];
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

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

    public int CurrentBalance => VendorBalance + PettyCashBalance;

    public int VendorBalance => CalcBalance(t => t.HasBag);

    public int PettyCashBalance => CalcBalance(t => !t.HasBag);

    private int CalcBalance(Func<Transaction, bool> filter)
    {
        var filtered = _transactions.Where(filter);
        var deposits = filtered.Where(t => t.Type == Enums.TransactionType.Deposit).Sum(t => t.Amount);
        var withdrawals = filtered.Where(t => t.Type == Enums.TransactionType.Withdrawal).Sum(t => t.Amount);
        return deposits - withdrawals;
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
