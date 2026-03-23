using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.PettyCash.DenomCheck;

namespace PettyCash.Domain.Shared.Services;

public interface ISequenceNumberService
{
    Task AssignAsync(VendorTransaction transaction);
    Task AssignAsync(PettyCashTransaction transaction);
    Task AssignAsync(VendorDenominationCheck check);
    Task AssignAsync(PettyCashDenominationCheck check);
}
