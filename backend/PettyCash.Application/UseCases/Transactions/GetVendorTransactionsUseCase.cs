using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.Transactions;

public class GetVendorTransactionsUseCase(IVendorTransactionRepository repository)
{
    public async Task<IReadOnlyList<VendorTransactionDto>> ExecuteAsync(int safeId)
    {
        var transactions = await repository.GetBySafeIdAsync(safeId);

        return transactions.Select(t =>
        {
            var denomDto = t.Denomination is { } dn
                ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
                : null;

            return new VendorTransactionDto(
                t.Id, t.SequenceNumber, t.ChangeBagId, t.CashBagId, t.PrepBagId,
                t.Type.ToString(), t.Amount, t.Description, t.CreatedAt, denomDto
            );
        }).ToList();
    }
}
