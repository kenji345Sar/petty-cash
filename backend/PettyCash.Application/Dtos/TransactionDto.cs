namespace PettyCash.Application.Dtos;

public record TransactionDto(
    int Id,
    int? ChangeBagId,
    int? CashBagId,
    int? PrepBagId,
    string Type,
    int Amount,
    string Description,
    DateTime CreatedAt,
    DenominationDto? Denomination
);
