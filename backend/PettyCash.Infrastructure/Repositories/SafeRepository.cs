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
        var balances = await context.Database
            .SqlQueryRaw<BalanceResult>(
                @"SELECT
                    COALESCE((SELECT SUM(CASE WHEN type = 0 THEN amount ELSE -amount END) FROM vendor_transactions WHERE safe_id = {0}), 0) AS ""VendorBalance"",
                    COALESCE((SELECT SUM(CASE WHEN type = 0 THEN amount ELSE -amount END) FROM petty_cash_transactions WHERE safe_id = {0}), 0) AS ""PettyCashBalance""",
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
