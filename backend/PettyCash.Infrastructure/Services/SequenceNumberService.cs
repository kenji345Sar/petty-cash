using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.PettyCash.DenomCheck;
using PettyCash.Domain.Shared.Services;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Services;

public class SequenceNumberService(PettyCashDbContext context) : ISequenceNumberService
{
    public async Task AssignAsync(VendorTransaction transaction)
    {
        var seq = await GetNextAsync(transaction.SafeId);
        transaction.SetSequenceNumber(seq);
    }

    public async Task AssignAsync(PettyCashTransaction transaction)
    {
        var seq = await GetNextAsync(transaction.SafeId);
        transaction.SetSequenceNumber(seq);
    }

    public async Task AssignAsync(VendorDenominationCheck check)
    {
        var seq = await GetNextAsync(check.SafeId);
        check.SetSequenceNumber(seq);
    }

    public async Task AssignAsync(PettyCashDenominationCheck check)
    {
        var seq = await GetNextAsync(check.SafeId);
        check.SetSequenceNumber(seq);
    }

    private async Task<int> GetNextAsync(int safeId)
    {
        return await context.Database
            .SqlQueryRaw<int>(
                @"SELECT GREATEST(
                    COALESCE((SELECT MAX(sequence_number) FROM vendor_transactions WHERE safe_id = {0}), 0),
                    COALESCE((SELECT MAX(sequence_number) FROM petty_cash_transactions WHERE safe_id = {0}), 0),
                    COALESCE((SELECT MAX(sequence_number) FROM vendor_denomination_checks WHERE safe_id = {0}), 0),
                    COALESCE((SELECT MAX(sequence_number) FROM safe_denomination_checks WHERE safe_id = {0}), 0)
                ) + 1 AS ""Value""",
                safeId)
            .FirstAsync();
    }
}
