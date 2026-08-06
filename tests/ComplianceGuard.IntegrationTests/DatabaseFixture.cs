using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace ComplianceGuard.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public static readonly Guid FacilityA_Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid FacilityB_Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateContext(Guid.Empty);
        await db.Database.MigrateAsync();

        db.Facilities.AddRange(
            new Facility
            {
                Id = FacilityA_Id,
                LicenseNumber = "OR-CUL-00100",
                Name = "Facility A - Cultivator",
                FacilityType = "Cultivator",
                State = "OR",
                City = "Portland",
                Latitude = 45.5,
                Longitude = -122.6,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Facility
            {
                Id = FacilityB_Id,
                LicenseNumber = "OR-RET-00200",
                Name = "Facility B - Retailer",
                FacilityType = "Retailer",
                State = "OR",
                City = "Eugene",
                Latitude = 44.0,
                Longitude = -123.0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await db.SaveChangesAsync();
    }

    public AppDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options, new TestTenantContext(tenantId));
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private class TestTenantContext(Guid tenantId) : ITenantContext
    {
        public Guid TenantId => tenantId;
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;
