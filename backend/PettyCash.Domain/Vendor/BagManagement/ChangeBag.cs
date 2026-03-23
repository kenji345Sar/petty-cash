using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;
using PettyCash.Domain.Vendor.Ledger;

namespace PettyCash.Domain.Vendor.BagManagement;

public class ChangeBag
{
    public int Id { get; private set; }
    public int SafeId { get; private set; }
    public Safe? Safe { get; private set; }
    public int TotalAmount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public BagStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? MovedAt { get; private set; }

    private readonly List<VendorTransaction> _transactions = [];
    public VendorTransaction? DepositTransaction => _transactions.FirstOrDefault(t => t.Type == TransactionType.Deposit);
    public VendorTransaction? WithdrawalTransaction => _transactions.FirstOrDefault(t => t.Type == TransactionType.Withdrawal);

    private readonly List<VendorDenominationCheck> _denominationChecks = [];
    public IReadOnlyCollection<VendorDenominationCheck> DenominationChecks => _denominationChecks.AsReadOnly();

    private ChangeBag() { }

    public static ChangeBag CreateDeposit(int safeId, int amount, string description, DateTime date, Denomination? denomination = null)
    {
        if (denomination == null)
            throw new ArgumentException("金種表の入力が必須です。");

        var finalAmount = denomination.TotalAmount;
        if (finalAmount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        var bag = new ChangeBag
        {
            SafeId = safeId,
            TotalAmount = finalAmount,
            Description = description,
            Status = BagStatus.InSafe,
            CreatedAt = date
        };

        bag._transactions.Add(VendorTransaction.CreateDeposit(bag, finalAmount, description, date, denomination));

        return bag;
    }

    public VendorTransaction MoveToRegister(DateTime movedAt)
    {
        if (Status == BagStatus.MovedToRegister)
        {
            throw new InvalidOperationException("このバッグは既にレジへ移動済みです。");
        }

        Status = BagStatus.MovedToRegister;
        MovedAt = movedAt;

        var transaction = VendorTransaction.CreateWithdrawal(this, TotalAmount, movedAt);
        _transactions.Add(transaction);

        return transaction;
    }

    public void UpdateAmount(int newAmount)
    {
        TotalAmount = newAmount;
    }
}
