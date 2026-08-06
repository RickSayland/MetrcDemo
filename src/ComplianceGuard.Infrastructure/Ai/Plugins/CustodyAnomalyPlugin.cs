using Microsoft.SemanticKernel;

namespace ComplianceGuard.Infrastructure.Ai.Plugins;

public class CustodyAnomalyPlugin
{
    [KernelFunction("detect_timing_gap")]
    public Task<string> DetectTimingGapAsync()
    {
        throw new NotImplementedException();
    }

    [KernelFunction("detect_location_jump")]
    public Task<string> DetectLocationJumpAsync()
    {
        throw new NotImplementedException();
    }
}
