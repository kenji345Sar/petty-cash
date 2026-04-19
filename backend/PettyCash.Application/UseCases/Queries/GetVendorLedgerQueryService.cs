using PettyCash.Application.Dtos;

namespace PettyCash.Application.UseCases.Queries;

public interface IVendorLedgerQueryService
{
    Task<IReadOnlyList<VendorTransactionDto>> GetBySafeIdAsync(int safeId);
}
