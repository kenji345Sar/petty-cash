using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
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

    public async Task AddAsync(ChangeBag bag)
    {
        context.ChangeBags.Add(bag);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ChangeBag bag)
    {
        await context.SaveChangesAsync();
    }
}
