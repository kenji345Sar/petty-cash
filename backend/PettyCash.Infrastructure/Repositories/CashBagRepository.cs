using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class CashBagRepository(PettyCashDbContext context) : ICashBagRepository
{
    public async Task<CashBag?> GetByIdAsync(int id)
    {
        return await context.CashBags
            .Include(b => b.Transaction)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IReadOnlyList<CashBag>> GetAllAsync()
    {
        return await context.CashBags
            .Include(b => b.Transaction)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CashBag>> GetBySafeIdAsync(int safeId)
    {
        return await context.CashBags
            .Include(b => b.Transaction)
            .Where(b => b.SafeId == safeId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public Task AddAsync(CashBag bag)
    {
        context.CashBags.Add(bag);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CashBag bag)
    {
        // EF Core tracks changes automatically
        return Task.CompletedTask;
    }
}
