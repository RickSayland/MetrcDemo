using ComplianceGuard.Infrastructure.Ai.Plugins;

namespace ComplianceGuard.Infrastructure.Ai.Workflows;

public record ComplianceScanRequest(
    List<TransferDto> Transfers,
    Guid FacilityId,
    List<PackageLabDto>? Packages = null);

public record ComplianceCheckResult(
    List<AnomalyResult> Anomalies,
    List<TransferDto> Transfers,
    Guid FacilityId);

public record ComplianceReport(
    string Status,
    string Summary,
    List<AnomalyResult> Anomalies,
    string? RiskAssessment);
