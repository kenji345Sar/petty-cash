using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class VendorTransactionRepository(PettyCashDbContext context) : IVendorTransactionRepository
{
    public async Task<IReadOnlyList<VendorTransaction>> GetBySafeIdAsync(int safeId)
    {
        return await context.VendorTransactions
            .Where(t => t.SafeId == safeId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<VendorTransaction?> GetByIdAsync(int id)
    {
        return await context.VendorTransactions.FirstOrDefaultAsync(t => t.Id == id);
    }

    public Task AddAsync(VendorTransaction transaction)
    {
        context.VendorTransactions.Add(transaction);
        return Task.CompletedTask;
    }
}
