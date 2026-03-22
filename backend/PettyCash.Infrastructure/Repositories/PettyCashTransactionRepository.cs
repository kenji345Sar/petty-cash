using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class PettyCashTransactionRepository(PettyCashDbContext context) : IPettyCashTransactionRepository
{
    public async Task<IReadOnlyList<PettyCashTransaction>> GetBySafeIdAsync(int safeId)
    {
        return await context.PettyCashTransactions
            .Where(t => t.SafeId == safeId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(PettyCashTransaction transaction)
    {
        context.PettyCashTransactions.Add(transaction);
        await context.SaveChangesAsync();
    }
}
