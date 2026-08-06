using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;

namespace ComplianceGuard.Application.Transfers;

public class GetPackageTransferHistoryHandler
{
    private readonly ITransferRepository _repository;

    public GetPackageTransferHistoryHandler(ITransferRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Transfer>> HandleAsync(string packageTag, CancellationToken ct = default)
    {
        return await _repository.GetByPackageTagAsync(packageTag, ct);
    }
}
