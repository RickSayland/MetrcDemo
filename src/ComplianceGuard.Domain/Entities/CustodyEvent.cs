namespace ComplianceGuard.Domain.Entities;

public class CustodyEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}
