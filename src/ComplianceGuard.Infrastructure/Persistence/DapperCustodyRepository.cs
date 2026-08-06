using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Infrastructure.Persistence;

public class DapperCustodyRepository : ICustodyRepository
{
    private readonly ITenantContext _tenantContext;

    public DapperCustodyRepository(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Task<CustodyEvent?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<CustodyEvent>> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(CustodyEvent custodyEvent, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
