using PettyCash.Application.Dtos;
using PettyCash.Domain.Entities;
using PettyCash.Domain.Enums;
using PettyCash.Domain.Repositories;
using PettyCash.Domain.Services;
using PettyCash.Domain.ValueObjects;

namespace PettyCash.Application.UseCases.Transactions;

public class CreateTransactionUseCase(ITransactionRepository transactionRepository, ISequenceNumberService sequenceNumberService)
{
    public async Task<TransactionDto> ExecuteAsync(CreateTransactionRequestDto dto)
    {
        var type = dto.Type == "Deposit" ? TransactionType.Deposit : TransactionType.Withdrawal;
        var date = DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc);

        Denomination? denomination = null;
        if (dto.Denomination is { } d)
        {
            denomination = new Denomination(
                d.Count10000, d.Count5000, d.Count1000,
                d.Count500, d.Count100, d.Count50,
                d.Count10, d.Count5, d.Count1
            );
        }

        if (denomination == null && dto.Amount <= 0)
            throw new ArgumentException("金額は1以上である必要があります。");

        var transaction = Transaction.CreateStandalone(dto.SafeId, type, dto.Amount, dto.Description, date, denomination);
        await sequenceNumberService.AssignAsync(transaction);
        await transactionRepository.AddAsync(transaction);

        return MapToDto(transaction);
    }

    private static TransactionDto MapToDto(Transaction t)
    {
        var denomDto = t.Denomination is { } dn
            ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
            : null;

        return new TransactionDto(
            t.Id, t.SequenceNumber, t.ChangeBagId, t.CashBagId, t.PrepBagId,
            t.Type.ToString(), t.Amount, t.Description, t.CreatedAt, denomDto
        );
    }
}
