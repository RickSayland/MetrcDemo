using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ComplianceGuard.Infrastructure.Persistence.Configurations;

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ManifestNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.ShipperFacilityLicenseNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.ShipperFacilityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.RecipientFacilityLicenseNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.RecipientFacilityName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.TransporterName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.DriverName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.VehicleLicensePlate)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(t => t.ManifestNumber)
            .IsUnique();

        builder.HasIndex(t => t.FacilityId);

        builder.HasIndex(t => t.Status);

        builder.HasOne(t => t.Facility)
            .WithMany(f => f.Transfers)
            .HasForeignKey(t => t.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
