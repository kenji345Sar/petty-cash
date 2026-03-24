using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class ChangeBagRepository(PettyCashDbContext context) : IChangeBagRepository
{
    public async Task<ChangeBag?> GetByIdAsync(int id)
    {
        return await context.ChangeBags
            .Include("_transactions")
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IReadOnlyList<ChangeBag>> GetAllAsync()
    {
        return await context.ChangeBags
            .Include("_transactions")
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ChangeBag>> GetBySafeIdAsync(int safeId)
    {
        return await context.ChangeBags
            .Include("_transactions")
            .Where(b => b.SafeId == safeId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public Task AddAsync(ChangeBag bag)
    {
        context.ChangeBags.Add(bag);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ChangeBag bag)
    {
        // EF Core tracks changes automatically
        return Task.CompletedTask;
    }
}
