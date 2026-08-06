using ComplianceGuard.Infrastructure.Ai.Plugins;
using Microsoft.Agents.AI.Workflows;

namespace ComplianceGuard.Infrastructure.Ai.Workflows;

internal sealed partial class CleanReportExecutor() : Executor("CleanReportExecutor")
{
    [MessageHandler]
    private ValueTask<ComplianceReport> HandleAsync(
        ComplianceCheckResult result, IWorkflowContext context)
    {
        var report = new ComplianceReport(
            Status: "Compliant",
            Summary: $"All {result.Transfers.Count} transfers passed compliance checks. " +
                     "No timing gaps, distance violations, quantity discrepancies, or lab test issues detected.",
            Anomalies: new List<AnomalyResult>(),
            RiskAssessment: null);

        return ValueTask.FromResult(report);
    }
}
