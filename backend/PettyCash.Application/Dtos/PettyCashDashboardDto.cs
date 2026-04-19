namespace PettyCash.Application.Dtos;

public record PettyCashDashboardDto(
    SafeDto Safe,
    IReadOnlyList<PettyCashTransactionDto> Transactions,
    IReadOnlyList<DenominationCheckDto> DenominationChecks
);
