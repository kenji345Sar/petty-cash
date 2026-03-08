namespace PettyCash.Application.Dtos;

public record DepositRequestDto(
    int Amount,
    string Description,
    DateTime Date
);
