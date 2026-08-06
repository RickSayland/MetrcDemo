using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Application.Transfers;

public class RecordTransferHandler
{
    private readonly ITransferRepository _repository;
    private readonly ITenantContext _tenantContext;

    public RecordTransferHandler(ITransferRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<Transfer> HandleAsync(RecordTransferCommand command, CancellationToken ct = default)
    {
        var transfer = new Transfer
        {
            Id = Guid.NewGuid(),
            FacilityId = _tenantContext.TenantId,
            ManifestNumber = command.ManifestNumber,
            ShipperFacilityLicenseNumber = command.ShipperFacilityLicenseNumber,
            ShipperFacilityName = command.ShipperFacilityName,
            RecipientFacilityLicenseNumber = command.RecipientFacilityLicenseNumber,
            RecipientFacilityName = command.RecipientFacilityName,
            TransporterName = command.TransporterName,
            DriverName = command.DriverName,
            VehicleLicensePlate = command.VehicleLicensePlate,
            PackageCount = command.PackageCount,
            EstimatedDepartureAt = command.EstimatedDepartureAt,
            EstimatedArrivalAt = command.EstimatedArrivalAt,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(transfer, ct);
        return transfer;
    }
}

public record RecordTransferCommand(
    string ManifestNumber,
    string ShipperFacilityLicenseNumber,
    string ShipperFacilityName,
    string RecipientFacilityLicenseNumber,
    string RecipientFacilityName,
    string TransporterName,
    string DriverName,
    string VehicleLicensePlate,
    int PackageCount,
    DateTime EstimatedDepartureAt,
    DateTime EstimatedArrivalAt);
