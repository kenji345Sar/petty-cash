using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.PrepBags;

public class CreatePrepBagUseCase(ICashBagRepository cashBagRepository, IPrepBagRepository prepBagRepository)
{
    public async Task<PrepBagDto> ExecuteAsync(CreatePrepBagRequestDto dto)
    {
        var cashBags = new List<CashBag>();
        foreach (var id in dto.CashBagIds)
        {
            var cashBag = await cashBagRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"キャッシュバッグ(ID={id})が見つかりません。");
            cashBags.Add(cashBag);
        }

        // 不変条件チェック（PrepBag割当済み等）は PrepBag.Create 内で実施
        var prepBag = PrepBag.Create(dto.SafeId, cashBags, DateTime.UtcNow);
        await prepBagRepository.AddAsync(prepBag);

        return ToDto(prepBag);
    }

    private static PrepBagDto ToDto(PrepBag bag) => new(
        bag.Id,
        bag.TotalAmount,
        bag.Status.ToString(),
        bag.CreatedAt,
        bag.HandedOverAt,
        bag.CashBags.Select(cb => cb.Id).ToList()
    );
}
