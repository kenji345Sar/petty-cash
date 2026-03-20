namespace PettyCash.Application.Dtos;

public record CreatePettyCashTransactionRequestDto(
    int SafeId,
    string Type,
    int Amount,
    string Description,
    DateTime Date,
    DenominationDto? Denomination = null
);
