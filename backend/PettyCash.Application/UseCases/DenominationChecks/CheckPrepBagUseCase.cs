using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.UseCases.DenominationChecks;

public class CheckPrepBagUseCase(
    IPrepBagRepository prepBagRepository,
    IVendorDenominationCheckRepository checkRepository,
    ISequenceNumberService sequenceNumberService,
    IUnitOfWork unitOfWork)
{
    public async Task<DenominationCheckDto> ExecuteAsync(int bagId, DenominationCheckRequestDto dto)
    {
        var bag = await prepBagRepository.GetByIdAsync(bagId)
            ?? throw new KeyNotFoundException($"準備バッグ(ID={bagId})が見つかりません。");

        var denomination = new Denomination(
            dto.Count10000, dto.Count5000, dto.Count1000,
            dto.Count500, dto.Count100, dto.Count50,
            dto.Count10, dto.Count5, dto.Count1);

        var check = VendorDenominationCheck.CreateForPrepBag(bag, denomination, DateTime.UtcNow);
        await sequenceNumberService.AssignAsync(check);
        await checkRepository.AddAsync(check);
        await unitOfWork.SaveChangesAsync();

        return new DenominationCheckDto(
            check.Id, check.SequenceNumber, check.ChangeBagId, check.CashBagId, check.PrepBagId,
            check.Denomination.Count10000, check.Denomination.Count5000, check.Denomination.Count1000,
            check.Denomination.Count500, check.Denomination.Count100, check.Denomination.Count50,
            check.Denomination.Count10, check.Denomination.Count5, check.Denomination.Count1,
            check.CheckedAmount, check.ExpectedAmount, check.Difference, check.CreatedAt);
    }
}
