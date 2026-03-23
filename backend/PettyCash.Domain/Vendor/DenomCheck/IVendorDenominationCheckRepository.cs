namespace PettyCash.Domain.Vendor.DenomCheck;

public interface IVendorDenominationCheckRepository
{
    Task<VendorDenominationCheck?> GetByIdAsync(int id);
    Task<IReadOnlyList<VendorDenominationCheck>> GetBySafeIdAsync(int safeId);
    Task AddAsync(VendorDenominationCheck check);
    Task UpdateAsync(VendorDenominationCheck check);
}
