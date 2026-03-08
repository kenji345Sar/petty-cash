namespace PettyCash.Application.Dtos;

public record CashBagDto(
    int Id,
    int TotalAmount,
    string Description,
    string Status,
    DateTime CreatedAt,
    DateTime? MovedAt
);
