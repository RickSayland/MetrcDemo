using System.Text.Json;
using ComplianceGuard.Infrastructure.Ai.Plugins;
using Microsoft.Agents.AI.Workflows;

namespace ComplianceGuard.Infrastructure.Ai.Workflows;

internal sealed partial class RuleEngineExecutor() : Executor("RuleEngineExecutor")
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [MessageHandler]
    private async ValueTask<ComplianceCheckResult> HandleAsync(
        ComplianceScanRequest request, IWorkflowContext context)
    {
        var plugin = new CustodyAnomalyPlugin();
        var transfersJson = JsonSerializer.Serialize(request.Transfers);
        var allAnomalies = new List<AnomalyResult>();

        var timingJson = await plugin.DetectTransferTimingGapAsync(transfersJson);
        allAnomalies.AddRange(
            JsonSerializer.Deserialize<List<AnomalyResult>>(timingJson, JsonOptions) ?? []);

        var distanceJson = await plugin.DetectFacilityDistanceViolationAsync(transfersJson);
        allAnomalies.AddRange(
            JsonSerializer.Deserialize<List<AnomalyResult>>(distanceJson, JsonOptions) ?? []);

        var quantityJson = await plugin.DetectPackageQuantityDiscrepancyAsync(transfersJson);
        allAnomalies.AddRange(
            JsonSerializer.Deserialize<List<AnomalyResult>>(quantityJson, JsonOptions) ?? []);

        return new ComplianceCheckResult(allAnomalies, request.Transfers, request.FacilityId);
    }
}
