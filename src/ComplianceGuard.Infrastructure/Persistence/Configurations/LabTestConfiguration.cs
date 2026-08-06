using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceGuard.Infrastructure.Persistence.Configurations;

public class LabTestConfiguration : IEntityTypeConfiguration<LabTest>
{
    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.TestType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.LabFacilityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(l => l.FacilityId);

        builder.HasIndex(l => l.PackageId);

        builder.HasOne(l => l.Package)
            .WithMany(p => p.LabTests)
            .HasForeignKey(l => l.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
