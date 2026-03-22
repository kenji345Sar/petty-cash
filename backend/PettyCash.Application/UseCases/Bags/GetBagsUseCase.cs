using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.Shared.DenomCheck;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.Bags;

public class GetBagsUseCase(IChangeBagRepository bagRepository)
{
    public async Task<IReadOnlyList<ChangeBagDto>> ExecuteAsync(int safeId)
    {
        var bags = await bagRepository.GetBySafeIdAsync(safeId);

        return bags.Select(bag => new ChangeBagDto(
            bag.Id,
            bag.TotalAmount,
            bag.Description,
            bag.Status.ToString(),
            bag.CreatedAt,
            bag.MovedAt,
            bag.DepositTransaction?.SequenceNumber,
            bag.WithdrawalTransaction?.SequenceNumber
        )).ToList();
    }
}
