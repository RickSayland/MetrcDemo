namespace ComplianceGuard.Domain.Entities;

public class LabTest
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PackageId { get; set; }
    public string TestType { get; set; } = string.Empty;
    public bool OverallPassed { get; set; }
    public DateTime ResultDate { get; set; }
    public string LabFacilityName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Package Package { get; set; } = null!;
}
