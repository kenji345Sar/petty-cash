using PettyCash.Application.Dtos;
using PettyCash.Domain.Repositories;

namespace PettyCash.Application.UseCases.Bags;

public class MoveBagToRegisterUseCase(IChangeBagRepository bagRepository)
{
    public async Task<TransactionDto> ExecuteAsync(int bagId)
    {
        var bag = await bagRepository.GetByIdAsync(bagId)
            ?? throw new KeyNotFoundException($"バッグID {bagId} が見つかりません。");

        var transaction = bag.MoveToRegister();
        await bagRepository.UpdateAsync(bag);

        return new TransactionDto(
            transaction.Id,
            transaction.ChangeBagId,
            transaction.CashBagId,
            transaction.PrepBagId,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.Description,
            transaction.CreatedAt,
            null
        );
    }
}
