namespace PettyCash.Application.Dtos;

public record VendorDashboardDto(
    SafeDto Safe,
    IReadOnlyList<ChangeBagDto> Bags,
    IReadOnlyList<CashBagDto> CashBags,
    IReadOnlyList<VendorTransactionDto> Transactions,
    IReadOnlyList<DenominationCheckDto> DenominationChecks,
    IReadOnlyList<PrepBagDto> PrepBags
);
