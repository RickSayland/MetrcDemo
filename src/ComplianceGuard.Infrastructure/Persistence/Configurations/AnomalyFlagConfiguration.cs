using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceGuard.Infrastructure.Persistence.Configurations;

public class AnomalyFlagConfiguration : IEntityTypeConfiguration<AnomalyFlag>
{
    public void Configure(EntityTypeBuilder<AnomalyFlag> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AnomalyType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(a => a.Severity)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Resolution)
            .HasMaxLength(1000);

        builder.HasIndex(a => a.FacilityId);

        builder.HasIndex(a => a.IsResolved);

        builder.HasOne(a => a.Transfer)
            .WithMany(t => t.AnomalyFlags)
            .HasForeignKey(a => a.TransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Package)
            .WithMany()
            .HasForeignKey(a => a.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
