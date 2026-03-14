using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Enums;
using PettyCash.Domain.Repositories;

namespace PettyCash.Application.UseCases.Transactions;

public class CreateTransactionUseCase(ITransactionRepository transactionRepository)
{
    public async Task<TransactionDto> ExecuteAsync(CreateTransactionRequestDto dto)
    {
        if (dto.Amount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        var type = dto.Type == "Deposit" ? TransactionType.Deposit : TransactionType.Withdrawal;
        var date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        var transaction = Transaction.CreateStandalone(dto.SafeId, type, dto.Amount, dto.Description, date);
        await transactionRepository.AddAsync(transaction);

        return new TransactionDto(
            transaction.Id,
            transaction.ChangeBagId,
            transaction.CashBagId,
            transaction.PrepBagId,
            transaction.Type.ToString(),
            transaction.Amount,
            transaction.Description,
            transaction.CreatedAt
        );
    }
}
