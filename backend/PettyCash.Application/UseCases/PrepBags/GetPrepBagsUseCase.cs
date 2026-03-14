using PettyCash.Application.Dtos;
using PettyCash.Domain.Repositories;

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
