using ComplianceGuard.Application.Anomalies;

namespace ComplianceGuard.Eval;

public class EvalRunner
{
    private readonly IAnomalyDetectionService _detectionService;

    public EvalRunner(IAnomalyDetectionService detectionService)
    {
        _detectionService = detectionService;
    }
}
