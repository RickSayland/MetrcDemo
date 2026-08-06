namespace ComplianceGuard.Domain.Entities;

public class Package
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LabTestStatus { get; set; }
    public DateTime PackagedDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
