using PettyCash.Domain.Vendor.BagManagement;

namespace PettyCash.Domain.Vendor.BagManagement;

public interface IPrepBagRepository
{
    Task<PrepBag?> GetByIdAsync(int id);
    Task<IReadOnlyList<PrepBag>> GetAllAsync();
    Task<IReadOnlyList<PrepBag>> GetBySafeIdAsync(int safeId);
    Task AddAsync(PrepBag bag);
    Task UpdateAsync(PrepBag bag);
}
