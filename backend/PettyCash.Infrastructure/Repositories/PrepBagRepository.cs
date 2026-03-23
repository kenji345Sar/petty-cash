using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class PrepBagRepository(PettyCashDbContext context) : IPrepBagRepository
{
    public async Task<PrepBag?> GetByIdAsync(int id)
    {
        return await context.PrepBags
            .Include(b => b.CashBags)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IReadOnlyList<PrepBag>> GetAllAsync()
    {
        return await context.PrepBags
            .Include(b => b.CashBags)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<PrepBag>> GetBySafeIdAsync(int safeId)
    {
        return await context.PrepBags
            .Include(b => b.CashBags)
            .Where(b => b.SafeId == safeId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(PrepBag bag)
    {
        context.PrepBags.Add(bag);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PrepBag bag)
    {
        await context.SaveChangesAsync();
    }
}
