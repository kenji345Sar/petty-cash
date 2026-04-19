using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.Transactions;

public class GetPettyCashTransactionsUseCase(IPettyCashTransactionRepository repository)
{
    public async Task<IReadOnlyList<PettyCashTransactionDto>> ExecuteAsync(int safeId)
    {
        var transactions = await repository.GetBySafeIdAsync(safeId);

        return transactions.Select(t =>
        {
            var denomDto = t.Denomination is { } dn
                ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
                : null;

            return new PettyCashTransactionDto(
                t.Id, t.SequenceNumber,
                t.Type.ToString(), t.Amount, t.Balance, t.Description, t.CreatedAt, denomDto
            );
        }).ToList();
    }
}
