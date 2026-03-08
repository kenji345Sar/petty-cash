using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface IDenominationCheckRepository
{
    Task<DenominationCheck?> GetByIdAsync(int id);
    Task<DenominationCheck> AddAsync(DenominationCheck check);
    Task UpdateAsync(DenominationCheck check);
    Task<IReadOnlyList<DenominationCheck>> GetAllAsync();
}
