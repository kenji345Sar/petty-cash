using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface IChangeBagRepository
{
    Task<ChangeBag?> GetByIdAsync(int id);
    Task<IReadOnlyList<ChangeBag>> GetAllAsync();
    Task<IReadOnlyList<ChangeBag>> GetBySafeIdAsync(int safeId);
    Task AddAsync(ChangeBag bag);
    Task UpdateAsync(ChangeBag bag);
}
