using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.UseCases.DenominationChecks;

public class CheckCashBagUseCase(
    ICashBagRepository bagRepository,
    IVendorDenominationCheckRepository checkRepository,
    IVendorTransactionRepository transactionRepository,
    ISequenceNumberService sequenceNumberService,
    IBalanceService balanceService,
    IUnitOfWork unitOfWork)
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

        var check = VendorDenominationCheck.CreateForCashBag(bag, denomination, now);
        await sequenceNumberService.AssignAsync(check);
        await checkRepository.AddAsync(check);

        // 差額があればバッグ金額を実数に調整し、調整取引を記録
        if (check.Difference != 0)
        {
            var adjustment = VendorTransaction.CreateAdjustment(bag, check.Difference, now);
            await sequenceNumberService.AssignAsync(adjustment);
            await balanceService.AssignBalanceAsync(adjustment);
            await transactionRepository.AddAsync(adjustment);
            bag.UpdateAmount(denomination.TotalAmount);
        }

        await unitOfWork.SaveChangesAsync();

        return ToDto(check);
    }

    private static DenominationCheckDto ToDto(VendorDenominationCheck c) => new(
        c.Id, c.SequenceNumber, c.ChangeBagId, c.CashBagId, c.PrepBagId,
        c.Denomination.Count10000, c.Denomination.Count5000, c.Denomination.Count1000,
        c.Denomination.Count500, c.Denomination.Count100, c.Denomination.Count50,
        c.Denomination.Count10, c.Denomination.Count5, c.Denomination.Count1,
        c.CheckedAmount, c.ExpectedAmount, c.Difference, c.CreatedAt);
}
