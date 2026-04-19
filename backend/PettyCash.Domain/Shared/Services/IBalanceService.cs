using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.PettyCash.Ledger;

namespace PettyCash.Domain.Shared.Services;

public interface IBalanceService
{
    Task AssignBalanceAsync(VendorTransaction transaction);
    Task AssignBalanceAsync(PettyCashTransaction transaction);
}
