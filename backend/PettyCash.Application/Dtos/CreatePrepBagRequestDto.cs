namespace PettyCash.Application.Dtos;

public record CreatePrepBagRequestDto(int SafeId, List<int> CashBagIds);
