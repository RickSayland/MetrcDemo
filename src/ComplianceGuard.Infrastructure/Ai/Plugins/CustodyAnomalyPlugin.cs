using System.ComponentModel;
using System.Text.Json;
using ComplianceGuard.Domain;
using Microsoft.SemanticKernel;

namespace ComplianceGuard.Infrastructure.Ai.Plugins;

public class CustodyAnomalyPlugin
{
    private readonly List<TransferDto> _transfers;
    private readonly List<PackageLabDto> _packages;

    public CustodyAnomalyPlugin(string transfersJson, string? packagesJson = null)
    {
        _transfers = JsonSerializer.Deserialize<List<TransferDto>>(transfersJson, JsonDefaults.CaseInsensitive) ?? [];
        _packages = packagesJson is not null
            ? JsonSerializer.Deserialize<List<PackageLabDto>>(packagesJson, JsonDefaults.CaseInsensitive) ?? []
            : [];
    }

    [KernelFunction("detect_transfer_timing_gap")]
    [Description("Detects suspicious timing gaps between transfer departure and arrival that suggest diversion or unauthorized stops.")]
    public Task<string> DetectTransferTimingGapAsync()
    {
        var anomalies = new List<AnomalyResult>();

        foreach (var t in _transfers)
        {
            if (t.ActualDepartureAt is null || t.ActualArrivalAt is null)
                continue;

            var expectedHours = (t.EstimatedArrivalAt - t.EstimatedDepartureAt).TotalHours;
            var actualHours = (t.ActualArrivalAt.Value - t.ActualDepartureAt.Value).TotalHours;

            if (expectedHours <= 0)
                continue;

            var ratio = actualHours / expectedHours;

            if (ratio >= 10)
            {
                anomalies.Add(new AnomalyResult
                {
                    AnomalyType = AnomalyTypes.TransferTimingGap,
                    Severity = "High",
                    TransferId = t.TransferId,
                    Description = $"Transfer {t.ManifestNumber} took {actualHours:F1}h but was estimated at {expectedHours:F1}h ({ratio:F0}x longer). " +
                                  $"This {(actualHours - expectedHours):F0}-hour gap suggests possible diversion or unauthorized stop."
                });
            }
            else if (ratio >= 3)
            {
                anomalies.Add(new AnomalyResult
                {
                    AnomalyType = AnomalyTypes.TransferTimingGap,
                    Severity = "Medium",
                    TransferId = t.TransferId,
                    Description = $"Transfer {t.ManifestNumber} took {actualHours:F1}h vs estimated {expectedHours:F1}h ({ratio:F1}x longer)."
                });
            }
        }

        return Task.FromResult(JsonSerializer.Serialize(anomalies));
    }

    [KernelFunction("detect_facility_distance_violation")]
    [Description("Detects transfers between facilities that are geographically impossible given the transit time, suggesting manifest fraud.")]
    public Task<string> DetectFacilityDistanceViolationAsync()
    {
        var anomalies = new List<AnomalyResult>();

        foreach (var t in _transfers)
        {
            if (t.ActualDepartureAt is null || t.ActualArrivalAt is null)
                continue;

            var distanceMiles = HaversineDistance(
                t.ShipperLatitude, t.ShipperLongitude,
                t.RecipientLatitude, t.RecipientLongitude);

            var transitHours = (t.ActualArrivalAt.Value - t.ActualDepartureAt.Value).TotalHours;

            if (transitHours <= 0)
                continue;

            var impliedSpeedMph = distanceMiles / transitHours;

            if (impliedSpeedMph > 200)
            {
                anomalies.Add(new AnomalyResult
                {
                    AnomalyType = AnomalyTypes.FacilityDistanceViolation,
                    Severity = "Critical",
                    TransferId = t.TransferId,
                    Description = $"Transfer {t.ManifestNumber} covered {distanceMiles:F0} miles in {transitHours:F1}h " +
                                  $"(implied speed {impliedSpeedMph:F0} mph). Physically impossible — suggests manifest tampering or data entry fraud."
                });
            }
        }

        return Task.FromResult(JsonSerializer.Serialize(anomalies));
    }

