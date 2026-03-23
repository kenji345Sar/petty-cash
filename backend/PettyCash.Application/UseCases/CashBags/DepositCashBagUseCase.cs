using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.UseCases.CashBags;

public class DepositCashBagUseCase(ICashBagRepository cashBagRepository, ISequenceNumberService sequenceNumberService)
{
    public async Task<CashBagDto> ExecuteAsync(DepositRequestDto dto)
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
        var bag = CashBag.CreateDeposit(dto.SafeId, dto.Amount, dto.Description, date, denomination);
        await sequenceNumberService.AssignAsync(bag.Transaction!);
        await cashBagRepository.AddAsync(bag);

        return ToDto(bag);
    }

    private static CashBagDto ToDto(CashBag bag)
    {
        var dn = bag.Transaction?.Denomination;
        var denomDto = dn != null
            ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
            : null;
        return new CashBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Description,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.MovedAt,
            bag.Transaction?.SequenceNumber,
            denomDto
        );
    }
}
