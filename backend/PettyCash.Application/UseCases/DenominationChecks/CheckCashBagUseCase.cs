using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;
using PettyCash.Domain.ValueObjects;

namespace PettyCash.Application.UseCases.DenominationChecks;

public class CheckCashBagUseCase(
    ICashBagRepository bagRepository,
    IDenominationCheckRepository checkRepository,
    IVendorTransactionRepository transactionRepository,
    ISequenceNumberService sequenceNumberService)
{
    public async Task<DenominationCheckDto> ExecuteAsync(int bagId, DenominationCheckRequestDto dto)
    {
        var bag = await bagRepository.GetByIdAsync(bagId)
            ?? throw new KeyNotFoundException($"キャッシュバッグ(ID={bagId})が見つかりません。");

        var now = DateTime.UtcNow;
        var denomination = new Denomination(
            dto.Count10000, dto.Count5000, dto.Count1000,
            dto.Count500, dto.Count100, dto.Count50,
            dto.Count10, dto.Count5, dto.Count1);

        var check = DenominationCheck.CreateForCashBag(bag, denomination, now);
        await sequenceNumberService.AssignAsync(check);
        await checkRepository.AddAsync(check);

        // 差額があればバッグ金額を実数に調整し、調整取引を記録
        var adjustment = bag.AdjustByCheck(denomination.TotalAmount, now);
        if (adjustment != null)
        {
            await sequenceNumberService.AssignAsync(adjustment);
            await transactionRepository.AddAsync(adjustment);
        }

        return ToDto(check);
    }

    private static DenominationCheckDto ToDto(DenominationCheck c) => new(
        c.Id, c.SequenceNumber, c.ChangeBagId, c.CashBagId, c.PrepBagId,
        c.Denomination.Count10000, c.Denomination.Count5000, c.Denomination.Count1000,
        c.Denomination.Count500, c.Denomination.Count100, c.Denomination.Count50,
        c.Denomination.Count10, c.Denomination.Count5, c.Denomination.Count1,
        c.CheckedAmount, c.ExpectedAmount, c.Difference, c.CreatedAt);
}
