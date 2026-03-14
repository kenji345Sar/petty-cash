namespace PettyCash.Application.Dtos;

public record SafeDto(
    int Id,
    string Name,
    string Description,
    int CurrentBalance,
    DateTime CreatedAt
);

public record CreateSafeRequestDto(
    string Name,
    string Description
);
