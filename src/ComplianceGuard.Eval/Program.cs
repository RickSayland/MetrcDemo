using System.Text.Json;
using ComplianceGuard.Eval;
using ComplianceGuard.Infrastructure.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var runner = new EvalRunner(new AnomalyDetectionAgent(
    SemanticKernelFactory.Create(BuildServiceProvider()),
    Microsoft.Extensions.Logging.LoggerFactory.Create(b => b.AddConsole())
        .CreateLogger<AnomalyDetectionAgent>()));

var scenariosDir = Path.Combine(AppContext.BaseDirectory, "GoldenScenarios");
var baselinePath = Path.Combine(AppContext.BaseDirectory, "BaselineResults.json");

Console.WriteLine("=== ComplianceGuard Eval Harness ===");
Console.WriteLine();

var report = await runner.RunAllAsync(scenariosDir);

PrintReport(report);

var baselineComparison = CompareToBaseline(report, baselinePath);

SaveBaseline(report, baselinePath);

var exitCode = report.FailedScenarios > 0 || baselineComparison.HasRegressions ? 1 : 0;
return exitCode;

// --- Helper methods ---

static void PrintReport(EvalReport report)
{
    Console.WriteLine($"{"Scenario",-35} {"Result",-8} {"Expected",-10} {"Detected",-10} {"Matched",-10} {"Severity",-10}");
    Console.WriteLine(new string('-', 93));

    foreach (var r in report.Results)
    {
        var status = r.Passed ? "PASS" : "FAIL";
        var severityStatus = r.SeverityMatch ? "OK" : "MISMATCH";

        Console.ForegroundColor = r.Passed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.Write($"{r.ScenarioId,-35} ");
        Console.Write($"{status,-8} ");
        Console.ResetColor();
        Console.WriteLine($"{r.ExpectedCount,-10} {r.DetectedCount,-10} {r.MatchedCount,-10} {severityStatus,-10}");

        foreach (var missed in r.MissedAnomalies)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  MISSED: {missed.AnomalyType} ({missed.Severity})");
            Console.ResetColor();
        }

        foreach (var fp in r.FalsePositives)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  FALSE+: {fp.AnomalyType} ({fp.Severity}) — {fp.Description}");
            Console.ResetColor();
        }
    }

    Console.WriteLine();
    Console.ForegroundColor = report.FailedScenarios == 0 ? ConsoleColor.Green : ConsoleColor.Red;
    Console.WriteLine($"Score: {report.PassedScenarios}/{report.TotalScenarios} ({report.Score:F0}%)");
    Console.ResetColor();
    Console.WriteLine();
}

static BaselineComparison CompareToBaseline(EvalReport current, string baselinePath)
{
    var comparison = new BaselineComparison();

    if (!File.Exists(baselinePath))
    {
        Console.WriteLine("No baseline found — this run becomes the baseline.");
        return comparison;
    }

    var baselineJson = File.ReadAllText(baselinePath);
    var baseline = JsonSerializer.Deserialize<EvalReport>(baselineJson,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (baseline is null || baseline.Results.Count == 0)
    {
        Console.WriteLine("Baseline is empty — this run becomes the baseline.");
        return comparison;
    }

    Console.WriteLine("=== Baseline Comparison ===");
    Console.WriteLine();

    foreach (var currentResult in current.Results)
    {
        var baselineResult = baseline.Results.FirstOrDefault(
            b => b.ScenarioId == currentResult.ScenarioId);

        if (baselineResult is null)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  NEW: {currentResult.ScenarioId} — {(currentResult.Passed ? "PASS" : "FAIL")}");
            Console.ResetColor();
            continue;
        }

        if (baselineResult.Passed && !currentResult.Passed)
        {
            comparison.HasRegressions = true;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  REGRESSION: {currentResult.ScenarioId} was PASS, now FAIL");
            Console.ResetColor();
        }
        else if (!baselineResult.Passed && currentResult.Passed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  FIXED: {currentResult.ScenarioId} was FAIL, now PASS");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine($"  UNCHANGED: {currentResult.ScenarioId} — {(currentResult.Passed ? "PASS" : "FAIL")}");
        }
    }

    Console.WriteLine();
    return comparison;
}

static void SaveBaseline(EvalReport report, string baselinePath)
{
    var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(baselinePath, json);
    Console.WriteLine($"Baseline saved to {baselinePath}");
}

static IServiceProvider BuildServiceProvider()
{
    var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
    services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(
        new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
    services.AddLogging(b => b.AddConsole());
    return services.BuildServiceProvider();
}

class BaselineComparison
{
    public bool HasRegressions { get; set; }
}
