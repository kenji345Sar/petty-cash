using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.Dashboard;

public class GetPettyCashDashboardUseCase(
    ISafeRepository safeRepository,
    GetPettyCashTransactionsUseCase getTransactions,
    GetPettyCashDenominationChecksUseCase getDenomChecks)
{
    public async Task<PettyCashDashboardDto> ExecuteAsync(int safeId)
    {
        var safe = await safeRepository.GetByIdAsync(safeId)
            ?? throw new KeyNotFoundException($"金庫ID {safeId} が見つかりません。");

        var transactions = await getTransactions.ExecuteAsync(safeId);
        var denomChecks = await getDenomChecks.ExecuteAsync(safeId);

        var safeDto = new SafeDto(
            safe.Id, safe.Name, safe.Description,
            safe.CurrentBalance, safe.VendorBalance, safe.PettyCashBalance,
            safe.CreatedAt
        );

        return new PettyCashDashboardDto(safeDto, transactions, denomChecks);
    }
}
