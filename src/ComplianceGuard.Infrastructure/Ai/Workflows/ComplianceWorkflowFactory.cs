using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ComplianceGuard.Infrastructure.Ai.Workflows;

public static class ComplianceWorkflowFactory
{
    public static Workflow Create(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var chatClient = CreateChatClient(config);

        var ruleEngine = new RuleEngineExecutor();
        var riskAssessment = new RiskAssessmentExecutor(chatClient);
        var cleanReport = new CleanReportExecutor();

        var workflow = new WorkflowBuilder(ruleEngine)
            .AddEdge<ComplianceCheckResult>(ruleEngine, riskAssessment,
                condition: r => r is not null && r.Anomalies.Count > 0)
            .AddEdge<ComplianceCheckResult>(ruleEngine, cleanReport,
                condition: r => r is not null && r.Anomalies.Count == 0)
            .WithOutputFrom(riskAssessment, cleanReport)
            .Build();

        return workflow;
    }

    private static IChatClient? CreateChatClient(IConfiguration config)
    {
        var apiKey = config["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("MetrcOpenAiKey");

        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var modelId = config["OpenAI:ModelId"] ?? "gpt-4o-mini";

        return new OpenAI.OpenAIClient(apiKey)
            .GetChatClient(modelId)
            .AsIChatClient();
    }
}
