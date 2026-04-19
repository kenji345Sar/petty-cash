using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Queries;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Queries;

public class PettyCashLedgerQueryService(PettyCashDbContext context) : IPettyCashLedgerQueryService
{
    public async Task<IReadOnlyList<PettyCashTransactionDto>> GetBySafeIdAsync(int safeId)
    {
        var entries = await context.PettyCashLedgerEntries
            .Where(e => e.SafeId == safeId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return entries.Select(e => new PettyCashTransactionDto(
            e.Id, e.SequenceNumber,
            ((Domain.Shared.TransactionType)e.Type).ToString(),
            e.Amount, e.Balance, e.Description, e.CreatedAt, null
        )).ToList();
    }
}
