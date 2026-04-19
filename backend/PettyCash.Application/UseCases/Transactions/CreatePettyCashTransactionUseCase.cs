using PettyCash.Application.Dtos;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.Vendor.BagManagement;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Shared.ValueObjects;

namespace PettyCash.Application.UseCases.Transactions;

public class CreatePettyCashTransactionUseCase(
    IPettyCashTransactionRepository transactionRepository,
    ISafeRepository safeRepository,
    ISequenceNumberService sequenceNumberService,
    IBalanceService balanceService,
    IProjectionService projectionService,
    IEventStore eventStore,
    IUnitOfWork unitOfWork)
{
    public async Task<PettyCashTransactionDto> ExecuteAsync(CreatePettyCashTransactionRequestDto dto)
    {
        var type = dto.Type == "Deposit" ? TransactionType.Deposit : TransactionType.Withdrawal;
        var date = DateTime.UtcNow;

        Denomination? denomination = null;
        if (dto.Denomination is { } d)
        {
            denomination = new Denomination(
                d.Count10000, d.Count5000, d.Count1000,
                d.Count500, d.Count100, d.Count50,
                d.Count10, d.Count5, d.Count1
            );
        }

        var transaction = PettyCashTransaction.Create(dto.SafeId, type, dto.Amount, dto.Description, date, denomination);

        if (type == TransactionType.Withdrawal)
        {
            var safe = await safeRepository.GetByIdAsync(dto.SafeId)
                ?? throw new KeyNotFoundException($"金庫(ID={dto.SafeId})が見つかりません。");
            safe.EnsureCanWithdraw(transaction.Amount);
        }

        await sequenceNumberService.AssignAsync(transaction);
        await balanceService.AssignBalanceAsync(transaction);
        await transactionRepository.AddAsync(transaction);
        await eventStore.AppendAsync(transaction);
        await projectionService.ProjectAsync(transaction);
        await unitOfWork.SaveChangesAsync();

        var denomDto = transaction.Denomination is { } dn
            ? new DenominationDto(dn.Count10000, dn.Count5000, dn.Count1000, dn.Count500, dn.Count100, dn.Count50, dn.Count10, dn.Count5, dn.Count1)
            : null;

        return new PettyCashTransactionDto(
            transaction.Id, transaction.SequenceNumber,
            transaction.Type.ToString(), transaction.Amount, transaction.Balance, transaction.Description, transaction.CreatedAt, denomDto
        );
    }
}
