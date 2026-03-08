using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;

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

            if (cashBag.PrepBagId != null)
                throw new InvalidOperationException($"キャッシュバッグ(ID={id})は既に準備バッグに含まれています。");

            cashBags.Add(cashBag);
        }

        var prepBag = PrepBag.Create(cashBags, DateTime.UtcNow);
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
