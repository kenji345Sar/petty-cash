namespace PettyCash.Infrastructure.ReadModels;

public class SafeBalance
{
    public int SafeId { get; set; }
    public int VendorBalance { get; set; }
    public int PettyCashBalance { get; set; }
    public DateTime UpdatedAt { get; set; }
}
