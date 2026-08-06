namespace ComplianceGuard.Domain.Entities;

public class Facility
{
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<Package> Packages { get; set; } = [];
    public ICollection<Transfer> Transfers { get; set; } = [];
}
