using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;
using PettyCash.Domain.ValueObjects;

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

    private static CashBagDto ToDto(CashBag bag) => new(
        bag.Id,
        bag.TotalAmount,
        bag.Description,
        bag.Status.ToString(),
        bag.CreatedAt,
        bag.MovedAt,
        bag.Transaction?.SequenceNumber
    );
}
