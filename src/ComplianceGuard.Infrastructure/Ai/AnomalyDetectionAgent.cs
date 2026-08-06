using ComplianceGuard.Application.Anomalies;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Infrastructure.Ai;

public class AnomalyDetectionAgent : IAnomalyDetectionService
{
    public Task<IReadOnlyList<AnomalyFlag>> AnalyzeTransfersAsync(IReadOnlyList<Transfer> transfers, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<AnomalyFlag>> AnalyzePackageHistoryAsync(Package package, IReadOnlyList<Transfer> transfers, IReadOnlyList<LabTest> labTests, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
