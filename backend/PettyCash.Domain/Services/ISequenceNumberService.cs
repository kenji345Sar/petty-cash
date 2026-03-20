using PettyCash.Domain.Entities;

namespace PettyCash.Domain.Services;

public interface ISequenceNumberService
{
    Task AssignAsync(VendorTransaction transaction);
    Task AssignAsync(PettyCashTransaction transaction);
    Task AssignAsync(DenominationCheck check);
}
