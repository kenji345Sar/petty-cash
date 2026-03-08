namespace PettyCash.Application.Dtos;

public record PrepBagDto(
    int Id,
    int TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? HandedOverAt,
    List<int> CashBagIds
);
