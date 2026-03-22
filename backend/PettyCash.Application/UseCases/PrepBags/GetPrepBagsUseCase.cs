using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.PrepBags;

public class GetPrepBagsUseCase(IPrepBagRepository prepBagRepository)
{
    public async Task<IReadOnlyList<PrepBagDto>> ExecuteAsync(int safeId)
    {
        var bags = await prepBagRepository.GetBySafeIdAsync(safeId);

        return bags.Select(bag => new PrepBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.HandedOverAt,
            bag.CashBags.Select(cb => cb.Id).ToList()
        )).ToList();
    }
}
