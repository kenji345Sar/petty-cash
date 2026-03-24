using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.UseCases.Bags;

public class DepositBagUseCase(IChangeBagRepository bagRepository, ISequenceNumberService sequenceNumberService, IUnitOfWork unitOfWork)
{
    public async Task<ChangeBagDto> ExecuteAsync(DepositRequestDto dto)
    {
        var date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        Denomination? denomination = null;
        if (dto.Denomination is { } d)
        {
            denomination = new Denomination(
                d.Count10000, d.Count5000, d.Count1000,
                d.Count500, d.Count100, d.Count50,
                d.Count10, d.Count5, d.Count1
            );
        }
        var bag = ChangeBag.CreateDeposit(dto.SafeId, dto.Amount, dto.Description, date, denomination);
        await sequenceNumberService.AssignAsync(bag.DepositTransaction!);
        await bagRepository.AddAsync(bag);
        await unitOfWork.SaveChangesAsync();

        return ToDto(bag);
    }

    private static ChangeBagDto ToDto(ChangeBag bag)
    {
        var dn = bag.DepositTransaction?.Denomination;
        var denomDto = dn != null
            ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
            : null;
        return new ChangeBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Description,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.MovedAt,
            bag.DepositTransaction?.SequenceNumber,
            bag.WithdrawalTransaction?.SequenceNumber,
            denomDto
        );
    }
}
