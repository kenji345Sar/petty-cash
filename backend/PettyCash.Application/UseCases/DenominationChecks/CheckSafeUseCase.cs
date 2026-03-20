using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;
using PettyCash.Domain.ValueObjects;

namespace PettyCash.Application.UseCases.DenominationChecks;

public class CheckSafeUseCase(
    ISafeRepository safeRepository,
    IDenominationCheckRepository checkRepository,
    IVendorTransactionRepository transactionRepository,
    ISequenceNumberService sequenceNumberService)
{
    public async Task<DenominationCheckDto> ExecuteAsync(int safeId, DenominationCheckRequestDto dto)
    {
        var safe = await safeRepository.GetByIdAsync(safeId)
            ?? throw new KeyNotFoundException($"金庫(ID={safeId})が見つかりません。");

        var now = DateTime.UtcNow;
        var denomination = new Denomination(
            dto.Count10000, dto.Count5000, dto.Count1000,
            dto.Count500, dto.Count100, dto.Count50,
            dto.Count10, dto.Count5, dto.Count1);

        var check = DenominationCheck.CreateForSafe(safeId, denomination, safe.CurrentBalance, now);
        await sequenceNumberService.AssignAsync(check);
        await checkRepository.AddAsync(check);

        // 差額があれば調整取引を記録
        if (check.Difference != 0)
        {
            var adjustment = VendorTransaction.CreateSafeAdjustment(safeId, check.Difference, now);
            await sequenceNumberService.AssignAsync(adjustment);
            await transactionRepository.AddAsync(adjustment);
        }

        return new DenominationCheckDto(
            check.Id, check.SequenceNumber, check.ChangeBagId, check.CashBagId, check.PrepBagId,
            check.Denomination.Count10000, check.Denomination.Count5000, check.Denomination.Count1000,
            check.Denomination.Count500, check.Denomination.Count100, check.Denomination.Count50,
            check.Denomination.Count10, check.Denomination.Count5, check.Denomination.Count1,
            check.CheckedAmount, check.ExpectedAmount, check.Difference, check.CreatedAt);
    }
}
