namespace PettyCash.Application.Dtos;

public record DepositRequestDto(
    int SafeId,
    int Amount,
    string Description,
    DateTime Date
);
