using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Domain.PettyCash.Ledger;

/// <summary>
/// 小口取引。小口現金の入出金・調整の記録（小口出納帳の1行）。
///
/// 【取引種別】
///   - 入金（Deposit）: 金庫への小口現金の入金
///   - 出金（Withdrawal）: 金庫からの小口現金の出金
///   - 調整（Adjustment）: 有高チェックで差額が出た場合の自動調整
///
/// 【業務ルール】
///   - 金額は1円以上（金種指定時は金種合計が金額になる）
///   - 採番は金庫単位で一意、業者取引と通し番号を共有する
/// </summary>
public class PettyCashTransaction
{
    public int Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public int SafeId { get; private set; }
    public TransactionType Type { get; private set; }
    public int Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public Denomination? Denomination { get; private set; }

    /// <summary>この取引後の小口残高（ランニングバランス）</summary>
    public int Balance { get; private set; }

    private PettyCashTransaction() { }

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

    public static PettyCashTransaction Create(int safeId, TransactionType type, int amount, string description, DateTime date, Denomination? denomination = null)
    {
        var finalAmount = denomination?.TotalAmount ?? amount;
        if (finalAmount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        return new PettyCashTransaction
        {
            SafeId = safeId,
            Type = type,
            Amount = finalAmount,
            Description = description,
            Denomination = denomination,
            CreatedAt = date
        };
    }

    /// <summary>
    /// 赤伝取引を生成する。元取引の種別に応じて逆の種別・金額を決定する業務判断を含む。
    /// </summary>
    public static PettyCashTransaction CreateReversal(PettyCashTransaction original, string description, DateTime date)
    {
        var (reverseType, reverseAmount) = original.Type switch
        {
            TransactionType.Deposit    => (TransactionType.Withdrawal, original.Amount),
            TransactionType.Withdrawal => (TransactionType.Deposit,    original.Amount),
            TransactionType.Adjustment when original.Amount >= 0 => (TransactionType.Withdrawal, original.Amount),
            _                                                     => (TransactionType.Deposit,    Math.Abs(original.Amount)),
        };

        return new PettyCashTransaction
        {
            SafeId = original.SafeId,
            Type = reverseType,
            Amount = reverseAmount,
            Description = description,
            CreatedAt = date
        };
    }
}
