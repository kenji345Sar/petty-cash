namespace PettyCash.Application.Dtos;

public record CreateTransactionRequestDto(
    string Type,
    int Amount,
    string Description,
    DateTime Date
);
