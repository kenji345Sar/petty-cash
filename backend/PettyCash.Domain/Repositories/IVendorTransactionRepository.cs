using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface IVendorTransactionRepository
{
    Task<IReadOnlyList<VendorTransaction>> GetBySafeIdAsync(int safeId);
    Task AddAsync(VendorTransaction transaction);
}
