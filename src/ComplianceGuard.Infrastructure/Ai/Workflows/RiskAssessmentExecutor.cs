using System.Text.Json;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ComplianceGuard.Infrastructure.Ai.Workflows;

internal sealed partial class RiskAssessmentExecutor : Executor
{
    private readonly IChatClient? _chatClient;

    private const string SystemPrompt = """
        You are a cannabis regulatory compliance risk analyst. You have received
        anomaly detection results from an automated rule engine. Your job is to:

        1. Assess the overall risk level (Critical, High, Medium, Low)
        2. Explain what these anomalies mean in plain language for a compliance officer
        3. Recommend specific enforcement actions (hold shipment, audit facility, refer to state agency, etc.)
        4. Identify any patterns that suggest coordinated diversion vs isolated incidents

        Be concise and actionable. This goes directly to compliance officers.
        """;

    public RiskAssessmentExecutor(IChatClient? chatClient) : base("RiskAssessmentExecutor")
    {
        _chatClient = chatClient;
    }

    [MessageHandler]
    private async ValueTask<ComplianceReport> HandleAsync(
        ComplianceCheckResult result, IWorkflowContext context)
    {
        string riskAssessment;

        if (_chatClient is not null)
        {
            riskAssessment = await RunLlmAssessmentAsync(result);
        }
        else
        {
            riskAssessment = GenerateDefaultAssessment(result);
        }

        var maxSeverity = result.Anomalies
            .Select(a => a.Severity)
            .OrderByDescending(s => s switch
            {
                "Critical" => 4, "High" => 3, "Medium" => 2, "Low" => 1, _ => 0
            })
            .FirstOrDefault() ?? "Unknown";

        return new ComplianceReport(
            Status: "Violations Detected",
            Summary: $"{result.Anomalies.Count} anomal{(result.Anomalies.Count == 1 ? "y" : "ies")} detected. " +
                     $"Highest severity: {maxSeverity}.",
            Anomalies: result.Anomalies,
            RiskAssessment: riskAssessment);
    }

    private async Task<string> RunLlmAssessmentAsync(ComplianceCheckResult result)
    {
        var anomalySummary = JsonSerializer.Serialize(result.Anomalies, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,
                $"Analyze these anomalies detected for facility {result.FacilityId}:\n\n{anomalySummary}")
        };

        var response = await _chatClient!.GetResponseAsync(messages);
        return response.Text;
    }

    private static string GenerateDefaultAssessment(ComplianceCheckResult result)
    {
        var critical = result.Anomalies.Count(a => a.Severity == "Critical");
        var high = result.Anomalies.Count(a => a.Severity == "High");

        var actions = new List<string>();
        if (critical > 0)
            actions.Add("Immediate facility audit recommended");
        if (result.Anomalies.Any(a => a.AnomalyType == "FacilityDistanceViolation"))
            actions.Add("Refer manifest data to state regulatory agency");
        if (result.Anomalies.Any(a => a.AnomalyType == "PackageQuantityDiscrepancy"))
            actions.Add("Hold affected shipments pending inventory reconciliation");
        if (result.Anomalies.Any(a => a.AnomalyType == "MissingLabTest"))
            actions.Add("Quarantine untested product — do not release to dispensary");
        if (result.Anomalies.Any(a => a.AnomalyType == "TransferTimingGap"))
            actions.Add("Request GPS logs and driver statements for affected transfers");

        return $"Risk Level: {(critical > 0 ? "Critical" : high > 0 ? "High" : "Medium")}. " +
               $"Recommended actions: {string.Join("; ", actions)}.";
    }
}
