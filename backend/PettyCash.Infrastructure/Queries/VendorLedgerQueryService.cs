using Microsoft.EntityFrameworkCore;
using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Queries;
using PettyCash.Infrastructure.Data;

namespace PettyCash.Infrastructure.Queries;

public class VendorLedgerQueryService(PettyCashDbContext context) : IVendorLedgerQueryService
{
    public async Task<IReadOnlyList<VendorTransactionDto>> GetBySafeIdAsync(int safeId)
    {
        var entries = await context.VendorTransactions
            .Where(t => t.SafeId == safeId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return entries.Select(e =>
        {
            var dn = e.Denomination;
            var denomDto = dn != null
                ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
                : null;
            return new VendorTransactionDto(
                e.Id, e.SequenceNumber, e.ChangeBagId, e.CashBagId, e.PrepBagId,
                e.Type.ToString(), e.Amount, e.Balance, e.Description, e.CreatedAt, denomDto);
        }).ToList();
    }
}
