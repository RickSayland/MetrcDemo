using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Api.Endpoints;

public static class FacilityEndpoints
{
    public static IEndpointRouteBuilder MapFacilityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/facilities").WithTags("Facilities");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var facilities = await db.Facilities
                .IgnoreQueryFilters()
                .OrderBy(f => f.State).ThenBy(f => f.Name)
                .ToListAsync();
            return Results.Ok(facilities.Select(ToResponse));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var facility = await db.Facilities
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.Id == id);
            return facility is null ? Results.NotFound() : Results.Ok(ToResponse(facility));
        });

        group.MapPost("/", async (CreateFacilityRequest request, AppDbContext db) =>
        {
            var facility = new Facility
            {
                Id = Guid.NewGuid(),
                LicenseNumber = request.LicenseNumber,
                Name = request.Name,
                FacilityType = request.FacilityType,
                State = request.State,
                City = request.City,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Facilities.Add(facility);
            await db.SaveChangesAsync();
            return Results.Created($"/facilities/{facility.Id}", ToResponse(facility));
        });

        return routes;
    }

    private static FacilityResponse ToResponse(Facility f) => new(
        f.Id, f.LicenseNumber, f.Name, f.FacilityType,
        f.State, f.City, f.Latitude, f.Longitude, f.IsActive, f.CreatedAt);
}

public record CreateFacilityRequest(
    string LicenseNumber,
    string Name,
    string FacilityType,
    string State,
    string City,
    double Latitude,
    double Longitude);

public record FacilityResponse(
    Guid Id,
    string LicenseNumber,
    string Name,
    string FacilityType,
    string State,
    string City,
    double Latitude,
    double Longitude,
    bool IsActive,
    DateTime CreatedAt);
