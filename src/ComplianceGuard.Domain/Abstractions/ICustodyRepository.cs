using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Domain.Abstractions;

public interface ICustodyRepository
{
    Task<CustodyEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CustodyEvent>> GetByProductIdAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(CustodyEvent custodyEvent, CancellationToken ct = default);
}
