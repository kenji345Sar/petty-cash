namespace PettyCash.Infrastructure.ReadModels;

public class DomainEvent
{
    public long Id { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public int AggregateId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
