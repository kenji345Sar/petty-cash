using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Services;

public interface ISequenceNumberService
{
    Task AssignAsync(Transaction transaction);
    Task AssignAsync(DenominationCheck check);
}
