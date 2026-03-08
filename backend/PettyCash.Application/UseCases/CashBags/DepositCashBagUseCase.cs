using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;

namespace PettyCash.Application.UseCases.CashBags;

public class DepositCashBagUseCase(ICashBagRepository cashBagRepository)
{
    public async Task<CashBagDto> ExecuteAsync(DepositRequestDto dto)
    {
        var date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        var bag = CashBag.CreateDeposit(dto.Amount, dto.Description, date);
        await cashBagRepository.AddAsync(bag);

        return ToDto(bag);
    }

    private static CashBagDto ToDto(CashBag bag) => new(
        bag.Id,
        bag.TotalAmount,
        bag.Description,
        bag.Status.ToString(),
        bag.CreatedAt,
        bag.MovedAt
    );
}
