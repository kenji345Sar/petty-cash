using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.CashBags;

public class GetCashBagsUseCase(ICashBagRepository cashBagRepository)
{
    public async Task<IReadOnlyList<CashBagDto>> ExecuteAsync(int safeId)
    {
        var bags = await cashBagRepository.GetBySafeIdAsync(safeId);

        return bags.Select(bag => new CashBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Description,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.MovedAt,
            bag.Transaction?.SequenceNumber
        )).ToList();
    }
}
