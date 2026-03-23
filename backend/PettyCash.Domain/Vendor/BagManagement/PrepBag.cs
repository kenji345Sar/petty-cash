using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Vendor.Ledger;

namespace PettyCash.Domain.Vendor.BagManagement;

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

        Status = PrepBagStatus.HandedOver;
        HandedOverAt = handedOverAt;

        var tx = VendorTransaction.CreatePrepBagWithdrawal(this, TotalAmount, handedOverAt);
        Transaction = tx;
        return tx;
    }
}
