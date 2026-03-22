using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;

namespace PettyCash.Domain.Shared.Services;

public interface ISequenceNumberService
{
    Task AssignAsync(VendorTransaction transaction);
    Task AssignAsync(PettyCashTransaction transaction);
    Task AssignAsync(DenominationCheck check);
}
