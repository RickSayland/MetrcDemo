using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceGuard.Infrastructure.Persistence.Configurations;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Tag)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(p => p.ItemName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.ItemCategory)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Quantity)
            .HasPrecision(18, 4);

        builder.Property(p => p.UnitOfMeasure)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.LabTestStatus)
            .HasMaxLength(30);

        builder.HasIndex(p => p.Tag)
            .IsUnique();

        builder.HasIndex(p => p.FacilityId);

        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Facility)
            .WithMany(f => f.Packages)
            .HasForeignKey(p => p.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
