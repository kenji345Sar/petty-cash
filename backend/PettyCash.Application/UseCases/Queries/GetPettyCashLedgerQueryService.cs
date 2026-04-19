using PettyCash.Application.Dtos;

namespace PettyCash.Application.UseCases.Queries;

public interface IPettyCashLedgerQueryService
{
    Task<IReadOnlyList<PettyCashTransactionDto>> GetBySafeIdAsync(int safeId);
}
