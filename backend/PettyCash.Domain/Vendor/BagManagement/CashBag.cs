using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.ValueObjects;
using PettyCash.Domain.Vendor.Ledger;

namespace PettyCash.Domain.Vendor.BagManagement;

/// <summary>
/// 売上バッグ。レジの売上金を金庫へ入金するための袋。
///
/// 【ライフサイクル】
///   レジから金庫へ入金 → 準備バッグにまとめられる → 業者へ引渡
///
/// 【業務ルール】
///   - 入金時は金種表の入力が必須（金額直接入力は不可）
///   - 金額は金種の合計から自動算出される
///   - 1つの売上バッグは1つの準備バッグにしか含められない
///   - 入金時に業者取引（VendorTransaction）が自動生成される
/// </summary>
public class CashBag
{
    public int Id { get; private set; }
    public int SafeId { get; private set; }
    public Safe? Safe { get; private set; }
    public int TotalAmount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public CashBagStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? MovedAt { get; private set; }

    public int? PrepBagId { get; private set; }
    public PrepBag? PrepBag { get; private set; }

    public VendorTransaction? Transaction { get; private set; }

    private readonly List<VendorDenominationCheck> _denominationChecks = [];
    public IReadOnlyCollection<VendorDenominationCheck> DenominationChecks => _denominationChecks.AsReadOnly();

    private CashBag() { }

    public static CashBag CreateDeposit(int safeId, int amount, string description, DateTime date, Denomination? denomination = null)
    {
        if (denomination == null)
            throw new ArgumentException("金種表の入力が必須です。");

        var finalAmount = denomination.TotalAmount;
        if (finalAmount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        var bag = new CashBag
        {
            SafeId = safeId,
            TotalAmount = finalAmount,
            Description = description,
            Status = CashBagStatus.MovedToSafe,
            CreatedAt = date,
            MovedAt = date
        };

        bag.Transaction = VendorTransaction.CreateCashBagDeposit(bag, finalAmount, date, denomination);

        return bag;
    }

    public void UpdateAmount(int newAmount)
    {
        TotalAmount = newAmount;
    }
}
