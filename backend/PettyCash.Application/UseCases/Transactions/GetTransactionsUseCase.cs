using PettyCash.Application.Dtos;
using PettyCash.Domain.Repositories;

namespace PettyCash.Application.UseCases.Transactions;

public class GetTransactionsUseCase(ITransactionRepository transactionRepository)
{
    public async Task<IReadOnlyList<TransactionDto>> ExecuteAsync(int safeId)
    {
        var transactions = await transactionRepository.GetBySafeIdAsync(safeId);

        return transactions.Select(t => new TransactionDto(
            t.Id,
            t.ChangeBagId,
            t.CashBagId,
            t.PrepBagId,
            t.Type.ToString(),
            t.Amount,
            t.Description,
            t.CreatedAt
        )).ToList();
    }
}
