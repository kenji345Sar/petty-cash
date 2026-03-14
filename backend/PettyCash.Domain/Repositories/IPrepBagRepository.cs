using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface IPrepBagRepository
{
    Task<PrepBag?> GetByIdAsync(int id);
    Task<IReadOnlyList<PrepBag>> GetAllAsync();
    Task<IReadOnlyList<PrepBag>> GetBySafeIdAsync(int safeId);
    Task AddAsync(PrepBag bag);
    Task UpdateAsync(PrepBag bag);
}