    [KernelFunction("detect_package_quantity_discrepancy")]
    [Description("Detects quantity mismatches between manifested package count and actual packages received in a transfer.")]
    public Task<string> DetectPackageQuantityDiscrepancyAsync()
    {
        var anomalies = new List<AnomalyResult>();

        foreach (var t in _transfers)
        {
            if (t.ActualPackageCount is null)
                continue;

            var missing = t.PackageCount - t.ActualPackageCount.Value;

            if (missing > 0)
            {
                var severity = missing >= 5 || (t.PackageCount > 0 && (double)missing / t.PackageCount > 0.1)
                    ? "Critical"
                    : "High";

                anomalies.Add(new AnomalyResult
                {
                    AnomalyType = AnomalyTypes.PackageQuantityDiscrepancy,
                    Severity = severity,
                    TransferId = t.TransferId,
                    Description = $"Transfer {t.ManifestNumber} manifested {t.PackageCount} packages but only {t.ActualPackageCount.Value} received. " +
                                  $"{missing} package(s) unaccounted for in the supply chain."
                });
            }
        }

        return Task.FromResult(JsonSerializer.Serialize(anomalies));
    }

    [KernelFunction("detect_lab_test_anomaly")]
    [Description("Detects packages that were transferred without passing required lab tests — a regulatory violation.")]
    public Task<string> DetectLabTestAnomalyAsync()
    {
        var anomalies = new List<AnomalyResult>();

        foreach (var pkg in _packages)
        {
            if (!pkg.HasBeenTransferred)
                continue;

            var hasPassingTest = pkg.LabTests.Any(lt => lt.OverallPassed);

            if (!hasPassingTest)
            {
                var reason = pkg.LabTests.Count == 0
                    ? "no lab tests on record"
                    : "no passing lab test results";

                anomalies.Add(new AnomalyResult
                {
                    AnomalyType = AnomalyTypes.MissingLabTest,
                    Severity = "Critical",
                    PackageId = pkg.PackageId,
                    Description = $"Package {pkg.Tag} was transferred to a dispensary with {reason}. " +
                                  "Regulatory violation — product must be held until compliant testing is completed."
                });
            }
        }

        return Task.FromResult(JsonSerializer.Serialize(anomalies));
    }

    public async Task<List<AnomalyResult>> RunAllTransferChecksAsync()
    {
        var results = new List<AnomalyResult>();

        var timingJson = await DetectTransferTimingGapAsync();
        results.AddRange(JsonSerializer.Deserialize<List<AnomalyResult>>(timingJson, JsonDefaults.CaseInsensitive) ?? []);

        var distanceJson = await DetectFacilityDistanceViolationAsync();
        results.AddRange(JsonSerializer.Deserialize<List<AnomalyResult>>(distanceJson, JsonDefaults.CaseInsensitive) ?? []);

        var quantityJson = await DetectPackageQuantityDiscrepancyAsync();
        results.AddRange(JsonSerializer.Deserialize<List<AnomalyResult>>(quantityJson, JsonDefaults.CaseInsensitive) ?? []);

        return results;
    }

    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMiles = 3958.8;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMiles * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}

public record TransferDto
{
    public Guid TransferId { get; init; }
    public Guid FacilityId { get; init; }
    public string ManifestNumber { get; init; } = string.Empty;
    public int PackageCount { get; init; }
    public int? ActualPackageCount { get; init; }
    public DateTime EstimatedDepartureAt { get; init; }
    public DateTime EstimatedArrivalAt { get; init; }
    public DateTime? ActualDepartureAt { get; init; }
    public DateTime? ActualArrivalAt { get; init; }
    public string Status { get; init; } = string.Empty;
    public double ShipperLatitude { get; init; }
    public double ShipperLongitude { get; init; }
    public double RecipientLatitude { get; init; }
    public double RecipientLongitude { get; init; }
}

public record PackageLabDto
{
    public Guid PackageId { get; init; }
    public string Tag { get; init; } = string.Empty;
    public string? LabTestStatus { get; init; }
    public bool HasBeenTransferred { get; init; }
    public List<LabTestDto> LabTests { get; init; } = [];
}

public record LabTestDto
{
    public string TestType { get; init; } = string.Empty;
    public bool OverallPassed { get; init; }
    public DateTime ResultDate { get; init; }
}

public record AnomalyResult
{
    public string AnomalyType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public Guid? TransferId { get; init; }
    public Guid? PackageId { get; init; }
}
