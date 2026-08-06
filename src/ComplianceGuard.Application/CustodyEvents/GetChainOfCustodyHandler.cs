using ComplianceGuard.Domain.Abstractions;

namespace ComplianceGuard.Application.CustodyEvents;

public class GetChainOfCustodyHandler
{
    private readonly ICustodyRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetChainOfCustodyHandler(ICustodyRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }
}
