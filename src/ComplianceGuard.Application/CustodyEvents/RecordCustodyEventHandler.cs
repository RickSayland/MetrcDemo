using ComplianceGuard.Domain.Abstractions;

namespace ComplianceGuard.Application.CustodyEvents;

public class RecordCustodyEventHandler
{
    private readonly ICustodyRepository _repository;
    private readonly ITenantContext _tenantContext;

    public RecordCustodyEventHandler(ICustodyRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }
}
