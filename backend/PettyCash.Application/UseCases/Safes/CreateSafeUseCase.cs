using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.Safes;

public class CreateSafeUseCase(ISafeRepository safeRepository)
{
    public async Task<SafeDto> ExecuteAsync(CreateSafeRequestDto dto)
    {
        var safe = Safe.Create(dto.Name, dto.Description, DateTime.UtcNow);
        await safeRepository.AddAsync(safe);

        return new SafeDto(
            safe.Id,
            safe.Name,
            safe.Description,
            safe.CurrentBalance,
            safe.VendorBalance,
            safe.PettyCashBalance,
            safe.CreatedAt
        );
    }
}
