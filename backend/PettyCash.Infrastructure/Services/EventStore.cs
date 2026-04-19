using System.Text.Json;
using PettyCash.Domain.Vendor.Ledger;
using PettyCash.Domain.PettyCash.Ledger;
using PettyCash.Domain.Shared;
using PettyCash.Domain.Shared.Services;
using PettyCash.Infrastructure.Data;
using PettyCash.Infrastructure.ReadModels;

namespace PettyCash.Infrastructure.Services;

public class EventStore(PettyCashDbContext context) : IEventStore
{
    public Task AppendAsync(VendorTransaction transaction)
    {
        var eventType = transaction.Type switch
        {
            TransactionType.Deposit => "VendorMoneyDeposited",
            TransactionType.Withdrawal => "VendorMoneyWithdrawn",
            TransactionType.Adjustment => "VendorBalanceAdjusted",
            _ => throw new ArgumentException($"Unknown transaction type: {transaction.Type}")
        };

        var payload = JsonSerializer.Serialize(new
        {
            safeId = transaction.SafeId,
            amount = transaction.Amount,
            balance = transaction.Balance,
            description = transaction.Description,
            sequenceNumber = transaction.SequenceNumber,
            changeBagId = transaction.ChangeBagId,
            cashBagId = transaction.CashBagId,
            prepBagId = transaction.PrepBagId
        });

        context.DomainEvents.Add(new DomainEvent
        {
            AggregateType = "Safe",
            AggregateId = transaction.SafeId,
            EventType = eventType,
            Payload = payload,
            CreatedAt = transaction.CreatedAt
        });

        return Task.CompletedTask;
    }

    public Task AppendAsync(PettyCashTransaction transaction)
    {
        var eventType = transaction.Type switch
        {
            TransactionType.Deposit => "PettyCashDeposited",
            TransactionType.Withdrawal => "PettyCashWithdrawn",
            TransactionType.Adjustment => "PettyCashBalanceAdjusted",
            _ => throw new ArgumentException($"Unknown transaction type: {transaction.Type}")
        };

        var payload = JsonSerializer.Serialize(new
        {
            safeId = transaction.SafeId,
            amount = transaction.Amount,
            balance = transaction.Balance,
            description = transaction.Description,
            sequenceNumber = transaction.SequenceNumber
        });

        context.DomainEvents.Add(new DomainEvent
        {
            AggregateType = "Safe",
            AggregateId = transaction.SafeId,
            EventType = eventType,
            Payload = payload,
            CreatedAt = transaction.CreatedAt
        });

        return Task.CompletedTask;
    }
}
