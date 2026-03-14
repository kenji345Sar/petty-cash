using PettyCash.Application.Dtos;
using PettyCash.Domain.Repositories;

namespace PettyCash.Application.UseCases.Bags;

public class GetBagsUseCase(IChangeBagRepository bagRepository)
{
    public async Task<IReadOnlyList<ChangeBagDto>> ExecuteAsync(int safeId)
    {
        var bags = await bagRepository.GetBySafeIdAsync(safeId);

        return bags.Select(bag => new ChangeBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Description,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.MovedAt
        )).ToList();
    }
}
