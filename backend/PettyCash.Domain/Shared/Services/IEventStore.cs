using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.PettyCash.Ledger;

namespace PettyCash.Domain.Shared.Services;

public interface IEventStore
{
    Task AppendAsync(VendorTransaction transaction);
    Task AppendAsync(PettyCashTransaction transaction);
}
