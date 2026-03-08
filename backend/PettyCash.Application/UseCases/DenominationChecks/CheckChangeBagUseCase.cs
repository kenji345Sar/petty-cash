using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.ValueObjects;

namespace PettyCash.Application.UseCases.DenominationChecks;

public class CheckChangeBagUseCase(
    IChangeBagRepository bagRepository,
    IDenominationCheckRepository checkRepository)
{
    public async Task<DenominationCheckDto> ExecuteAsync(int bagId, DenominationCheckRequestDto dto)
    {
        var bag = await bagRepository.GetByIdAsync(bagId)
            ?? throw new KeyNotFoundException($"釣り銭バッグ(ID={bagId})が見つかりません。");

        var denomination = new Denomination(
            dto.Count10000, dto.Count5000, dto.Count1000,
            dto.Count500, dto.Count100, dto.Count50,
            dto.Count10, dto.Count5, dto.Count1);

        var check = DenominationCheck.CreateForChangeBag(bag, denomination);
        await checkRepository.AddAsync(check);

        return ToDto(check);
    }

    private static DenominationCheckDto ToDto(DenominationCheck c) => new(
        c.Id, c.ChangeBagId, c.CashBagId, c.PrepBagId,
        c.Denomination.Count10000, c.Denomination.Count5000, c.Denomination.Count1000,
        c.Denomination.Count500, c.Denomination.Count100, c.Denomination.Count50,
        c.Denomination.Count10, c.Denomination.Count5, c.Denomination.Count1,
        c.CheckedAmount, c.ExpectedAmount, c.Difference, c.CreatedAt);
}
