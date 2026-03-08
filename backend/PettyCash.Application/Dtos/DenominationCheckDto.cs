namespace PettyCash.Application.Dtos;

public record DenominationCheckDto(
    int Id,
    int? ChangeBagId,
    int? CashBagId,
    int? PrepBagId,
    int Count10000,
    int Count5000,
    int Count1000,
    int Count500,
    int Count100,
    int Count50,
    int Count10,
    int Count5,
    int Count1,
    int CheckedAmount,
    int ExpectedAmount,
    int Difference,
    DateTime CreatedAt
);
