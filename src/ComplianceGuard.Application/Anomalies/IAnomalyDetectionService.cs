using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Application.Anomalies;

public interface IAnomalyDetectionService
{
    Task<IReadOnlyList<AnomalyFlag>> AnalyzeChainAsync(IReadOnlyList<CustodyEvent> events, CancellationToken ct = default);
}
