namespace PettyCash.Application.Dtos;

public record SafeDto(
    int Id,
    string Name,
    string Description,
    int CurrentBalance,
    int VendorBalance,
    int PettyCashBalance,
    DateTime CreatedAt
);

public record CreateSafeRequestDto(
    string Name,
    string Description
);
