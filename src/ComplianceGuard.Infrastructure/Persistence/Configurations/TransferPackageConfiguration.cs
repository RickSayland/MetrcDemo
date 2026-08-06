using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceGuard.Infrastructure.Persistence.Configurations;

public class TransferPackageConfiguration : IEntityTypeConfiguration<TransferPackage>
{
    public void Configure(EntityTypeBuilder<TransferPackage> builder)
    {
        builder.HasKey(tp => new { tp.TransferId, tp.PackageId });

        builder.HasOne(tp => tp.Transfer)
            .WithMany(t => t.TransferPackages)
            .HasForeignKey(tp => tp.TransferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tp => tp.Package)
            .WithMany(p => p.TransferPackages)
            .HasForeignKey(tp => tp.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
