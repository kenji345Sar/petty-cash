using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
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

    public async Task AddAsync(Safe safe)
    {
        context.Safes.Add(safe);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Safe safe)
    {
        await context.SaveChangesAsync();
    }

    private async Task LoadBalances(Safe safe)
    {
        // HasBag = change_bag_id IS NOT NULL OR cash_bag_id IS NOT NULL OR prep_bag_id IS NOT NULL
        // Deposit = 0, Withdrawal = 1
        var balances = await context.Database
            .SqlQueryRaw<BalanceResult>(
                @"SELECT
                    COALESCE(SUM(CASE WHEN (change_bag_id IS NOT NULL OR cash_bag_id IS NOT NULL OR prep_bag_id IS NOT NULL) AND type = 0 THEN amount ELSE 0 END), 0)
                    - COALESCE(SUM(CASE WHEN (change_bag_id IS NOT NULL OR cash_bag_id IS NOT NULL OR prep_bag_id IS NOT NULL) AND type = 1 THEN amount ELSE 0 END), 0)
                    AS ""VendorBalance"",
                    COALESCE(SUM(CASE WHEN (change_bag_id IS NULL AND cash_bag_id IS NULL AND prep_bag_id IS NULL) AND type = 0 THEN amount ELSE 0 END), 0)
                    - COALESCE(SUM(CASE WHEN (change_bag_id IS NULL AND cash_bag_id IS NULL AND prep_bag_id IS NULL) AND type = 1 THEN amount ELSE 0 END), 0)
                    AS ""PettyCashBalance""
                FROM transactions WHERE safe_id = {0}",
                safe.Id)
            .FirstAsync();

        safe.SetBalances(balances.VendorBalance, balances.PettyCashBalance);
    }
}

internal class BalanceResult
{
    public int VendorBalance { get; set; }
    public int PettyCashBalance { get; set; }
}
