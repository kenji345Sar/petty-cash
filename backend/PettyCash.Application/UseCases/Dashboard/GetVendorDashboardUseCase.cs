using PettyCash.Application.Dtos;
using PettyCash.Application.UseCases.Bags;
using PettyCash.Application.UseCases.CashBags;
using PettyCash.Application.UseCases.Transactions;
using PettyCash.Application.UseCases.DenominationChecks;
using PettyCash.Application.UseCases.PrepBags;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.Dashboard;

public class GetVendorDashboardUseCase(
    ISafeRepository safeRepository,
    GetBagsUseCase getBags,
    GetCashBagsUseCase getCashBags,
    GetVendorTransactionsUseCase getTransactions,
    GetVendorDenominationChecksUseCase getDenomChecks,
    GetPrepBagsUseCase getPrepBags)
{
    public async Task<VendorDashboardDto> ExecuteAsync(int safeId)
    {
        var safe = await safeRepository.GetByIdAsync(safeId)
            ?? throw new KeyNotFoundException($"金庫ID {safeId} が見つかりません。");

        var bags = await getBags.ExecuteAsync(safeId);
        var cashBags = await getCashBags.ExecuteAsync(safeId);
        var transactions = await getTransactions.ExecuteAsync(safeId);
        var denomChecks = await getDenomChecks.ExecuteAsync(safeId);
        var prepBags = await getPrepBags.ExecuteAsync(safeId);

        var safeDto = new SafeDto(
            safe.Id, safe.Name, safe.Description,
            safe.CurrentBalance, safe.VendorBalance, safe.PettyCashBalance,
            safe.CreatedAt
        );

        return new VendorDashboardDto(safeDto, bags, cashBags, transactions, denomChecks, prepBags);
    }
}
