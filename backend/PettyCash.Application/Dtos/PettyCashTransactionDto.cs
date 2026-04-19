namespace PettyCash.Application.Dtos;

public record PettyCashTransactionDto(
    int Id,
    int SequenceNumber,
    string Type,
    int Amount,
    int Balance,
    string Description,
    DateTime CreatedAt,
    DenominationDto? Denomination
);
