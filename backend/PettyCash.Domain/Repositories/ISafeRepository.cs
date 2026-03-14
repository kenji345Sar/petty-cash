using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface ISafeRepository
{
    Task<Safe?> GetByIdAsync(int id);
    Task<IReadOnlyList<Safe>> GetAllAsync();
    Task AddAsync(Safe safe);
    Task UpdateAsync(Safe safe);
}
