using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.UseCases.DenominationChecks;

public class UpdateDenominationCheckUseCase(
    IDenominationCheckRepository checkRepository,
    IChangeBagRepository changeBagRepository,
    ICashBagRepository cashBagRepository,
    IPrepBagRepository prepBagRepository,
    ISafeRepository safeRepository)
{
    public async Task<DenominationCheckDto> ExecuteAsync(int id, DenominationCheckRequestDto dto)
    {
        var check = await checkRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"有高(ID={id})が見つかりません。");

        var expectedAmount = 0;
        if (check.ChangeBagId.HasValue)
        {
            var bag = await changeBagRepository.GetByIdAsync(check.ChangeBagId.Value)
                ?? throw new KeyNotFoundException($"釣り銭バッグ(ID={check.ChangeBagId})が見つかりません。");
            expectedAmount = bag.TotalAmount;
        }
        else if (check.CashBagId.HasValue)
        {
            var bag = await cashBagRepository.GetByIdAsync(check.CashBagId.Value)
                ?? throw new KeyNotFoundException($"キャッシュバッグ(ID={check.CashBagId})が見つかりません。");
            expectedAmount = bag.TotalAmount;
        }
        else if (check.PrepBagId.HasValue)
        {
            var bag = await prepBagRepository.GetByIdAsync(check.PrepBagId.Value)
                ?? throw new KeyNotFoundException($"準備バッグ(ID={check.PrepBagId})が見つかりません。");
            expectedAmount = bag.TotalAmount;
        }
        else
        {
            var safe = await safeRepository.GetByIdAsync(check.SafeId)
                ?? throw new KeyNotFoundException($"金庫(ID={check.SafeId})が見つかりません。");
            expectedAmount = safe.CurrentBalance;
        }

        var denomination = new Denomination(
            dto.Count10000, dto.Count5000, dto.Count1000,
            dto.Count500, dto.Count100, dto.Count50,
            dto.Count10, dto.Count5, dto.Count1);

        check.Update(denomination, expectedAmount);
        await checkRepository.UpdateAsync(check);

        return new DenominationCheckDto(
            check.Id, check.SequenceNumber, check.ChangeBagId, check.CashBagId, check.PrepBagId,
            check.Denomination.Count10000, check.Denomination.Count5000, check.Denomination.Count1000,
            check.Denomination.Count500, check.Denomination.Count100, check.Denomination.Count50,
            check.Denomination.Count10, check.Denomination.Count5, check.Denomination.Count1,
            check.CheckedAmount, check.ExpectedAmount, check.Difference, check.CreatedAt);
    }
}
