using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Domain.Abstractions;

public interface ITransferRepository
{
    Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Transfer>> GetByPackageTagAsync(string packageTag, CancellationToken ct = default);
    Task<IReadOnlyList<Transfer>> GetByFacilityAsync(CancellationToken ct = default);
    Task AddAsync(Transfer transfer, CancellationToken ct = default);
}
