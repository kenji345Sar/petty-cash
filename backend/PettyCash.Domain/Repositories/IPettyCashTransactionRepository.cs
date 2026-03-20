using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface IPettyCashTransactionRepository
{
    Task<IReadOnlyList<PettyCashTransaction>> GetBySafeIdAsync(int safeId);
    Task AddAsync(PettyCashTransaction transaction);
}
