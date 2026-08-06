using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Infrastructure.Persistence;

public class DapperTransferRepository : ITransferRepository
{
    private readonly ITenantContext _tenantContext;

    public DapperTransferRepository(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Transfer>> GetByPackageTagAsync(string packageTag, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Transfer>> GetByFacilityAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Transfer transfer, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
