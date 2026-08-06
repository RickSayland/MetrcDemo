using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Application.Anomalies;

public class ReviewAnomalyHandler
{
    public AnomalyFlag Resolve(AnomalyFlag anomaly, string resolution)
    {
        anomaly.IsResolved = true;
        anomaly.Resolution = resolution;
        anomaly.ResolvedAt = DateTime.UtcNow;
        return anomaly;
    }
}
