using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Domain.SafeAggregate;

public interface ISafeRepository
{
    Task<Safe?> GetByIdAsync(int id);
    Task<IReadOnlyList<Safe>> GetAllAsync();
    Task AddAsync(Safe safe);
    Task UpdateAsync(Safe safe);
}
