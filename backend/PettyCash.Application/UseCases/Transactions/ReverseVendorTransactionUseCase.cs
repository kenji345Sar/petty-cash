using PettyCash.Application.Dtos;
using PettyCash.Domain.SafeAggregate;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.Services;
using PettyCash.Domain.Vendor.Ledger;

namespace PettyCash.Application.UseCases.Transactions;

public class ReverseVendorTransactionUseCase(
    IVendorTransactionRepository transactionRepository,
    ISafeRepository safeRepository,
    ISequenceNumberService sequenceNumberService,
    IBalanceService balanceService,
    IUnitOfWork unitOfWork)
{
    public async Task<VendorTransactionDto> ExecuteAsync(int originalId, ReverseTransactionRequestDto dto)
    {
        var original = await transactionRepository.GetByIdAsync(originalId)
            ?? throw new KeyNotFoundException($"取引(ID={originalId})が見つかりません。");

        var desc = string.IsNullOrWhiteSpace(dto.Description)
            ? $"#{original.SequenceNumber}の修正（赤伝）"
            : dto.Description;

        // Domain: 赤伝取引をどう作るか（逆転ロジックは Domain が知っている）
        var reversal = VendorTransaction.CreateReversal(original, desc, DateTime.UtcNow);

        // Domain: 出金可能かどうか
        if (reversal.Type == TransactionType.Withdrawal)
        {
            var safe = await safeRepository.GetByIdAsync(original.SafeId)
                ?? throw new KeyNotFoundException($"金庫(ID={original.SafeId})が見つかりません。");
            safe.EnsureCanWithdraw(reversal.Amount);
        }

        await sequenceNumberService.AssignAsync(reversal);
        await balanceService.AssignBalanceAsync(reversal);
        await transactionRepository.AddAsync(reversal);
        await unitOfWork.SaveChangesAsync();

        return new VendorTransactionDto(
            reversal.Id, reversal.SequenceNumber,
            null, null, null,
            reversal.Type.ToString(), reversal.Amount, reversal.Balance, reversal.Description, reversal.CreatedAt,
            null
        );
    }
}
