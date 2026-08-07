using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.IntegrationTests;

[Collection("Database")]
public class TenantIsolationTests
{
    private readonly DatabaseFixture _fixture;

    public TenantIsolationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Packages_AreIsolated_PerFacility()
    {
        // Arrange — seed a package in each facility
        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityA_Id))
        {
            db.Packages.Add(new Package
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityA_Id,
                Tag = "ISOPKG-A-001",
                ItemName = "Facility A Flower",
                ItemCategory = "Flower",
                Quantity = 100m,
                UnitOfMeasure = "Grams",
                Status = "Active",
                PackagedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityB_Id))
        {
            db.Packages.Add(new Package
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityB_Id,
                Tag = "ISOPKG-B-001",
                ItemName = "Facility B Edible",
                ItemCategory = "Edible",
                Quantity = 50m,
                UnitOfMeasure = "Each",
                Status = "Active",
                PackagedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // Act — query from each facility's context
        await using var dbA = _fixture.CreateContext(DatabaseFixture.FacilityA_Id);
        await using var dbB = _fixture.CreateContext(DatabaseFixture.FacilityB_Id);

        var packagesA = await dbA.Packages.ToListAsync();
        var packagesB = await dbB.Packages.ToListAsync();

        // Assert — each facility only sees its own packages
        Assert.All(packagesA, p => Assert.Equal(DatabaseFixture.FacilityA_Id, p.FacilityId));
        Assert.All(packagesB, p => Assert.Equal(DatabaseFixture.FacilityB_Id, p.FacilityId));
        Assert.DoesNotContain(packagesA, p => p.Tag == "ISOPKG-B-001");
        Assert.DoesNotContain(packagesB, p => p.Tag == "ISOPKG-A-001");
    }

    [Fact]
    public async Task Transfers_AreIsolated_PerFacility()
    {
        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityA_Id))
        {
            db.Transfers.Add(new Transfer
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityA_Id,
                ManifestNumber = "ISO-MAN-A-001",
                ShipperFacilityLicenseNumber = "OR-CUL-00100",
                ShipperFacilityName = "Facility A",
                RecipientFacilityLicenseNumber = "OR-RET-00200",
                RecipientFacilityName = "Facility B",
                TransporterName = "Test Transport",
                DriverName = "Test Driver",
                VehicleLicensePlate = "TEST-001",
                PackageCount = 1,
                EstimatedDepartureAt = DateTime.UtcNow,
                EstimatedArrivalAt = DateTime.UtcNow.AddHours(2),
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityB_Id))
        {
            db.Transfers.Add(new Transfer
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityB_Id,
                ManifestNumber = "ISO-MAN-B-001",
                ShipperFacilityLicenseNumber = "OR-RET-00200",
                ShipperFacilityName = "Facility B",
                RecipientFacilityLicenseNumber = "OR-CUL-00100",
                RecipientFacilityName = "Facility A",
                TransporterName = "Test Transport",
                DriverName = "Test Driver",
                VehicleLicensePlate = "TEST-002",
                PackageCount = 1,
                EstimatedDepartureAt = DateTime.UtcNow,
                EstimatedArrivalAt = DateTime.UtcNow.AddHours(2),
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using var dbA = _fixture.CreateContext(DatabaseFixture.FacilityA_Id);
        await using var dbB = _fixture.CreateContext(DatabaseFixture.FacilityB_Id);

        var transfersA = await dbA.Transfers.ToListAsync();
        var transfersB = await dbB.Transfers.ToListAsync();

        Assert.All(transfersA, t => Assert.Equal(DatabaseFixture.FacilityA_Id, t.FacilityId));
        Assert.All(transfersB, t => Assert.Equal(DatabaseFixture.FacilityB_Id, t.FacilityId));
        Assert.DoesNotContain(transfersA, t => t.ManifestNumber == "ISO-MAN-B-001");
        Assert.DoesNotContain(transfersB, t => t.ManifestNumber == "ISO-MAN-A-001");
    }

    [Fact]
    public async Task LabTests_AreIsolated_PerFacility()
    {
        Guid pkgAId, pkgBId;

        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityA_Id))
        {
            var pkg = new Package
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityA_Id,
                Tag = "ISOLAB-PKG-A-001",
                ItemName = "Lab Test Package A",
                ItemCategory = "Flower",
                Quantity = 10m,
                UnitOfMeasure = "Grams",
                Status = "Active",
                PackagedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            pkgAId = pkg.Id;
            db.Packages.Add(pkg);
            db.LabTests.Add(new LabTest
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityA_Id,
                PackageId = pkg.Id,
                TestType = "Potency",
                OverallPassed = true,
                ResultDate = DateTime.UtcNow,
                LabFacilityName = "Test Lab",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityB_Id))
        {
            var pkg = new Package
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityB_Id,
                Tag = "ISOLAB-PKG-B-001",
                ItemName = "Lab Test Package B",
                ItemCategory = "Edible",
                Quantity = 20m,
                UnitOfMeasure = "Each",
                Status = "Active",
                PackagedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            pkgBId = pkg.Id;
            db.Packages.Add(pkg);
            db.LabTests.Add(new LabTest
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityB_Id,
                PackageId = pkg.Id,
                TestType = "Pesticides",
                OverallPassed = false,
                ResultDate = DateTime.UtcNow,
                LabFacilityName = "Test Lab",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using var dbA = _fixture.CreateContext(DatabaseFixture.FacilityA_Id);
        await using var dbB = _fixture.CreateContext(DatabaseFixture.FacilityB_Id);

        var labTestsA = await dbA.LabTests.ToListAsync();
        var labTestsB = await dbB.LabTests.ToListAsync();

        Assert.All(labTestsA, l => Assert.Equal(DatabaseFixture.FacilityA_Id, l.FacilityId));
        Assert.All(labTestsB, l => Assert.Equal(DatabaseFixture.FacilityB_Id, l.FacilityId));
        Assert.DoesNotContain(labTestsA, l => l.PackageId == pkgBId);
        Assert.DoesNotContain(labTestsB, l => l.PackageId == pkgAId);
    }

    [Fact]
    public async Task AnomalyFlags_AreIsolated_PerFacility()
    {
        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityA_Id))
        {
            db.AnomalyFlags.Add(new AnomalyFlag
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityA_Id,
                AnomalyType = "TransferTimingGap",
                Description = "Facility A anomaly",
                Severity = "High",
                IsResolved = false,
                DetectedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext(DatabaseFixture.FacilityB_Id))
        {
            db.AnomalyFlags.Add(new AnomalyFlag
            {
                Id = Guid.NewGuid(),
                FacilityId = DatabaseFixture.FacilityB_Id,
                AnomalyType = "MissingLabTest",
                Description = "Facility B anomaly",
                Severity = "Critical",
                IsResolved = false,
                DetectedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        await using var dbA = _fixture.CreateContext(DatabaseFixture.FacilityA_Id);
        await using var dbB = _fixture.CreateContext(DatabaseFixture.FacilityB_Id);

        var anomaliesA = await dbA.AnomalyFlags.ToListAsync();
        var anomaliesB = await dbB.AnomalyFlags.ToListAsync();

        Assert.All(anomaliesA, a => Assert.Equal(DatabaseFixture.FacilityA_Id, a.FacilityId));
        Assert.All(anomaliesB, a => Assert.Equal(DatabaseFixture.FacilityB_Id, a.FacilityId));
        Assert.DoesNotContain(anomaliesA, a => a.Description == "Facility B anomaly");
        Assert.DoesNotContain(anomaliesB, a => a.Description == "Facility A anomaly");
    }

    [Fact]
    public async Task QueryWithWrongTenant_ReturnsEmpty()
    {
        var nonexistentTenantId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");

        await using var db = _fixture.CreateContext(nonexistentTenantId);

        var packages = await db.Packages.ToListAsync();
        var transfers = await db.Transfers.ToListAsync();
        var labTests = await db.LabTests.ToListAsync();
        var anomalies = await db.AnomalyFlags.ToListAsync();

        Assert.Empty(packages);
        Assert.Empty(transfers);
        Assert.Empty(labTests);
        Assert.Empty(anomalies);
    }
}
