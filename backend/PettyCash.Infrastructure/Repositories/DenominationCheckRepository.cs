using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class DenominationCheckRepository(PettyCashDbContext context) : IDenominationCheckRepository
{
    public async Task<DenominationCheck?> GetByIdAsync(int id)
    {
        return await context.DenominationChecks.FindAsync(id);
    }

    public async Task UpdateAsync(DenominationCheck check)
    {
        await context.SaveChangesAsync();
    }

    public async Task<DenominationCheck> AddAsync(DenominationCheck check)
    {
        context.DenominationChecks.Add(check);
        await context.SaveChangesAsync();
        return check;
    }

    public async Task<IReadOnlyList<DenominationCheck>> GetAllAsync()
    {
        return await context.DenominationChecks
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<DenominationCheck>> GetBySafeIdAsync(int safeId)
    {
        return await context.DenominationChecks
            .Where(c => c.SafeId == safeId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
