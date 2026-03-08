using PettyCash.Domain.Enums;

namespace PettyCash.Domain.Entities;

public class ChangeBag
{
    public int Id { get; private set; }
    public int TotalAmount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public BagStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? MovedAt { get; private set; }

    private readonly List<Transaction> _transactions = [];
    public Transaction? DepositTransaction => _transactions.FirstOrDefault(t => t.Type == Enums.TransactionType.Deposit);
    public Transaction? WithdrawalTransaction => _transactions.FirstOrDefault(t => t.Type == Enums.TransactionType.Withdrawal);

    private readonly List<DenominationCheck> _denominationChecks = [];
    public IReadOnlyCollection<DenominationCheck> DenominationChecks => _denominationChecks.AsReadOnly();

    private ChangeBag() { }

    public static ChangeBag CreateDeposit(int amount, string description, DateTime date)
    {
        if (amount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        var bag = new ChangeBag
        {
            TotalAmount = amount,
            Description = description,
            Status = BagStatus.InSafe,
            CreatedAt = date
        };

        bag._transactions.Add(Transaction.CreateDeposit(bag, amount, description, date));

        return bag;
    }

    public Transaction MoveToRegister()
    {
        if (Status == BagStatus.MovedToRegister)
        {
            throw new InvalidOperationException("このバッグは既にレジへ移動済みです。");
        }

        Status = BagStatus.MovedToRegister;
        MovedAt = DateTime.UtcNow;

        var transaction = Transaction.CreateWithdrawal(this, TotalAmount);
        _transactions.Add(transaction);

        return transaction;
    }
}
