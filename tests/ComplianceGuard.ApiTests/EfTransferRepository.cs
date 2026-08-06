using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.ApiTests;

internal class EfTransferRepository : ITransferRepository
{
    private readonly AppDbContext _db;

    public EfTransferRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Transfers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Transfer>> GetByFacilityAsync(CancellationToken ct = default)
        => await _db.Transfers.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Transfer>> GetByPackageTagAsync(string packageTag, CancellationToken ct = default)
        => await _db.Transfers
            .Where(t => t.TransferPackages.Any(tp => tp.Package.Tag == packageTag))
            .OrderBy(t => t.EstimatedDepartureAt)
            .ToListAsync(ct);

    public async Task AddAsync(Transfer transfer, CancellationToken ct = default)
    {
        _db.Transfers.Add(transfer);
        await _db.SaveChangesAsync(ct);
    }
}
