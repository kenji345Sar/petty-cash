namespace PettyCash.Domain.Entities;

public class Safe
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<Transaction> _transactions = [];
    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    private readonly List<ChangeBag> _changeBags = [];
    public IReadOnlyCollection<ChangeBag> ChangeBags => _changeBags.AsReadOnly();

    private readonly List<CashBag> _cashBags = [];
    public IReadOnlyCollection<CashBag> CashBags => _cashBags.AsReadOnly();

    private readonly List<PrepBag> _prepBags = [];
    public IReadOnlyCollection<PrepBag> PrepBags => _prepBags.AsReadOnly();

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

    public int CurrentBalance
    {
        get
        {
            var deposits = _transactions
                .Where(t => t.Type == Enums.TransactionType.Deposit)
                .Sum(t => t.Amount);
            var withdrawals = _transactions
                .Where(t => t.Type == Enums.TransactionType.Withdrawal)
                .Sum(t => t.Amount);
            return deposits - withdrawals;
        }
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
