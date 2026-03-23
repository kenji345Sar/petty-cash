using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.BagManagement;

namespace PettyCash.Application.UseCases.PrepBags;

public class CancelPrepBagUseCase(IPrepBagRepository prepBagRepository)
{
    public async Task<PrepBagDto> ExecuteAsync(int id)
    {
        var bag = await prepBagRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"準備バッグ(ID={id})が見つかりません。");

        bag.Cancel();
        await prepBagRepository.UpdateAsync(bag);

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
