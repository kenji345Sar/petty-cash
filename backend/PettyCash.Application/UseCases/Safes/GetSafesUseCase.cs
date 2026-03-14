using PettyCash.Application.Dtos;
using PettyCash.Domain.Repositories;

namespace PettyCash.Application.UseCases.Safes;

public class GetSafesUseCase(ISafeRepository safeRepository)
{
    public async Task<IReadOnlyList<SafeDto>> ExecuteAsync()
    {
        var safes = await safeRepository.GetAllAsync();
        return safes.Select(s => new SafeDto(
            s.Id,
            s.Name,
            s.Description,
            s.CurrentBalance,
            s.CreatedAt
        )).ToList();
    }
}
