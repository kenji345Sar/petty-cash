using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class TransactionRepository(PettyCashDbContext context) : ITransactionRepository
{
    public async Task<IReadOnlyList<Transaction>> GetAllAsync()
    {
        return await context.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetBySafeIdAsync(int safeId)
    {
        return await context.Transactions
            .Where(t => t.SafeId == safeId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Transaction transaction)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
    }
}
