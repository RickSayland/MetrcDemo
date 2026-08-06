using ComplianceGuard.Domain.Abstractions;

namespace ComplianceGuard.Application.Anomalies;

public class ReviewAnomalyHandler
{
    private readonly ITenantContext _tenantContext;

    public ReviewAnomalyHandler(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }
}
