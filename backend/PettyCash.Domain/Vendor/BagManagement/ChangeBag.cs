using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;
using PettyCash.Domain.Vendor.Ledger;

namespace PettyCash.Domain.Vendor.BagManagement;

/// <summary>
/// 釣り銭バッグ。業者から受け取った釣り銭を金庫で保管し、必要に応じてレジへ移動する。
///
/// 【ライフサイクル】
///   入金（金庫に保管）→ レジへ移動
///
/// 【業務ルール】
///   - 入金時は金種表の入力が必須（金額直接入力は不可）
///   - 金額は金種の合計から自動算出される
///   - レジへの移動は1回のみ（二重移動は不可）
///   - 入金時・移動時にそれぞれ業者取引（VendorTransaction）が自動生成される
/// </summary>
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
