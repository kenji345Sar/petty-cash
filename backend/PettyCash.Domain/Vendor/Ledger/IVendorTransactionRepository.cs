using PettyCash.Domain.Vendor.Ledger;

namespace PettyCash.Domain.Vendor.Ledger;

public interface IVendorTransactionRepository
{
    Task<IReadOnlyList<VendorTransaction>> GetBySafeIdAsync(int safeId);
    Task AddAsync(VendorTransaction transaction);
}
