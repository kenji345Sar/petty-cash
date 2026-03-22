using PettyCash.Domain.Shared.DenomCheck;

namespace PettyCash.Domain.Shared.DenomCheck;

public interface IDenominationCheckRepository
{
    Task<DenominationCheck?> GetByIdAsync(int id);
    Task<DenominationCheck> AddAsync(DenominationCheck check);
    Task UpdateAsync(DenominationCheck check);
    Task<IReadOnlyList<DenominationCheck>> GetAllAsync();
    Task<IReadOnlyList<DenominationCheck>> GetBySafeIdAsync(int safeId);
}
