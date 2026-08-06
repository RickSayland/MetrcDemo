namespace ComplianceGuard.Domain.Entities;

public class AnomalyFlag
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CustodyEventId { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public string? Resolution { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
