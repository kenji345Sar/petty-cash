using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class VendorDenominationCheckRepository(PettyCashDbContext context) : IVendorDenominationCheckRepository
{
    public async Task<VendorDenominationCheck?> GetByIdAsync(int id)
    {
        return await context.VendorDenominationChecks.FindAsync(id);
    }

    public async Task<IReadOnlyList<VendorDenominationCheck>> GetBySafeIdAsync(int safeId)
    {
        return await context.VendorDenominationChecks
            .Where(c => c.SafeId == safeId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public Task AddAsync(VendorDenominationCheck check)
    {
        context.VendorDenominationChecks.Add(check);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(VendorDenominationCheck check)
    {
        // EF Core tracks changes automatically
        return Task.CompletedTask;
    }
}
