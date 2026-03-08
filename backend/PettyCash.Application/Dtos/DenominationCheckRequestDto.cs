namespace PettyCash.Application.Dtos;

public record DenominationCheckRequestDto(
    int Count10000,
    int Count5000,
    int Count1000,
    int Count500,
    int Count100,
    int Count50,
    int Count10,
    int Count5,
    int Count1
);
