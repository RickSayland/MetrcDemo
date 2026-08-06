using System.Text.Json;
using ComplianceGuard.Infrastructure.Ai.Plugins;
using Microsoft.Agents.AI.Workflows;

namespace ComplianceGuard.Infrastructure.Ai.Workflows;

internal sealed partial class RuleEngineExecutor() : Executor("RuleEngineExecutor")
{
    [MessageHandler]
    private async ValueTask<ComplianceCheckResult> HandleAsync(
        ComplianceScanRequest request, IWorkflowContext context)
    {
        var transfersJson = JsonSerializer.Serialize(request.Transfers);
        var allAnomalies = await new CustodyAnomalyPlugin().RunAllTransferChecksAsync(transfersJson);
        return new ComplianceCheckResult(allAnomalies, request.Transfers, request.FacilityId);
    }
}
