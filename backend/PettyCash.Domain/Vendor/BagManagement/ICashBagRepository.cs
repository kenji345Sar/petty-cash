using PettyCash.Domain.Vendor.BagManagement;

namespace PettyCash.Domain.Vendor.BagManagement;

public interface ICashBagRepository
{
    Task<CashBag?> GetByIdAsync(int id);
    Task<IReadOnlyList<CashBag>> GetAllAsync();
    Task<IReadOnlyList<CashBag>> GetBySafeIdAsync(int safeId);
    Task AddAsync(CashBag bag);
    Task UpdateAsync(CashBag bag);
}
