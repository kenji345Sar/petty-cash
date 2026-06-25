using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Vendor.Ledger;

namespace PettyCash.Domain.Vendor.BagManagement;

/// <summary>
/// 準備バッグ。複数のキャッシュバッグをまとめて業者へ引き渡すための単位。
///
/// 【ライフサイクル】
///   作成（複数CashBagをまとめる）→ 引渡 or 取消
///
/// 【業務ルール】
///   - 1つ以上のキャッシュバッグが必要
///   - 既に別の準備バッグに含まれているキャッシュバッグは追加できない
///   - 合計金額は含まれるキャッシュバッグの合計から自動算出される
///   - 引渡時に出金取引（VendorTransaction）が自動生成される
///   - 引渡済みの準備バッグは取消できない
///   - 取消するとキャッシュバッグとの紐づけが解除され、再割り当て可能になる
/// </summary>
public class PrepBag
{
    public int Id { get; private set; }
    public int SafeId { get; private set; }
    public Safe? Safe { get; private set; }
    public int TotalAmount { get; private set; }
    public PrepBagStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? HandedOverAt { get; private set; }

    public VendorTransaction? Transaction { get; private set; }

    private readonly List<VendorDenominationCheck> _denominationChecks = [];
    public IReadOnlyCollection<VendorDenominationCheck> DenominationChecks => _denominationChecks.AsReadOnly();

    private readonly List<CashBag> _cashBags = [];
    public IReadOnlyCollection<CashBag> CashBags => _cashBags.AsReadOnly();

    private PrepBag() { }

    public static PrepBag Create(int safeId, IReadOnlyList<CashBag> cashBags, DateTime date)
    {
        if (cashBags.Count == 0)
            throw new ArgumentException("キャッシュバッグを1つ以上選択してください。");

        foreach (var cashBag in cashBags)
        {
            if (cashBag.PrepBagId != null)
                throw new InvalidOperationException($"キャッシュバッグ(ID={cashBag.Id})は既に準備バッグに含まれています。");
        }

        var bag = new PrepBag
        {
            SafeId = safeId,
            TotalAmount = cashBags.Sum(b => b.TotalAmount),
            Status = PrepBagStatus.Preparing,
            CreatedAt = date
        };

        foreach (var cashBag in cashBags)
        {
            bag._cashBags.Add(cashBag);
        }

        return bag;
    }

    public void Cancel()
    {
        if (Status == PrepBagStatus.HandedOver)
            throw new InvalidOperationException("引渡済みの準備バッグは戻せません。");
        if (Status == PrepBagStatus.Cancelled)
            throw new InvalidOperationException("この準備バッグは既に取消済みです。");

        _cashBags.Clear();
        Status = PrepBagStatus.Cancelled;
    }

    public VendorTransaction MarkHandedOver(DateTime handedOverAt)
    {
        if (Status == PrepBagStatus.HandedOver)
            throw new InvalidOperationException("この準備バッグは既に引渡済みです。");
        if (Status == PrepBagStatus.Cancelled)
            throw new InvalidOperationException("取消済みの準備バッグは引渡できません。");

        Status = PrepBagStatus.HandedOver;
        HandedOverAt = handedOverAt;

        var tx = VendorTransaction.CreatePrepBagWithdrawal(this, TotalAmount, handedOverAt);
        Transaction = tx;
        return tx;
    }
}
