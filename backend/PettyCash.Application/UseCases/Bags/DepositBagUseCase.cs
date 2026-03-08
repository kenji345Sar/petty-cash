using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Repositories;

namespace PettyCash.Application.UseCases.Bags;

public class DepositBagUseCase(IChangeBagRepository bagRepository)
{
    public async Task<ChangeBagDto> ExecuteAsync(DepositRequestDto dto)
    {
        var date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);
        var bag = ChangeBag.CreateDeposit(dto.Amount, dto.Description, date);
        await bagRepository.AddAsync(bag);

        return ToDto(bag);
    }

    private static ChangeBagDto ToDto(ChangeBag bag) => new(
        bag.Id,
        bag.TotalAmount,
        bag.Description,
        bag.Status.ToString(),
        bag.CreatedAt,
        bag.MovedAt
    );
}
