using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.PettyCash.Ledger;

namespace PettyCash.Domain.Shared.Services;

public interface IProjectionService
{
    Task ProjectAsync(VendorTransaction transaction);
    Task ProjectAsync(PettyCashTransaction transaction);
}
