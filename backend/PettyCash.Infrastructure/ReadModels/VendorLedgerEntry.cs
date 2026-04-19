namespace PettyCash.Infrastructure.ReadModels;

public class VendorLedgerEntry
{
    public int Id { get; set; }
    public int SequenceNumber { get; set; }
    public int SafeId { get; set; }
    public int? ChangeBagId { get; set; }
    public int? CashBagId { get; set; }
    public int? PrepBagId { get; set; }
    public int Type { get; set; }
    public int Amount { get; set; }
    public int Balance { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
