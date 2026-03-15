namespace PettyCash.Application.Dtos;

public record CreateTransactionRequestDto(
    int SafeId,
    string Type,
    int Amount,
    string Description,
    DateTime Date,
    DenominationDto? Denomination = null
);
