namespace PettyCash.Application.Dtos;

public record VendorTransactionDto(
    int Id,
    int SequenceNumber,
    int? ChangeBagId,
    int? CashBagId,
    int? PrepBagId,
    string Type,
    int Amount,
    string Description,
    DateTime CreatedAt,
    DenominationDto? Denomination
);
