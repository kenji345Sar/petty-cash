namespace PettyCash.Application.Dtos;

public record ChangeBagDto(
    int Id,
    int TotalAmount,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? MovedAt,
    int? DepositSequenceNumber,
    int? WithdrawalSequenceNumber,
    DenominationDto? Denomination
);
