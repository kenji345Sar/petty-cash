using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Queries;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Queries;

public class PettyCashLedgerQueryService(PettyCashDbContext context) : IPettyCashLedgerQueryService
{
    public async Task<IReadOnlyList<PettyCashTransactionDto>> GetBySafeIdAsync(int safeId)
    {
        var entries = await context.PettyCashTransactions
            .Where(t => t.SafeId == safeId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return entries.Select(e =>
        {
            var dn = e.Denomination;
            var denomDto = dn != null
                ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
                : null;
            return new PettyCashTransactionDto(
                e.Id, e.SequenceNumber,
                e.Type.ToString(), e.Amount, e.Balance, e.Description, e.CreatedAt, denomDto);
        }).ToList();
    }
}
