using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.PettyCash.DenomCheck;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class PettyCashDenominationCheckRepository(PettyCashDbContext context) : IPettyCashDenominationCheckRepository
{
    public async Task<PettyCashDenominationCheck?> GetByIdAsync(int id)
    {
        return await context.PettyCashDenominationChecks.FindAsync(id);
    }

    public async Task<IReadOnlyList<PettyCashDenominationCheck>> GetBySafeIdAsync(int safeId)
    {
        return await context.PettyCashDenominationChecks
            .Where(c => c.SafeId == safeId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(PettyCashDenominationCheck check)
    {
        context.PettyCashDenominationChecks.Add(check);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PettyCashDenominationCheck check)
    {
        await context.SaveChangesAsync();
    }
}
