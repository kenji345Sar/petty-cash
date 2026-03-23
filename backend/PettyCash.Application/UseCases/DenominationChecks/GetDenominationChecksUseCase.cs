using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.DenomCheck;
using PettyCash.Domain.PettyCash.DenomCheck;

namespace PettyCash.Application.UseCases.DenominationChecks;

public class GetVendorDenominationChecksUseCase(IVendorDenominationCheckRepository checkRepository)
{
    public async Task<IReadOnlyList<DenominationCheckDto>> ExecuteAsync(int safeId)
    {
        var checks = await checkRepository.GetBySafeIdAsync(safeId);

        return checks.Select(c => new DenominationCheckDto(
            c.Id, c.SequenceNumber, c.ChangeBagId, c.CashBagId, c.PrepBagId,
            c.Denomination.Count10000, c.Denomination.Count5000, c.Denomination.Count1000,
            c.Denomination.Count500, c.Denomination.Count100, c.Denomination.Count50,
            c.Denomination.Count10, c.Denomination.Count5, c.Denomination.Count1,
            c.CheckedAmount, c.ExpectedAmount, c.Difference, c.CreatedAt
        )).ToList();
    }
}

public class GetPettyCashDenominationChecksUseCase(IPettyCashDenominationCheckRepository checkRepository)
{
    public async Task<IReadOnlyList<DenominationCheckDto>> ExecuteAsync(int safeId)
    {
        var checks = await checkRepository.GetBySafeIdAsync(safeId);

        return checks.Select(c => new DenominationCheckDto(
            c.Id, c.SequenceNumber, null, null, null,
            c.Denomination.Count10000, c.Denomination.Count5000, c.Denomination.Count1000,
            c.Denomination.Count500, c.Denomination.Count100, c.Denomination.Count50,
            c.Denomination.Count10, c.Denomination.Count5, c.Denomination.Count1,
            c.CheckedAmount, c.ExpectedAmount, c.Difference, c.CreatedAt
        )).ToList();
    }
}
