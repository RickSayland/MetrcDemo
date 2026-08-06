using ComplianceGuard.Application.Anomalies;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Infrastructure.Ai;

public class AnomalyDetectionAgent : IAnomalyDetectionService
{
    public Task<IReadOnlyList<AnomalyFlag>> AnalyzeChainAsync(IReadOnlyList<CustodyEvent> events, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
