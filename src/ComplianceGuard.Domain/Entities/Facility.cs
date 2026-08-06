namespace ComplianceGuard.Domain.Entities;

public class Facility
{
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
