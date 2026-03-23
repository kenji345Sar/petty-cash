namespace PettyCash.Domain.PettyCash.DenomCheck;

public interface IPettyCashDenominationCheckRepository
{
    Task<PettyCashDenominationCheck?> GetByIdAsync(int id);
    Task<IReadOnlyList<PettyCashDenominationCheck>> GetBySafeIdAsync(int safeId);
    Task AddAsync(PettyCashDenominationCheck check);
    Task UpdateAsync(PettyCashDenominationCheck check);
}
