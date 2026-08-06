namespace ComplianceGuard.Domain.Entities;

public class Transfer
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public string ManifestNumber { get; set; } = string.Empty;
    public string ShipperFacilityLicenseNumber { get; set; } = string.Empty;
    public string ShipperFacilityName { get; set; } = string.Empty;
    public string RecipientFacilityLicenseNumber { get; set; } = string.Empty;
    public string RecipientFacilityName { get; set; } = string.Empty;
    public string TransporterName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string VehicleLicensePlate { get; set; } = string.Empty;
    public int PackageCount { get; set; }
    public DateTime EstimatedDepartureAt { get; set; }
    public DateTime EstimatedArrivalAt { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public DateTime? ActualArrivalAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
