using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;

namespace PettyCash.Application.UseCases.Bags;

public class GetBagsUseCase(IChangeBagRepository bagRepository)
{
    public async Task<IReadOnlyList<ChangeBagDto>> ExecuteAsync(int safeId)
    {
        var bags = await bagRepository.GetBySafeIdAsync(safeId);

        return bags.Select(bag =>
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
        }).ToList();
    }
}
