using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllAsync();
    Task<IReadOnlyList<Transaction>> GetBySafeIdAsync(int safeId);
    Task AddAsync(Transaction transaction);
}
