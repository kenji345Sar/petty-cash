using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;
using PettyCash.Domain.Vendor.BagManagement;

namespace PettyCash.Domain.Vendor.Ledger;

/// <summary>
/// 業者取引。業者との間で発生する入出金・調整の記録（業者出納帳の1行）。
///
/// 【取引種別】
///   - 入金（Deposit）: 両替金バッグ/売上バッグの金庫への入金
///   - 出金（Withdrawal）: 両替金バッグのレジ移動、準備バッグの業者引渡
///   - 調整（Adjustment）: 有高チェックで差額が出た場合の自動調整
///
/// 【業務ルール】
///   - 採番（SequenceNumber）は金庫単位で一意、1回のみ設定可能
///   - 金種情報を保持可能（入金時に金種表が入力された場合）
///   - バッグ操作から自動生成される（直接作成は調整のみ）
/// </summary>
public class VendorTransaction
{
    public int Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public int SafeId { get; private set; }
    public int? ChangeBagId { get; private set; }
    public int? CashBagId { get; private set; }
    public int? PrepBagId { get; private set; }
    public TransactionType Type { get; private set; }
    public int Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public ChangeBag? ChangeBag { get; private set; }
    public CashBag? CashBag { get; private set; }
    public PrepBag? PrepBag { get; private set; }

    public Denomination? Denomination { get; private set; }

    /// <summary>この取引後の業者残高（ランニングバランス）</summary>
    public int Balance { get; private set; }

    private VendorTransaction() { }

    internal void SetBalance(int balance)
    {
        Balance = balance;
    }

    internal void SetSequenceNumber(int sequenceNumber)
    {
        if (SequenceNumber != 0)
            throw new InvalidOperationException("採番済みのため、番号を変更できません。");
        if (sequenceNumber <= 0)
            throw new ArgumentException("採番は1以上である必要があります。");
        SequenceNumber = sequenceNumber;
    }

    internal static VendorTransaction CreateDeposit(ChangeBag bag, int amount, string description, DateTime date, Denomination? denomination = null)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            ChangeBag = bag,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(description) ? "両替金バッグ入金" : description,
            Denomination = denomination,
            CreatedAt = date
        };
    }

    internal static VendorTransaction CreateWithdrawal(ChangeBag bag, int amount, DateTime date)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            ChangeBag = bag,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Description = "両替金バッグ出金（レジへ移動）",
            CreatedAt = date
        };
    }

    internal static VendorTransaction CreateCashBagDeposit(CashBag bag, int amount, DateTime date, Denomination? denomination = null)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            CashBag = bag,
            Type = TransactionType.Deposit,
            Amount = amount,
            Description = string.IsNullOrWhiteSpace(bag.Description) ? "売上バッグ入金（レジから金庫へ）" : bag.Description,
            Denomination = denomination,
            CreatedAt = date
        };
    }

    public static VendorTransaction CreateAdjustment(ChangeBag bag, int difference, DateTime date)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            ChangeBag = bag,
            Type = TransactionType.Adjustment,
            Amount = difference,
            Description = $"両替金バッグ#{bag.Id} 有高調整（{(difference > 0 ? "+" : "")}{difference:N0}円）",
            CreatedAt = date
        };
    }

    public static VendorTransaction CreateAdjustment(CashBag bag, int difference, DateTime date)
    {
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            CashBag = bag,
            Type = TransactionType.Adjustment,
            Amount = difference,
            Description = $"売上バッグ#{bag.Id} 有高調整（{(difference > 0 ? "+" : "")}{difference:N0}円）",
            CreatedAt = date
        };
    }

    public static VendorTransaction CreateSafeAdjustment(int safeId, int difference, DateTime date)
    {
        return new VendorTransaction
        {
            SafeId = safeId,
            Type = TransactionType.Adjustment,
            Amount = difference,
            Description = $"金庫有高調整（{(difference > 0 ? "+" : "")}{difference:N0}円）",
            CreatedAt = date
        };
    }

    /// <summary>
    /// 赤伝取引を生成する。元取引の種別に応じて逆の種別・金額を決定する業務判断を含む。
    /// </summary>
    public static VendorTransaction CreateReversal(VendorTransaction original, string description, DateTime date)
    {
        var (reverseType, reverseAmount) = original.Type switch
        {
            TransactionType.Deposit    => (TransactionType.Withdrawal, original.Amount),
            TransactionType.Withdrawal => (TransactionType.Deposit,    original.Amount),
            TransactionType.Adjustment when original.Amount >= 0 => (TransactionType.Withdrawal, original.Amount),
            _                                                     => (TransactionType.Deposit,    Math.Abs(original.Amount)),
        };

        return new VendorTransaction
        {
            SafeId = original.SafeId,
            Type = reverseType,
            Amount = reverseAmount,
            Description = description,
            CreatedAt = date
        };
    }

    internal static VendorTransaction CreatePrepBagWithdrawal(PrepBag bag, int amount, DateTime date)
    {
        var ids = string.Join(", ", bag.CashBags.Select(cb => $"#{cb.Id}"));
        return new VendorTransaction
        {
            SafeId = bag.SafeId,
            PrepBag = bag,
            Type = TransactionType.Withdrawal,
            Amount = amount,
            Description = $"準備バッグ#{bag.Id} 引渡（{ids}）",
            CreatedAt = date
        };
    }
}
