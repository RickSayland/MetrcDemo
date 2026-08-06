using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceGuard.Infrastructure.Persistence.Configurations;

public class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.LicenseNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.FacilityType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(f => f.State)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(f => f.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(f => f.LicenseNumber)
            .IsUnique();

        builder.HasIndex(f => f.State);
    }
}
