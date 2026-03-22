using PettyCash.Domain.Vendor.BagManagement;

namespace PettyCash.Domain.Vendor.BagManagement;

public interface IChangeBagRepository
{
    Task<ChangeBag?> GetByIdAsync(int id);
    Task<IReadOnlyList<ChangeBag>> GetAllAsync();
    Task<IReadOnlyList<ChangeBag>> GetBySafeIdAsync(int safeId);
    Task AddAsync(ChangeBag bag);
    Task UpdateAsync(ChangeBag bag);
}
