using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.UseCases.Bags;

public class MoveBagToRegisterUseCase(IChangeBagRepository bagRepository, ISequenceNumberService sequenceNumberService)
{
    public async Task<VendorTransactionDto> ExecuteAsync(int bagId)
    {
        var bag = await bagRepository.GetByIdAsync(bagId)
            ?? throw new KeyNotFoundException($"バッグID {bagId} が見つかりません。");

        var transaction = bag.MoveToRegister(DateTime.UtcNow);
        await sequenceNumberService.AssignAsync(transaction);
        await bagRepository.UpdateAsync(bag);

        return new VendorTransactionDto(
            transaction.Id,
            transaction.SequenceNumber,
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
