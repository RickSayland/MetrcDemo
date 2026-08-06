using ComplianceGuard.Domain.Abstractions;

namespace ComplianceGuard.Application.Transfers;

public class GetPackageTransferHistoryHandler
{
    private readonly ITransferRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetPackageTransferHistoryHandler(ITransferRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }
}
