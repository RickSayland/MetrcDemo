using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ComplianceGuard.Infrastructure.Persistence;

public static class DataSeeder
{
    // Deterministic GUIDs so seed data is idempotent
    public static readonly Guid PortlandFacilityId = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000001");
    public static readonly Guid EugeneFacilityId = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000002");
    public static readonly Guid BendLabFacilityId = Guid.Parse("a1b2c3d4-0001-0001-0001-000000000003");

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        if (await db.Facilities.AnyAsync())
            return;

        var facilities = CreateFacilities();
        db.Facilities.AddRange(facilities);

        var packages = CreatePackages();
        db.Packages.AddRange(packages);

        var transfers = CreateTransfers();
        db.Transfers.AddRange(transfers);

        var transferPackages = CreateTransferPackages();
        db.TransferPackages.AddRange(transferPackages);

        var labTests = CreateLabTests();
        db.LabTests.AddRange(labTests);

        await db.SaveChangesAsync();
    }

    private static List<Facility> CreateFacilities() =>
    [
        new()
        {
            Id = PortlandFacilityId,
            LicenseNumber = "OR-CUL-00142",
            Name = "Emerald Valley Cultivation",
            FacilityType = "Cultivator",
            State = "OR",
            City = "Portland",
            Latitude = 45.5152,
            Longitude = -122.6784,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = EugeneFacilityId,
            LicenseNumber = "OR-RET-00287",
            Name = "Green Leaf Dispensary",
            FacilityType = "Retailer",
            State = "OR",
            City = "Eugene",
            Latitude = 44.0521,
            Longitude = -123.0868,
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 20, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = BendLabFacilityId,
            LicenseNumber = "OR-LAB-00051",
            Name = "Cascade Analytical Labs",
            FacilityType = "Laboratory",
            State = "OR",
            City = "Bend",
            Latitude = 44.0582,
            Longitude = -121.3153,
            IsActive = true,
            CreatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    private static readonly Guid Pkg1 = Guid.Parse("b2b2c3d4-0002-0002-0002-000000000001");
    private static readonly Guid Pkg2 = Guid.Parse("b2b2c3d4-0002-0002-0002-000000000002");
    private static readonly Guid Pkg3 = Guid.Parse("b2b2c3d4-0002-0002-0002-000000000003");
    private static readonly Guid Pkg4 = Guid.Parse("b2b2c3d4-0002-0002-0002-000000000004");
    private static readonly Guid Pkg5 = Guid.Parse("b2b2c3d4-0002-0002-0002-000000000005");

    private static List<Package> CreatePackages() =>
    [
        new()
        {
            Id = Pkg1,
            FacilityId = PortlandFacilityId,
            Tag = "1A4010300003B01000001",
            ItemName = "Blue Dream - Dried Flower",
            ItemCategory = "Flower",
            Quantity = 453.5924m,
            UnitOfMeasure = "Grams",
            Status = "Active",
            LabTestStatus = "TestPassed",
            PackagedDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Pkg2,
            FacilityId = PortlandFacilityId,
            Tag = "1A4010300003B01000002",
            ItemName = "OG Kush - Trim",
            ItemCategory = "Trim",
            Quantity = 907.1847m,
            UnitOfMeasure = "Grams",
            Status = "Active",
            LabTestStatus = "TestPassed",
            PackagedDate = new DateTime(2024, 6, 5, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 6, 5, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Pkg3,
            FacilityId = PortlandFacilityId,
            Tag = "1A4010300003B01000003",
            ItemName = "Sour Diesel - Concentrate",
            ItemCategory = "Concentrate",
            Quantity = 28.3495m,
            UnitOfMeasure = "Grams",
            Status = "InTransit",
            LabTestStatus = "TestPassed",
            PackagedDate = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 6, 10, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Pkg4,
            FacilityId = PortlandFacilityId,
            Tag = "1A4010300003B01000004",
            ItemName = "Girl Scout Cookies - Edible",
            ItemCategory = "Edible",
            Quantity = 100m,
            UnitOfMeasure = "Each",
            Status = "Active",
            LabTestStatus = null,
            PackagedDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Pkg5,
            FacilityId = EugeneFacilityId,
            Tag = "1A4010300003B01000005",
            ItemName = "Northern Lights - Pre-Roll",
            ItemCategory = "Pre-Roll",
            Quantity = 50m,
            UnitOfMeasure = "Each",
            Status = "Active",
            LabTestStatus = "TestPassed",
            PackagedDate = new DateTime(2024, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2024, 5, 20, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    private static readonly Guid Transfer1 = Guid.Parse("c3c3c3d4-0003-0003-0003-000000000001");
    private static readonly Guid Transfer2 = Guid.Parse("c3c3c3d4-0003-0003-0003-000000000002");
    private static readonly Guid Transfer3 = Guid.Parse("c3c3c3d4-0003-0003-0003-000000000003");

    private static List<Transfer> CreateTransfers() =>
    [
        new()
        {
            Id = Transfer1,
            FacilityId = PortlandFacilityId,
            ManifestNumber = "OR-MAN-2024-001542",
            ShipperFacilityLicenseNumber = "OR-CUL-00142",
            ShipperFacilityName = "Emerald Valley Cultivation",
            RecipientFacilityLicenseNumber = "OR-RET-00287",
            RecipientFacilityName = "Green Leaf Dispensary",
            TransporterName = "Pacific Northwest Transport LLC",
            DriverName = "Marcus Johnson",
            VehicleLicensePlate = "OR-TCH-4521",
            PackageCount = 2,
            EstimatedDepartureAt = new DateTime(2024, 6, 12, 8, 0, 0, DateTimeKind.Utc),
            EstimatedArrivalAt = new DateTime(2024, 6, 12, 10, 30, 0, DateTimeKind.Utc),
            ActualDepartureAt = new DateTime(2024, 6, 12, 8, 15, 0, DateTimeKind.Utc),
            ActualArrivalAt = new DateTime(2024, 6, 12, 10, 45, 0, DateTimeKind.Utc),
            Status = "Received",
            CreatedAt = new DateTime(2024, 6, 11, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Transfer2,
            FacilityId = PortlandFacilityId,
            ManifestNumber = "OR-MAN-2024-001587",
            ShipperFacilityLicenseNumber = "OR-CUL-00142",
            ShipperFacilityName = "Emerald Valley Cultivation",
            RecipientFacilityLicenseNumber = "OR-RET-00287",
            RecipientFacilityName = "Green Leaf Dispensary",
            TransporterName = "Pacific Northwest Transport LLC",
            DriverName = "Sarah Chen",
            VehicleLicensePlate = "OR-TCH-7788",
            PackageCount = 1,
            EstimatedDepartureAt = new DateTime(2024, 6, 18, 9, 0, 0, DateTimeKind.Utc),
            EstimatedArrivalAt = new DateTime(2024, 6, 18, 11, 30, 0, DateTimeKind.Utc),
            ActualDepartureAt = new DateTime(2024, 6, 18, 9, 10, 0, DateTimeKind.Utc),
            ActualArrivalAt = null,
            Status = "InTransit",
            CreatedAt = new DateTime(2024, 6, 17, 0, 0, 0, DateTimeKind.Utc)
        },
        // Suspicious transfer: 2.5h route took 72 hours — possible diversion
        new()
        {
            Id = Transfer3,
            FacilityId = PortlandFacilityId,
            ManifestNumber = "OR-MAN-2024-001601",
            ShipperFacilityLicenseNumber = "OR-CUL-00142",
            ShipperFacilityName = "Emerald Valley Cultivation",
            RecipientFacilityLicenseNumber = "OR-RET-00287",
            RecipientFacilityName = "Green Leaf Dispensary",
            TransporterName = "Pacific Northwest Transport LLC",
            DriverName = "Jake Morrison",
            VehicleLicensePlate = "OR-TCH-9102",
            PackageCount = 5,
            EstimatedDepartureAt = new DateTime(2024, 6, 22, 7, 0, 0, DateTimeKind.Utc),
            EstimatedArrivalAt = new DateTime(2024, 6, 22, 9, 30, 0, DateTimeKind.Utc),
            ActualDepartureAt = new DateTime(2024, 6, 22, 7, 5, 0, DateTimeKind.Utc),
            ActualArrivalAt = new DateTime(2024, 6, 25, 7, 5, 0, DateTimeKind.Utc),
            Status = "Received",
            CreatedAt = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    private static List<TransferPackage> CreateTransferPackages() =>
    [
        new() { TransferId = Transfer1, PackageId = Pkg1 },
        new() { TransferId = Transfer1, PackageId = Pkg2 },
        new() { TransferId = Transfer2, PackageId = Pkg3 },
        new() { TransferId = Transfer3, PackageId = Pkg1 },
        new() { TransferId = Transfer3, PackageId = Pkg4 }
    ];

    private static List<LabTest> CreateLabTests() =>
    [
        new()
        {
            Id = Guid.Parse("d4d4d4d4-0004-0004-0004-000000000001"),
            FacilityId = PortlandFacilityId,
            PackageId = Pkg1,
            TestType = "Potency",
            OverallPassed = true,
            ResultDate = new DateTime(2024, 5, 28, 0, 0, 0, DateTimeKind.Utc),
            LabFacilityName = "Cascade Analytical Labs",
            CreatedAt = new DateTime(2024, 5, 28, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("d4d4d4d4-0004-0004-0004-000000000002"),
            FacilityId = PortlandFacilityId,
            PackageId = Pkg1,
            TestType = "Pesticides",
            OverallPassed = true,
            ResultDate = new DateTime(2024, 5, 29, 0, 0, 0, DateTimeKind.Utc),
            LabFacilityName = "Cascade Analytical Labs",
            CreatedAt = new DateTime(2024, 5, 29, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("d4d4d4d4-0004-0004-0004-000000000003"),
            FacilityId = PortlandFacilityId,
            PackageId = Pkg2,
            TestType = "Potency",
            OverallPassed = true,
            ResultDate = new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc),
            LabFacilityName = "Cascade Analytical Labs",
            CreatedAt = new DateTime(2024, 6, 3, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("d4d4d4d4-0004-0004-0004-000000000004"),
            FacilityId = PortlandFacilityId,
            PackageId = Pkg3,
            TestType = "Residual Solvents",
            OverallPassed = true,
            ResultDate = new DateTime(2024, 6, 8, 0, 0, 0, DateTimeKind.Utc),
            LabFacilityName = "Cascade Analytical Labs",
            CreatedAt = new DateTime(2024, 6, 8, 0, 0, 0, DateTimeKind.Utc)
        },
        new()
        {
            Id = Guid.Parse("d4d4d4d4-0004-0004-0004-000000000005"),
            FacilityId = EugeneFacilityId,
            PackageId = Pkg5,
            TestType = "Potency",
            OverallPassed = true,
            ResultDate = new DateTime(2024, 5, 18, 0, 0, 0, DateTimeKind.Utc),
            LabFacilityName = "Cascade Analytical Labs",
            CreatedAt = new DateTime(2024, 5, 18, 0, 0, 0, DateTimeKind.Utc)
        }
    ];
}
