namespace ComplianceGuard.Domain.Entities;

public class TransferPackage
{
    public Guid TransferId { get; set; }
    public Guid PackageId { get; set; }

    public Transfer Transfer { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
