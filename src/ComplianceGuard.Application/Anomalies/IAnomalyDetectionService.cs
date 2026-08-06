using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Application.Anomalies;

public interface IAnomalyDetectionService
{
    Task<IReadOnlyList<AnomalyFlag>> AnalyzeTransfersAsync(IReadOnlyList<Transfer> transfers, CancellationToken ct = default);
    Task<IReadOnlyList<AnomalyFlag>> AnalyzePackageHistoryAsync(Package package, IReadOnlyList<Transfer> transfers, IReadOnlyList<LabTest> labTests, CancellationToken ct = default);
}
