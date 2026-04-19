using Microsoft.EntityFrameworkCore;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.Services;
using PettyCash.Infrastructure.Data;
using PettyCash.Infrastructure.ReadModels;

namespace PettyCash.Infrastructure.Services;

public class ProjectionService(PettyCashDbContext context) : IProjectionService
{
    public async Task ProjectAsync(VendorTransaction transaction)
    {
        // 1. 出納帳 Read Model にINSERT
        context.VendorLedgerEntries.Add(new VendorLedgerEntry
        {
            SequenceNumber = transaction.SequenceNumber,
            SafeId = transaction.SafeId,
            ChangeBagId = transaction.ChangeBagId,
            CashBagId = transaction.CashBagId,
            PrepBagId = transaction.PrepBagId,
            Type = (int)transaction.Type,
            Amount = transaction.Amount,
            Balance = transaction.Balance,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        });

        // 2. 残高 Read Model を更新
        await UpsertVendorBalance(transaction.SafeId, transaction.Balance);
    }

    public async Task ProjectAsync(PettyCashTransaction transaction)
    {
        // 1. 出納帳 Read Model にINSERT
        context.PettyCashLedgerEntries.Add(new PettyCashLedgerEntry
        {
            SequenceNumber = transaction.SequenceNumber,
            SafeId = transaction.SafeId,
            Type = (int)transaction.Type,
            Amount = transaction.Amount,
            Balance = transaction.Balance,
            Description = transaction.Description,
            CreatedAt = transaction.CreatedAt
        });

        // 2. 残高 Read Model を更新
        await UpsertPettyCashBalance(transaction.SafeId, transaction.Balance);
    }

    private async Task UpsertVendorBalance(int safeId, int newBalance)
    {
        var existing = await context.SafeBalances.FindAsync(safeId);
        if (existing != null)
        {
            existing.VendorBalance = newBalance;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            context.SafeBalances.Add(new SafeBalance
            {
                SafeId = safeId,
                VendorBalance = newBalance,
                PettyCashBalance = 0,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task UpsertPettyCashBalance(int safeId, int newBalance)
    {
        var existing = await context.SafeBalances.FindAsync(safeId);
        if (existing != null)
        {
            existing.PettyCashBalance = newBalance;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            context.SafeBalances.Add(new SafeBalance
            {
                SafeId = safeId,
                VendorBalance = 0,
                PettyCashBalance = newBalance,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
