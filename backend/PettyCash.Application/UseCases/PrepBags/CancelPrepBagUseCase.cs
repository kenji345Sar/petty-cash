using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.UseCases.PrepBags;

public class CancelPrepBagUseCase(IPrepBagRepository prepBagRepository, IUnitOfWork unitOfWork)
{
    public async Task<PrepBagDto> ExecuteAsync(int id)
    {
        var bag = await prepBagRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"準備バッグ(ID={id})が見つかりません。");

        bag.Cancel();
        await prepBagRepository.UpdateAsync(bag);
        await unitOfWork.SaveChangesAsync();

        return new PrepBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.HandedOverAt,
            bag.CashBags.Select(cb => cb.Id).ToList()
        );
    }
}
