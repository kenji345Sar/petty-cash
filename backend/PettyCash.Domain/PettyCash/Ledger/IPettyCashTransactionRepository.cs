using PettyCash.Domain.PettyCash.Ledger;

namespace PettyCash.Domain.PettyCash.Ledger;

public interface IPettyCashTransactionRepository
{
    Task<IReadOnlyList<PettyCashTransaction>> GetBySafeIdAsync(int safeId);
    Task AddAsync(PettyCashTransaction transaction);
}
