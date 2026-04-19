using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.Services;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Services;

public class BalanceService(PettyCashDbContext context) : IBalanceService
{
    public async Task AssignBalanceAsync(VendorTransaction transaction)
    {
        var currentBalance = await GetLatestVendorBalanceAsync(transaction.SafeId);
        var newBalance = currentBalance + CalculateImpact(transaction.Type, transaction.Amount);
        transaction.SetBalance(newBalance);
    }

    public async Task AssignBalanceAsync(PettyCashTransaction transaction)
    {
        var currentBalance = await GetLatestPettyCashBalanceAsync(transaction.SafeId);
        var newBalance = currentBalance + CalculateImpact(transaction.Type, transaction.Amount);
        transaction.SetBalance(newBalance);
    }

    private async Task<int> GetLatestVendorBalanceAsync(int safeId)
    {
        return await context.Database
            .SqlQueryRaw<int>(
                @"SELECT COALESCE(
                    (SELECT balance FROM vendor_transactions
                     WHERE safe_id = {0} ORDER BY created_at DESC, id DESC LIMIT 1),
                    0) AS ""Value""",
                safeId)
            .FirstAsync();
    }

    private async Task<int> GetLatestPettyCashBalanceAsync(int safeId)
    {
        return await context.Database
            .SqlQueryRaw<int>(
                @"SELECT COALESCE(
                    (SELECT balance FROM petty_cash_transactions
                     WHERE safe_id = {0} ORDER BY created_at DESC, id DESC LIMIT 1),
                    0) AS ""Value""",
                safeId)
            .FirstAsync();
    }

    private static int CalculateImpact(TransactionType type, int amount)
    {
        return type == TransactionType.Withdrawal ? -amount : amount;
    }
}
