using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.ValueObjects;
using PettyCash.Domain.Vendor.BagManagement;

namespace PettyCash.Domain.Vendor.DenomCheck;

/// <summary>
/// 業者側の有高チェック。両替金バッグ・売上バッグ・準備バッグの実際の金種を数えて帳簿と照合する。
///
/// 【業務ルール】
///   - チェック対象はChangeBag / CashBag / PrepBagのいずれか1つ
///   - 帳簿金額（ExpectedAmount）はバッグのTotalAmount
///   - 実際の金額（CheckedAmount）は入力された金種の合計
///   - 差額（Difference）= 実際 - 帳簿（プラスなら過剰、マイナスなら不足）
///   - 差額がある場合、UseCaseで調整取引が自動生成されバッグ金額が更新される
/// </summary>
public class VendorDenominationCheck
{
    public int Id { get; private set; }
    public int SequenceNumber { get; private set; }
    public int SafeId { get; private set; }
    public int? ChangeBagId { get; private set; }
    public int? CashBagId { get; private set; }
    public int? PrepBagId { get; private set; }
    public Denomination Denomination { get; private set; } = null!;
    public int CheckedAmount { get; private set; }
    public int ExpectedAmount { get; private set; }
    public int Difference { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ChangeBag? ChangeBag { get; private set; }
    public CashBag? CashBag { get; private set; }
    public PrepBag? PrepBag { get; private set; }

    private VendorDenominationCheck() { }

    internal void SetSequenceNumber(int sequenceNumber)
    {
        if (SequenceNumber != 0)
            throw new InvalidOperationException("採番済みのため、番号を変更できません。");
        if (sequenceNumber <= 0)
            throw new ArgumentException("採番は1以上である必要があります。");
        SequenceNumber = sequenceNumber;
    }

    public static VendorDenominationCheck CreateForChangeBag(ChangeBag bag, Denomination denomination, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new VendorDenominationCheck
        {
            SafeId = bag.SafeId,
            ChangeBagId = bag.Id,
            ChangeBag = bag,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = bag.TotalAmount,
            Difference = checkedAmount - bag.TotalAmount,
            CreatedAt = checkedAt
        };
    }

    public void Update(Denomination denomination, int expectedAmount)
    {
        Denomination = denomination;
        CheckedAmount = denomination.TotalAmount;
        ExpectedAmount = expectedAmount;
        Difference = CheckedAmount - expectedAmount;
    }

    public static VendorDenominationCheck CreateForCashBag(CashBag bag, Denomination denomination, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new VendorDenominationCheck
        {
            SafeId = bag.SafeId,
            CashBagId = bag.Id,
            CashBag = bag,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = bag.TotalAmount,
            Difference = checkedAmount - bag.TotalAmount,
            CreatedAt = checkedAt
        };
    }

    public static VendorDenominationCheck CreateForPrepBag(PrepBag bag, Denomination denomination, DateTime checkedAt)
    {
        var checkedAmount = denomination.TotalAmount;
        return new VendorDenominationCheck
        {
            SafeId = bag.SafeId,
            PrepBagId = bag.Id,
            PrepBag = bag,
            Denomination = denomination,
            CheckedAmount = checkedAmount,
            ExpectedAmount = bag.TotalAmount,
            Difference = checkedAmount - bag.TotalAmount,
            CreatedAt = checkedAt
        };
    }
}
