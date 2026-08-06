using System.Text.Json;

namespace ComplianceGuard.Infrastructure.Ai;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };
}
