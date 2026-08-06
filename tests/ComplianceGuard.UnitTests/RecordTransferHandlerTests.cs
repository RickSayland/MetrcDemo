using ComplianceGuard.Application.Transfers;
using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using Moq;

namespace ComplianceGuard.UnitTests;

public class RecordTransferHandlerTests
{
    private readonly Mock<ITransferRepository> _repoMock = new();
    private readonly Mock<ITenantContext> _tenantMock = new();
    private readonly Guid _tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private RecordTransferHandler CreateHandler()
    {
        _tenantMock.Setup(t => t.TenantId).Returns(_tenantId);
        return new RecordTransferHandler(_repoMock.Object, _tenantMock.Object);
    }

    [Fact]
    public async Task HandleAsync_SetsStatusToScheduled()
    {
        var handler = CreateHandler();
        var command = MakeCommand();

        var result = await handler.HandleAsync(command);

        Assert.Equal("Scheduled", result.Status);
    }

    [Fact]
    public async Task HandleAsync_AssignsFacilityIdFromTenantContext()
    {
        var handler = CreateHandler();
        var command = MakeCommand();

        var result = await handler.HandleAsync(command);

        Assert.Equal(_tenantId, result.FacilityId);
    }

    [Fact]
    public async Task HandleAsync_MapsAllCommandFieldsToTransfer()
    {
        var handler = CreateHandler();
        var command = MakeCommand();

        var result = await handler.HandleAsync(command);

        Assert.Equal(command.ManifestNumber, result.ManifestNumber);
        Assert.Equal(command.ShipperFacilityLicenseNumber, result.ShipperFacilityLicenseNumber);
        Assert.Equal(command.RecipientFacilityName, result.RecipientFacilityName);
        Assert.Equal(command.TransporterName, result.TransporterName);
        Assert.Equal(command.DriverName, result.DriverName);
        Assert.Equal(command.VehicleLicensePlate, result.VehicleLicensePlate);
        Assert.Equal(command.PackageCount, result.PackageCount);
    }

    [Fact]
    public async Task HandleAsync_CallsRepositoryAddAsync()
    {
        var handler = CreateHandler();
        var command = MakeCommand();

        await handler.HandleAsync(command);

        _repoMock.Verify(r => r.AddAsync(It.IsAny<Transfer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_GeneratesNewGuidForId()
    {
        var handler = CreateHandler();

        var result = await handler.HandleAsync(MakeCommand());

        Assert.NotEqual(Guid.Empty, result.Id);
    }

    private static RecordTransferCommand MakeCommand() => new(
        ManifestNumber: "OR-MAN-TEST-001",
        ShipperFacilityLicenseNumber: "OR-CUL-00142",
        ShipperFacilityName: "Test Shipper",
        RecipientFacilityLicenseNumber: "OR-RET-00287",
        RecipientFacilityName: "Test Recipient",
        TransporterName: "Test Transport",
        DriverName: "Test Driver",
        VehicleLicensePlate: "TEST-001",
        PackageCount: 5,
        EstimatedDepartureAt: new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc),
        EstimatedArrivalAt: new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc));
}
