using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Application.Anomalies;

public class AnomalyDetectionService
{
    private readonly IAnomalyDetectionService _aiService;

    public AnomalyDetectionService(IAnomalyDetectionService aiService)
    {
        _aiService = aiService;
    }

    public Task<IReadOnlyList<AnomalyFlag>> DetectTransferAnomaliesAsync(IReadOnlyList<Transfer> transfers, CancellationToken ct = default)
    {
        return _aiService.AnalyzeTransfersAsync(transfers, ct);
    }

    public Task<IReadOnlyList<AnomalyFlag>> DetectPackageAnomaliesAsync(Package package, IReadOnlyList<Transfer> transfers, IReadOnlyList<LabTest> labTests, CancellationToken ct = default)
    {
        return _aiService.AnalyzePackageHistoryAsync(package, transfers, labTests, ct);
    }
}
