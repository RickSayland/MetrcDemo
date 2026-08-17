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
        var packagesJson = request.Packages is { Count: > 0 }
            ? JsonSerializer.Serialize(request.Packages)
            : null;
        var plugin = new CustodyAnomalyPlugin(transfersJson, packagesJson);
        var allAnomalies = await plugin.RunAllTransferChecksAsync();

        if (packagesJson is not null)
        {
            var labJson = await plugin.DetectLabTestAnomalyAsync();
            allAnomalies.AddRange(
                JsonSerializer.Deserialize<List<AnomalyResult>>(labJson, JsonDefaults.CaseInsensitive) ?? []);
        }

        return new ComplianceCheckResult(allAnomalies, request.Transfers, request.FacilityId);
    }
}
