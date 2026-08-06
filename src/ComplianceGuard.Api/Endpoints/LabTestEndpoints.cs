using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Api.Endpoints;

public static class LabTestEndpoints
{
    public static IEndpointRouteBuilder MapLabTestEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/labtests").WithTags("Lab Tests");

        group.MapGet("/by-package/{packageId:guid}", async (Guid packageId, AppDbContext db) =>
        {
            var tests = await db.LabTests
                .Where(l => l.PackageId == packageId)
                .OrderByDescending(l => l.ResultDate)
                .ToListAsync();
            return Results.Ok(tests.Select(ToResponse));
        });

        group.MapPost("/", async (CreateLabTestRequest request, ITenantContext tenant, AppDbContext db) =>
        {
            var labTest = new LabTest
            {
                Id = Guid.NewGuid(),
                FacilityId = tenant.TenantId,
                PackageId = request.PackageId,
                TestType = request.TestType,
                OverallPassed = request.OverallPassed,
                ResultDate = request.ResultDate,
                LabFacilityName = request.LabFacilityName,
                CreatedAt = DateTime.UtcNow
            };

            db.LabTests.Add(labTest);

            var package = await db.Packages.FindAsync(request.PackageId);
            if (package is not null)
            {
                package.LabTestStatus = request.OverallPassed ? "TestPassed" : "TestFailed";
            }

            await db.SaveChangesAsync();
            return Results.Created($"/labtests/{labTest.Id}", ToResponse(labTest));
        });

        return routes;
    }

    private static LabTestResponse ToResponse(LabTest l) => new(
        l.Id, l.FacilityId, l.PackageId, l.TestType,
        l.OverallPassed, l.ResultDate, l.LabFacilityName, l.CreatedAt);
}

public record CreateLabTestRequest(
    Guid PackageId,
    string TestType,
    bool OverallPassed,
    DateTime ResultDate,
    string LabFacilityName);

public record LabTestResponse(
    Guid Id,
    Guid FacilityId,
    Guid PackageId,
    string TestType,
    bool OverallPassed,
    DateTime ResultDate,
    string LabFacilityName,
    DateTime CreatedAt);
