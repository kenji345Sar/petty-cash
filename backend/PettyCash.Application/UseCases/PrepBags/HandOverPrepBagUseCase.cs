using PettyCash.Application.Dtos;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;

namespace PettyCash.Application.UseCases.PrepBags;

public class HandOverPrepBagUseCase(IPrepBagRepository prepBagRepository, ITransactionRepository transactionRepository, ISequenceNumberService sequenceNumberService)
{
    public async Task<PrepBagDto> ExecuteAsync(int id)
    {
        var bag = await prepBagRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"準備バッグ(ID={id})が見つかりません。");

        var transaction = bag.MarkHandedOver();
        await sequenceNumberService.AssignAsync(transaction);
        await transactionRepository.AddAsync(transaction);

        return new PrepBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.HandedOverAt,
            bag.CashBags.Select(cb => cb.Id).ToList()
        );
    }
}
