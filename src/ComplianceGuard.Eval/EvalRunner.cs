using System.Text.Json;
using ComplianceGuard.Application.Anomalies;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Eval;

public class EvalRunner
{
    private readonly IAnomalyDetectionService _detectionService;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EvalRunner(IAnomalyDetectionService detectionService)
    {
        _detectionService = detectionService;
    }

    public async Task<EvalReport> RunAllAsync(string scenariosDirectory)
    {
        var scenarioFiles = Directory.GetFiles(scenariosDirectory, "scenario-*.json")
            .OrderBy(f => f)
            .ToList();

        var results = new List<ScenarioResult>();

        foreach (var file in scenarioFiles)
        {
            var json = await File.ReadAllTextAsync(file);
            var scenario = JsonSerializer.Deserialize<GoldenScenario>(json, JsonOptions)!;
            var result = await RunScenarioAsync(scenario);
            results.Add(result);
        }

        return new EvalReport
        {
            GeneratedAt = DateTime.UtcNow,
            Results = results
        };
    }

    public async Task<ScenarioResult> RunScenarioAsync(GoldenScenario scenario)
    {
        var facilities = scenario.Facilities.Select(f => new Facility
        {
            Id = f.Id,
            LicenseNumber = f.LicenseNumber,
            Name = f.Name,
            FacilityType = f.FacilityType,
            State = f.State,
            City = f.City,
            Latitude = f.Latitude,
            Longitude = f.Longitude,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var transfers = scenario.Transfers.Select(t => new Transfer
        {
            Id = t.Id,
            FacilityId = t.FacilityId,
            ManifestNumber = t.ManifestNumber,
            ShipperFacilityLicenseNumber = t.ShipperFacilityLicenseNumber,
            ShipperFacilityName = t.ShipperFacilityName,
            RecipientFacilityLicenseNumber = t.RecipientFacilityLicenseNumber,
            RecipientFacilityName = t.RecipientFacilityName,
            TransporterName = t.TransporterName,
            DriverName = t.DriverName,
            VehicleLicensePlate = t.VehicleLicensePlate,
            PackageCount = t.PackageCount,
            EstimatedDepartureAt = t.EstimatedDepartureAt,
            EstimatedArrivalAt = t.EstimatedArrivalAt,
            ActualDepartureAt = t.ActualDepartureAt,
            ActualArrivalAt = t.ActualArrivalAt,
            Status = t.Status,
            Facility = facilities.FirstOrDefault(f => f.Id == t.FacilityId)!,
            TransferPackages = BuildTransferPackages(t)
        }).ToList();

        var packages = scenario.Packages.Select(p => new Package
        {
            Id = p.Id,
            FacilityId = p.FacilityId,
            Tag = p.Tag,
            ItemName = p.ItemName,
            ItemCategory = p.ItemCategory,
            Quantity = p.Quantity,
            UnitOfMeasure = p.UnitOfMeasure,
            Status = p.Status,
            LabTestStatus = p.LabTestStatus,
            PackagedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            TransferPackages = transfers.Any() ? [new TransferPackage()] : []
        }).ToList();

        var labTests = scenario.LabTests.Select(lt => new LabTest
        {
            Id = lt.Id,
            FacilityId = lt.FacilityId,
            PackageId = lt.PackageId,
            TestType = lt.TestType,
            OverallPassed = lt.OverallPassed,
            ResultDate = lt.ResultDate,
            LabFacilityName = lt.LabFacilityName,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var detectedAnomalies = new List<AnomalyFlag>();

        if (transfers.Count > 0)
        {
            var transferAnomalies = await _detectionService.AnalyzeTransfersAsync(transfers, facilities);
            detectedAnomalies.AddRange(transferAnomalies);
        }

        if (packages.Count > 0)
        {
            foreach (var pkg in packages)
            {
                var pkgLabTests = labTests.Where(lt => lt.PackageId == pkg.Id).ToList();
                var pkgTransfers = transfers;
                var pkgAnomalies = await _detectionService.AnalyzePackageHistoryAsync(
                    pkg, pkgTransfers, pkgLabTests);
                detectedAnomalies.AddRange(pkgAnomalies);
            }
        }

        return Score(scenario, detectedAnomalies);
    }

    private static List<TransferPackage> BuildTransferPackages(ScenarioTransfer t)
    {
        var count = t.ActualPackageCount ?? t.PackageCount;
        return Enumerable.Range(0, count)
            .Select(_ => new TransferPackage { TransferId = t.Id, PackageId = Guid.NewGuid() })
            .ToList();
    }

    private static ScenarioResult Score(GoldenScenario scenario, List<AnomalyFlag> detected)
    {
        var expected = scenario.ExpectedAnomalies;
        var matched = new List<ExpectedAnomaly>();
        var missed = new List<ExpectedAnomaly>();
        var falsePositives = new List<DetectedAnomaly>();

        var remaining = detected.Select(d => new DetectedAnomaly
        {
            AnomalyType = d.AnomalyType,
            Severity = d.Severity,
            Description = d.Description
        }).ToList();

        foreach (var exp in expected)
        {
            var match = remaining.FirstOrDefault(d => d.AnomalyType == exp.AnomalyType);
            if (match is not null)
            {
                matched.Add(exp);
                remaining.Remove(match);
            }
            else
            {
                missed.Add(exp);
            }
        }

        falsePositives = remaining;

        var passed = missed.Count == 0 && falsePositives.Count == 0;

        return new ScenarioResult
        {
            ScenarioId = scenario.ScenarioId,
            Description = scenario.Description,
            Passed = passed,
            ExpectedCount = expected.Count,
            DetectedCount = detected.Count,
            MatchedCount = matched.Count,
            MissedAnomalies = missed,
            FalsePositives = falsePositives,
            SeverityMatch = matched.All(m =>
                detected.Any(d => d.AnomalyType == m.AnomalyType && d.Severity == m.Severity))
        };
    }
}

// --- Scenario JSON models ---

public class GoldenScenario
{
    public string ScenarioId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ScenarioFacility> Facilities { get; set; } = [];
    public List<ScenarioTransfer> Transfers { get; set; } = [];
    public List<ScenarioPackage> Packages { get; set; } = [];
    public List<ScenarioLabTest> LabTests { get; set; } = [];
    public List<ExpectedAnomaly> ExpectedAnomalies { get; set; } = [];
}

public class ScenarioFacility
{
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FacilityType { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ScenarioTransfer
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
    public int? ActualPackageCount { get; set; }
    public DateTime EstimatedDepartureAt { get; set; }
    public DateTime EstimatedArrivalAt { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public DateTime? ActualArrivalAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ScenarioPackage
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string ItemCategory { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LabTestStatus { get; set; }
}

public class ScenarioLabTest
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PackageId { get; set; }
    public string TestType { get; set; } = string.Empty;
    public bool OverallPassed { get; set; }
    public DateTime ResultDate { get; set; }
    public string LabFacilityName { get; set; } = string.Empty;
}

public class ExpectedAnomaly
{
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}

// --- Result models ---

public class EvalReport
{
    public DateTime GeneratedAt { get; set; }
    public List<ScenarioResult> Results { get; set; } = [];
    public int TotalScenarios => Results.Count;
    public int PassedScenarios => Results.Count(r => r.Passed);
    public int FailedScenarios => Results.Count(r => !r.Passed);
    public double Score => TotalScenarios > 0 ? (double)PassedScenarios / TotalScenarios * 100 : 0;
}

public class ScenarioResult
{
    public string ScenarioId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public int ExpectedCount { get; set; }
    public int DetectedCount { get; set; }
    public int MatchedCount { get; set; }
    public bool SeverityMatch { get; set; }
    public List<ExpectedAnomaly> MissedAnomalies { get; set; } = [];
    public List<DetectedAnomaly> FalsePositives { get; set; } = [];
}

public class DetectedAnomaly
{
    public string AnomalyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
