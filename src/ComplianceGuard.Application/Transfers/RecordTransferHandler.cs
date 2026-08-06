using ComplianceGuard.Domain.Abstractions;

namespace ComplianceGuard.Application.Transfers;

public class RecordTransferHandler
{
    private readonly ITransferRepository _repository;
    private readonly ITenantContext _tenantContext;

    public RecordTransferHandler(ITransferRepository repository, ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }
}
