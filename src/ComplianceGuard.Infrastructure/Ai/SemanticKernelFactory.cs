using ComplianceGuard.Infrastructure.Ai.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace ComplianceGuard.Infrastructure.Ai;

public static class SemanticKernelFactory
{
    public static Kernel Create(IServiceProvider services)
    {
        var config = services.GetRequiredService<IConfiguration>();
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(loggerFactory);

        var openAiKey = config["OpenAI:ApiKey"];
        var modelId = config["OpenAI:ModelId"] ?? "gpt-4o-mini";

        if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            builder.AddOpenAIChatCompletion(modelId, openAiKey);
        }

        builder.Plugins.AddFromType<CustodyAnomalyPlugin>();

        return builder.Build();
    }
}
