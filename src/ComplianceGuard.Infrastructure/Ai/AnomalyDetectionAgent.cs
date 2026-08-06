using System.Text.Json;
using ComplianceGuard.Application.Anomalies;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Ai.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ComplianceGuard.Infrastructure.Ai;

public class AnomalyDetectionAgent : IAnomalyDetectionService
{
    private readonly Kernel _kernel;
    private readonly ILogger<AnomalyDetectionAgent> _logger;
    private readonly bool _hasLlm;

    private const string SystemPrompt = """
        You are a cannabis supply chain compliance analyst. Your job is to analyze
        transfer and package data for regulatory violations and suspicious activity.

        You have access to detection functions. Call the appropriate ones based on the
        data provided. Consider:
        - Timing gaps that suggest diversion or unauthorized stops
        - Physically impossible transit speeds that indicate manifest fraud
        - Package count mismatches that suggest theft or loss
        - Missing lab tests that are regulatory violations

        Analyze ALL the data provided and call every relevant detection function.
        After calling the functions, summarize any anomalies found.
        """;

    public AnomalyDetectionAgent(Kernel kernel, ILogger<AnomalyDetectionAgent> logger)
    {
        _kernel = kernel;
        _logger = logger;
        _hasLlm = _kernel.Services.GetService(typeof(IChatCompletionService)) is not null;
    }

    public async Task<IReadOnlyList<AnomalyFlag>> AnalyzeTransfersAsync(
        IReadOnlyList<Transfer> transfers, IReadOnlyList<Facility> facilities, CancellationToken ct = default)
    {
        if (transfers.Count == 0)
            return [];

        var facilityLookup = facilities.ToDictionary(f => f.LicenseNumber);
        var facilityId = transfers[0].FacilityId;
        var transferDtos = transfers.Select(t => MapToDto(t, facilityLookup)).ToList();
        var transfersJson = JsonSerializer.Serialize(transferDtos);

        List<AnomalyResult> allResults;

        if (_hasLlm)
        {
            allResults = await RunWithLlmAsync(
                $"Analyze these transfers for compliance issues:\n{transfersJson}", ct);
        }
        else
        {
            allResults = await RunDirectAsync(transfersJson, ct);
        }

        return allResults.Select(r => ToAnomalyFlag(r, facilityId)).ToList();
    }

    public async Task<IReadOnlyList<AnomalyFlag>> AnalyzePackageHistoryAsync(
        Package package, IReadOnlyList<Transfer> transfers,
        IReadOnlyList<LabTest> labTests, CancellationToken ct = default)
    {
        var packageDto = new PackageLabDto
        {
            PackageId = package.Id,
            Tag = package.Tag,
            LabTestStatus = package.LabTestStatus,
            HasBeenTransferred = transfers.Count > 0,
            LabTests = labTests.Select(lt => new LabTestDto
            {
                TestType = lt.TestType,
                OverallPassed = lt.OverallPassed,
                ResultDate = lt.ResultDate
            }).ToList()
        };

        var packagesJson = JsonSerializer.Serialize(new[] { packageDto });
        var emptyLookup = new Dictionary<string, Facility>();
        var transfersJson = JsonSerializer.Serialize(transfers.Select(t => MapToDto(t, emptyLookup)).ToList());
        List<AnomalyResult> allResults;

        if (_hasLlm)
        {
            allResults = await RunWithLlmAsync(
                $"Analyze this package and its transfer history:\nPackages: {packagesJson}\nTransfers: {transfersJson}", ct);
        }
        else
        {
            allResults = await RunDirectPackageAsync(packagesJson, transfersJson, ct);
        }

        return allResults.Select(r => ToAnomalyFlag(r, package.FacilityId)).ToList();
    }

    private async Task<List<AnomalyResult>> RunWithLlmAsync(string userMessage, CancellationToken ct)
    {
        _logger.LogInformation("Running anomaly detection with LLM orchestration");

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt);
        history.AddUserMessage(userMessage);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatService.GetChatMessageContentAsync(
            history, settings, _kernel, ct);

        _logger.LogInformation("LLM response: {Response}", response.Content);

        return ExtractResultsFromFunctionCalls(history);
    }

    private async Task<List<AnomalyResult>> RunDirectAsync(string transfersJson, CancellationToken ct)
    {
        _logger.LogInformation("Running anomaly detection in direct mode (no LLM configured)");
        return await new CustodyAnomalyPlugin().RunAllTransferChecksAsync(transfersJson);
    }

    private async Task<List<AnomalyResult>> RunDirectPackageAsync(
        string packagesJson, string transfersJson, CancellationToken ct)
    {
        _logger.LogInformation("Running package anomaly detection in direct mode (no LLM configured)");

        var plugin = new CustodyAnomalyPlugin();
        var results = new List<AnomalyResult>();

        var labJson = await plugin.DetectLabTestAnomalyAsync(packagesJson);
        results.AddRange(JsonSerializer.Deserialize<List<AnomalyResult>>(labJson, JsonDefaults.CaseInsensitive) ?? []);

        return results;
    }

    private List<AnomalyResult> ExtractResultsFromFunctionCalls(ChatHistory history)
    {
        var results = new List<AnomalyResult>();

        foreach (var message in history)
        {
            foreach (var item in message.Items)
            {
                if (item is FunctionResultContent functionResult)
                {
                    var json = functionResult.Result?.ToString();
                    if (string.IsNullOrWhiteSpace(json)) continue;

                    var parsed = JsonSerializer.Deserialize<List<AnomalyResult>>(json, JsonDefaults.CaseInsensitive);
                    if (parsed is not null)
                        results.AddRange(parsed);
                }
            }
        }

        return results;
    }

    public static TransferDto MapToDto(Transfer t, Dictionary<string, Facility> facilityLookup)
    {
        facilityLookup.TryGetValue(t.RecipientFacilityLicenseNumber, out var recipient);

        return new TransferDto
        {
            TransferId = t.Id,
            FacilityId = t.FacilityId,
            ManifestNumber = t.ManifestNumber,
            PackageCount = t.PackageCount,
            ActualPackageCount = t.TransferPackages?.Count,
            EstimatedDepartureAt = t.EstimatedDepartureAt,
            EstimatedArrivalAt = t.EstimatedArrivalAt,
            ActualDepartureAt = t.ActualDepartureAt,
            ActualArrivalAt = t.ActualArrivalAt,
            Status = t.Status,
            ShipperLatitude = t.Facility?.Latitude ?? 0,
            ShipperLongitude = t.Facility?.Longitude ?? 0,
            RecipientLatitude = recipient?.Latitude ?? 0,
            RecipientLongitude = recipient?.Longitude ?? 0
        };
    }

    public static AnomalyFlag ToAnomalyFlag(AnomalyResult result, Guid facilityId) => new()
    {
        Id = Guid.NewGuid(),
        FacilityId = facilityId,
        TransferId = result.TransferId,
        PackageId = result.PackageId,
        AnomalyType = result.AnomalyType,
        Description = result.Description,
        Severity = result.Severity,
        IsResolved = false,
        DetectedAt = DateTime.UtcNow
    };
}
