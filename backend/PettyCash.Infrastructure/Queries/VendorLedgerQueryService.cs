using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Queries;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Queries;

public class VendorLedgerQueryService(PettyCashDbContext context) : IVendorLedgerQueryService
{
    public async Task<IReadOnlyList<VendorTransactionDto>> GetBySafeIdAsync(int safeId)
    {
        var entries = await context.VendorLedgerEntries
            .Where(e => e.SafeId == safeId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return entries.Select(e => new VendorTransactionDto(
            e.Id, e.SequenceNumber, e.ChangeBagId, e.CashBagId, e.PrepBagId,
            ((Domain.Shared.TransactionType)e.Type).ToString(),
            e.Amount, e.Balance, e.Description, e.CreatedAt, null
        )).ToList();
    }
}
