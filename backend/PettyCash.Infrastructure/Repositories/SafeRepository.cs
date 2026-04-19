using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Repositories;

public class SafeRepository(PettyCashDbContext context) : ISafeRepository
{
    public async Task<Safe?> GetByIdAsync(int id)
    {
        var safe = await context.Safes
            .FirstOrDefaultAsync(s => s.Id == id);
        if (safe != null)
            await LoadBalances(safe);
        return safe;
    }

    public async Task<IReadOnlyList<Safe>> GetAllAsync()
    {
        var safes = await context.Safes
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        foreach (var safe in safes)
            await LoadBalances(safe);
        return safes;
    }

    public Task AddAsync(Safe safe)
    {
        context.Safes.Add(safe);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Safe safe)
    {
        // EF Core tracks changes automatically
        return Task.CompletedTask;
    }

    private async Task LoadBalances(Safe safe)
    {
        var balance = await context.SafeBalances
            .FirstOrDefaultAsync(b => b.SafeId == safe.Id);

        if (balance != null)
            safe.SetBalances(balance.VendorBalance, balance.PettyCashBalance);
        else
            safe.SetBalances(0, 0);
    }
}
