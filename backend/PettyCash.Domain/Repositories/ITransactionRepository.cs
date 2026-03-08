using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Repositories;

public interface ITransactionRepository
{
    Task<IReadOnlyList<Transaction>> GetAllAsync();
    Task AddAsync(Transaction transaction);
}
