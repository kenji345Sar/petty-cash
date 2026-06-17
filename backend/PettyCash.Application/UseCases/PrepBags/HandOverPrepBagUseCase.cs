using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared.Services;

namespace PettyCash.Application.UseCases.PrepBags;

public class HandOverPrepBagUseCase(IPrepBagRepository prepBagRepository, IVendorTransactionRepository vendorTransactionRepository, ISequenceNumberService sequenceNumberService, IBalanceService balanceService, IUnitOfWork unitOfWork)
{
    public async Task<PrepBagDto> ExecuteAsync(int id)
    {
        var bag = await prepBagRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"準備バッグ(ID={id})が見つかりません。");

        var transaction = bag.MarkHandedOver(DateTime.UtcNow);
        await sequenceNumberService.AssignAsync(transaction);
        await balanceService.AssignBalanceAsync(transaction);
        await vendorTransactionRepository.AddAsync(transaction);
        await unitOfWork.SaveChangesAsync();

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
