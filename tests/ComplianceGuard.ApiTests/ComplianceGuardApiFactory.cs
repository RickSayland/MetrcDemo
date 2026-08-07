using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ComplianceGuard.ApiTests;

public class ComplianceGuardApiFactory : WebApplicationFactory<Program>
{
    public static readonly Guid PortlandFacilityId = Guid.Parse("a1b2c3d4-0001-4001-8001-000000000001");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // EF Core 10 enforces single-provider — purge SqlServer services before adding InMemory
            var efDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                            d.ImplementationType?.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in efDescriptors)
                services.Remove(d);

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseInMemoryDatabase("ComplianceGuardTests");
            });

            var dapperDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITransferRepository));
            if (dapperDescriptor is not null)
                services.Remove(dapperDescriptor);

            services.AddScoped<ITransferRepository, EfTransferRepository>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    private static void SeedTestData(AppDbContext db)
    {
        if (db.Facilities.Any())
            return;

        db.Facilities.AddRange(
            new Facility
            {
                Id = PortlandFacilityId,
                LicenseNumber = "OR-CUL-00142",
                Name = "Test Cultivator",
                FacilityType = "Cultivator",
                State = "OR",
                City = "Portland",
                Latitude = 45.5152,
                Longitude = -122.6784,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Facility
            {
                Id = Guid.Parse("a1b2c3d4-0001-4001-8001-000000000002"),
                LicenseNumber = "OR-RET-00287",
                Name = "Test Dispensary",
                FacilityType = "Retailer",
                State = "OR",
                City = "Eugene",
                Latitude = 44.0521,
                Longitude = -123.0868,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        db.Packages.Add(new Package
        {
            Id = Guid.Parse("b2b2c3d4-0002-4002-8002-000000000001"),
            FacilityId = PortlandFacilityId,
            Tag = "1A4010300003B01000001",
            ItemName = "Blue Dream - Dried Flower",
            ItemCategory = "Flower",
            Quantity = 453.59m,
            UnitOfMeasure = "Grams",
            Status = "Active",
            LabTestStatus = "TestPassed",
            PackagedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
    }
}
