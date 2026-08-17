using System.Text.Json;
using ComplianceGuard.Infrastructure.Ai.Plugins;

namespace ComplianceGuard.UnitTests;

public class CustodyAnomalyPluginTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // --- Transfer Timing Gap ---

    [Fact]
    public async Task DetectTimingGap_72HourDelayOn2HourRoute_ReturnsHighAnomaly()
    {
        var plugin = MakeTransferPlugin(MakeTransfer(estimatedHours: 2, actualHours: 72));

        var results = await DeserializeResults(plugin.DetectTransferTimingGapAsync());

        var anomaly = Assert.Single(results);
        Assert.Equal("TransferTimingGap", anomaly.AnomalyType);
        Assert.Equal("High", anomaly.Severity);
    }

    [Fact]
    public async Task DetectTimingGap_ModerateDelay_ReturnsMediumAnomaly()
    {
        var plugin = MakeTransferPlugin(MakeTransfer(estimatedHours: 2, actualHours: 8));

        var results = await DeserializeResults(plugin.DetectTransferTimingGapAsync());

        var anomaly = Assert.Single(results);
        Assert.Equal("Medium", anomaly.Severity);
    }

    [Fact]
    public async Task DetectTimingGap_NormalTransit_ReturnsEmpty()
    {
        var plugin = MakeTransferPlugin(MakeTransfer(estimatedHours: 2, actualHours: 2.5));

        var results = await DeserializeResults(plugin.DetectTransferTimingGapAsync());

        Assert.Empty(results);
    }

    [Fact]
    public async Task DetectTimingGap_NoActualArrival_SkipsTransfer()
    {
        var transfer = MakeTransfer(estimatedHours: 2, actualHours: 0) with { ActualArrivalAt = null };
        var plugin = MakeTransferPlugin(transfer);

        var results = await DeserializeResults(plugin.DetectTransferTimingGapAsync());

        Assert.Empty(results);
    }

    // --- Facility Distance Violation ---

    [Fact]
    public async Task DetectDistanceViolation_PortlandToSF_In30Min_ReturnsCritical()
    {
        var transfer = new TransferDto
        {
            TransferId = Guid.NewGuid(),
            ManifestNumber = "TEST-001",
            ShipperLatitude = 45.5152, ShipperLongitude = -122.6784,
            RecipientLatitude = 37.7749, RecipientLongitude = -122.4194,
            ActualDepartureAt = DateTime.UtcNow,
            ActualArrivalAt = DateTime.UtcNow.AddMinutes(30),
            EstimatedDepartureAt = DateTime.UtcNow,
            EstimatedArrivalAt = DateTime.UtcNow.AddHours(10),
            PackageCount = 1
        };
        var plugin = MakeTransferPlugin(transfer);

        var results = await DeserializeResults(plugin.DetectFacilityDistanceViolationAsync());

        var anomaly = Assert.Single(results);
        Assert.Equal("FacilityDistanceViolation", anomaly.AnomalyType);
        Assert.Equal("Critical", anomaly.Severity);
    }

    [Fact]
    public async Task DetectDistanceViolation_ReasonableSpeed_ReturnsEmpty()
    {
        var transfer = new TransferDto
        {
            TransferId = Guid.NewGuid(),
            ManifestNumber = "TEST-002",
            ShipperLatitude = 45.5152, ShipperLongitude = -122.6784,
            RecipientLatitude = 44.0521, RecipientLongitude = -123.0868,
            ActualDepartureAt = DateTime.UtcNow,
            ActualArrivalAt = DateTime.UtcNow.AddHours(2),
            EstimatedDepartureAt = DateTime.UtcNow,
            EstimatedArrivalAt = DateTime.UtcNow.AddHours(2),
            PackageCount = 1
        };
        var plugin = MakeTransferPlugin(transfer);

        var results = await DeserializeResults(plugin.DetectFacilityDistanceViolationAsync());

        Assert.Empty(results);
    }

    // --- Package Quantity Discrepancy ---

    [Fact]
    public async Task DetectQuantityDiscrepancy_3MissingFrom50_ReturnsAnomaly()
    {
        var transfer = MakeTransfer(estimatedHours: 2, actualHours: 2.5) with { PackageCount = 50, ActualPackageCount = 47 };
        var plugin = MakeTransferPlugin(transfer);

        var results = await DeserializeResults(plugin.DetectPackageQuantityDiscrepancyAsync());

        var anomaly = Assert.Single(results);
        Assert.Equal("PackageQuantityDiscrepancy", anomaly.AnomalyType);
    }

    [Fact]
    public async Task DetectQuantityDiscrepancy_AllAccountedFor_ReturnsEmpty()
    {
        var transfer = MakeTransfer(estimatedHours: 2, actualHours: 2.5) with { PackageCount = 10, ActualPackageCount = 10 };
        var plugin = MakeTransferPlugin(transfer);

        var results = await DeserializeResults(plugin.DetectPackageQuantityDiscrepancyAsync());

        Assert.Empty(results);
    }

    [Fact]
    public async Task DetectQuantityDiscrepancy_LargeShortage_ReturnsCritical()
    {
        var transfer = MakeTransfer(estimatedHours: 2, actualHours: 2.5) with { PackageCount = 20, ActualPackageCount = 10 };
        var plugin = MakeTransferPlugin(transfer);

        var results = await DeserializeResults(plugin.DetectPackageQuantityDiscrepancyAsync());

        var anomaly = Assert.Single(results);
        Assert.Equal("Critical", anomaly.Severity);
    }

    // --- Lab Test Anomaly ---

    [Fact]
    public async Task DetectLabTestAnomaly_TransferredWithNoTests_ReturnsCritical()
    {
        var plugin = MakePackagePlugin(new PackageLabDto
        {
            PackageId = Guid.NewGuid(),
            Tag = "TEST-PKG-001",
            HasBeenTransferred = true,
            LabTests = []
        });

        var results = await DeserializeResults(plugin.DetectLabTestAnomalyAsync());

        var anomaly = Assert.Single(results);
        Assert.Equal("MissingLabTest", anomaly.AnomalyType);
        Assert.Equal("Critical", anomaly.Severity);
        Assert.Contains("no lab tests on record", anomaly.Description);
    }

    [Fact]
    public async Task DetectLabTestAnomaly_TransferredWithFailedTests_ReturnsCritical()
    {
        var plugin = MakePackagePlugin(new PackageLabDto
        {
            PackageId = Guid.NewGuid(),
            Tag = "TEST-PKG-002",
            HasBeenTransferred = true,
            LabTests = [new LabTestDto { TestType = "Potency", OverallPassed = false, ResultDate = DateTime.UtcNow }]
        });

        var results = await DeserializeResults(plugin.DetectLabTestAnomalyAsync());

        var anomaly = Assert.Single(results);
        Assert.Contains("no passing lab test", anomaly.Description);
    }

    [Fact]
    public async Task DetectLabTestAnomaly_TransferredWithPassingTest_ReturnsEmpty()
    {
        var plugin = MakePackagePlugin(new PackageLabDto
        {
            PackageId = Guid.NewGuid(),
            Tag = "TEST-PKG-003",
            HasBeenTransferred = true,
            LabTests = [new LabTestDto { TestType = "Potency", OverallPassed = true, ResultDate = DateTime.UtcNow }]
        });

        var results = await DeserializeResults(plugin.DetectLabTestAnomalyAsync());

        Assert.Empty(results);
    }

    [Fact]
    public async Task DetectLabTestAnomaly_NotTransferred_SkipsPackage()
    {
        var plugin = MakePackagePlugin(new PackageLabDto
        {
            PackageId = Guid.NewGuid(),
            Tag = "TEST-PKG-004",
            HasBeenTransferred = false,
            LabTests = []
        });

        var results = await DeserializeResults(plugin.DetectLabTestAnomalyAsync());

        Assert.Empty(results);
    }

    // --- RunAllTransferChecksAsync ---

    [Fact]
    public async Task RunAllTransferChecks_ReturnsTransferAnomaliesOnly()
    {
        var transfer = MakeTransfer(estimatedHours: 2, actualHours: 72);
        var package = new PackageLabDto
        {
            PackageId = Guid.NewGuid(),
            Tag = "TEST-PKG-ALL",
            HasBeenTransferred = true,
            LabTests = []
        };
        var plugin = new CustodyAnomalyPlugin(
            JsonSerializer.Serialize(new[] { transfer }),
            JsonSerializer.Serialize(new[] { package }));

        var results = await plugin.RunAllTransferChecksAsync();

        Assert.Contains(results, r => r.AnomalyType == "TransferTimingGap");
        Assert.DoesNotContain(results, r => r.AnomalyType == "MissingLabTest");
    }

    [Fact]
    public async Task LabAndTransferChecks_CalledSeparately_FindBothTypes()
    {
        var transfer = MakeTransfer(estimatedHours: 2, actualHours: 72);
        var package = new PackageLabDto
        {
            PackageId = Guid.NewGuid(),
            Tag = "TEST-PKG-ALL",
            HasBeenTransferred = true,
            LabTests = []
        };
        var plugin = new CustodyAnomalyPlugin(
            JsonSerializer.Serialize(new[] { transfer }),
            JsonSerializer.Serialize(new[] { package }));

        var transferResults = await plugin.RunAllTransferChecksAsync();
        var labResults = await DeserializeResults(plugin.DetectLabTestAnomalyAsync());

        Assert.Contains(transferResults, r => r.AnomalyType == "TransferTimingGap");
        Assert.Contains(labResults, r => r.AnomalyType == "MissingLabTest");
    }

    // --- Helpers ---

    private static TransferDto MakeTransfer(double estimatedHours, double actualHours)
    {
        var departure = new DateTime(2024, 6, 15, 8, 0, 0, DateTimeKind.Utc);
        return new TransferDto
        {
            TransferId = Guid.NewGuid(),
            ManifestNumber = "TEST-MAN-001",
            PackageCount = 1,
            EstimatedDepartureAt = departure,
            EstimatedArrivalAt = departure.AddHours(estimatedHours),
            ActualDepartureAt = departure,
            ActualArrivalAt = departure.AddHours(actualHours)
        };
    }

    private static CustodyAnomalyPlugin MakeTransferPlugin(params TransferDto[] transfers)
        => new(JsonSerializer.Serialize(transfers));

    private static CustodyAnomalyPlugin MakePackagePlugin(params PackageLabDto[] packages)
        => new("[]", JsonSerializer.Serialize(packages));

    private static async Task<List<AnomalyResult>> DeserializeResults(Task<string> task)
    {
        var json = await task;
        return JsonSerializer.Deserialize<List<AnomalyResult>>(json, JsonOptions) ?? [];
    }
}
