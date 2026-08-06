namespace ComplianceGuard.Domain.Abstractions;

public interface ITenantContext
{
    Guid TenantId { get; }
}
