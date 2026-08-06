using ComplianceGuard.Application.Anomalies;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.UnitTests;

public class ReviewAnomalyHandlerTests
{
    private readonly ReviewAnomalyHandler _handler = new();

    [Fact]
    public void Resolve_SetsIsResolvedToTrue()
    {
        var anomaly = MakeAnomaly();

        _handler.Resolve(anomaly, "Investigated and cleared");

        Assert.True(anomaly.IsResolved);
    }

    [Fact]
    public void Resolve_SetsResolutionText()
    {
        var anomaly = MakeAnomaly();
        var resolution = "Driver confirmed flat tire via GPS logs";

        _handler.Resolve(anomaly, resolution);

        Assert.Equal(resolution, anomaly.Resolution);
    }

    [Fact]
    public void Resolve_SetsResolvedAtToCurrentTime()
    {
        var anomaly = MakeAnomaly();
        var before = DateTime.UtcNow;

        _handler.Resolve(anomaly, "Resolved");

        Assert.NotNull(anomaly.ResolvedAt);
        Assert.InRange(anomaly.ResolvedAt.Value, before, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Resolve_ReturnsSameAnomalyInstance()
    {
        var anomaly = MakeAnomaly();

        var result = _handler.Resolve(anomaly, "Done");

        Assert.Same(anomaly, result);
    }

    private static AnomalyFlag MakeAnomaly() => new()
    {
        Id = Guid.NewGuid(),
        FacilityId = Guid.NewGuid(),
        AnomalyType = "TransferTimingGap",
        Description = "Test anomaly",
        Severity = "High",
        IsResolved = false,
        DetectedAt = DateTime.UtcNow
    };
}
