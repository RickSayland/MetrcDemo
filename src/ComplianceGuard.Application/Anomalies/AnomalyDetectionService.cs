using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Application.Anomalies;

public class AnomalyDetectionService
{
    private readonly IAnomalyDetectionService _aiService;

    public AnomalyDetectionService(IAnomalyDetectionService aiService)
    {
        _aiService = aiService;
    }

    public Task<IReadOnlyList<AnomalyFlag>> DetectAnomaliesAsync(IReadOnlyList<CustodyEvent> events, CancellationToken ct = default)
    {
        return _aiService.AnalyzeChainAsync(events, ct);
    }
}
