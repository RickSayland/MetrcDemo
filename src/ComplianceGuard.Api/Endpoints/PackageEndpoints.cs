using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Api.Endpoints;

public static class PackageEndpoints
{
    public static IEndpointRouteBuilder MapPackageEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/packages").WithTags("Packages");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var packages = await db.Packages
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Results.Ok(packages.Select(ToResponse));
        });

        group.MapGet("/active", async (AppDbContext db) =>
        {
            var packages = await db.Packages
                .Where(p => p.Status == "Active")
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return Results.Ok(packages.Select(ToResponse));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var package = await db.Packages.FindAsync(id);
            return package is null ? Results.NotFound() : Results.Ok(ToResponse(package));
        });

        group.MapGet("/by-tag/{tag}", async (string tag, AppDbContext db) =>
        {
            var package = await db.Packages.FirstOrDefaultAsync(p => p.Tag == tag);
            return package is null ? Results.NotFound() : Results.Ok(ToResponse(package));
        });

        group.MapPost("/", async (CreatePackageRequest request, ITenantContext tenant, AppDbContext db) =>
        {
            var package = new Package
            {
                Id = Guid.NewGuid(),
                FacilityId = tenant.TenantId,
                Tag = request.Tag,
                ItemName = request.ItemName,
                ItemCategory = request.ItemCategory,
                Quantity = request.Quantity,
                UnitOfMeasure = request.UnitOfMeasure,
                Status = "Active",
                PackagedDate = request.PackagedDate,
                CreatedAt = DateTime.UtcNow
            };

            db.Packages.Add(package);
            await db.SaveChangesAsync();
            return Results.Created($"/packages/{package.Id}", ToResponse(package));
        });

        return routes;
    }

    private static PackageResponse ToResponse(Package p) => new(
        p.Id, p.FacilityId, p.Tag, p.ItemName, p.ItemCategory,
        p.Quantity, p.UnitOfMeasure, p.Status, p.LabTestStatus,
        p.PackagedDate, p.CreatedAt);
}

public record CreatePackageRequest(
    string Tag,
    string ItemName,
    string ItemCategory,
    decimal Quantity,
    string UnitOfMeasure,
    DateTime PackagedDate);

public record PackageResponse(
    Guid Id,
    Guid FacilityId,
    string Tag,
    string ItemName,
    string ItemCategory,
    decimal Quantity,
    string UnitOfMeasure,
    string Status,
    string? LabTestStatus,
    DateTime PackagedDate,
    DateTime CreatedAt);
