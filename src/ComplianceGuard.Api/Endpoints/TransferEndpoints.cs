using ComplianceGuard.Application.Transfers;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Api.Endpoints;

public static class TransferEndpoints
{
    public static IEndpointRouteBuilder MapTransferEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/transfers").WithTags("Transfers");

        group.MapGet("/", async (AppDbContext db) =>
        {
            var transfers = await db.Transfers
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return Results.Ok(transfers.Select(ToResponse));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var transfer = await db.Transfers
                .Include(t => t.TransferPackages)
                .ThenInclude(tp => tp.Package)
                .FirstOrDefaultAsync(t => t.Id == id);
            return transfer is null ? Results.NotFound() : Results.Ok(ToDetailResponse(transfer));
        });

        group.MapGet("/by-package/{tag}", async (string tag, GetPackageTransferHistoryHandler handler) =>
        {
            var transfers = await handler.HandleAsync(tag);
            return Results.Ok(transfers.Select(ToResponse));
        });

        group.MapPost("/", async (CreateTransferRequest request, RecordTransferHandler handler) =>
        {
            var command = new RecordTransferCommand(
                request.ManifestNumber,
                request.ShipperFacilityLicenseNumber,
                request.ShipperFacilityName,
                request.RecipientFacilityLicenseNumber,
                request.RecipientFacilityName,
                request.TransporterName,
                request.DriverName,
                request.VehicleLicensePlate,
                request.PackageCount,
                request.EstimatedDepartureAt,
                request.EstimatedArrivalAt);

            var transfer = await handler.HandleAsync(command);
            return Results.Created($"/transfers/{transfer.Id}", ToResponse(transfer));
        });

        return routes;
    }

    private static TransferResponse ToResponse(Transfer t) => new(
        t.Id, t.FacilityId, t.ManifestNumber,
        t.ShipperFacilityLicenseNumber, t.ShipperFacilityName,
        t.RecipientFacilityLicenseNumber, t.RecipientFacilityName,
        t.TransporterName, t.DriverName, t.VehicleLicensePlate,
        t.PackageCount, t.EstimatedDepartureAt, t.EstimatedArrivalAt,
        t.ActualDepartureAt, t.ActualArrivalAt, t.Status, t.CreatedAt);

    private static TransferDetailResponse ToDetailResponse(Transfer t) => new(
        t.Id, t.FacilityId, t.ManifestNumber,
        t.ShipperFacilityLicenseNumber, t.ShipperFacilityName,
        t.RecipientFacilityLicenseNumber, t.RecipientFacilityName,
        t.TransporterName, t.DriverName, t.VehicleLicensePlate,
        t.PackageCount, t.EstimatedDepartureAt, t.EstimatedArrivalAt,
        t.ActualDepartureAt, t.ActualArrivalAt, t.Status, t.CreatedAt,
        t.TransferPackages.Select(tp => tp.Package.Tag).ToList());
}

public record CreateTransferRequest(
    string ManifestNumber,
    string ShipperFacilityLicenseNumber,
    string ShipperFacilityName,
    string RecipientFacilityLicenseNumber,
    string RecipientFacilityName,
    string TransporterName,
    string DriverName,
    string VehicleLicensePlate,
    int PackageCount,
    DateTime EstimatedDepartureAt,
    DateTime EstimatedArrivalAt);

public record TransferResponse(
    Guid Id,
    Guid FacilityId,
    string ManifestNumber,
    string ShipperFacilityLicenseNumber,
    string ShipperFacilityName,
    string RecipientFacilityLicenseNumber,
    string RecipientFacilityName,
    string TransporterName,
    string DriverName,
    string VehicleLicensePlate,
    int PackageCount,
    DateTime EstimatedDepartureAt,
    DateTime EstimatedArrivalAt,
    DateTime? ActualDepartureAt,
    DateTime? ActualArrivalAt,
    string Status,
    DateTime CreatedAt);

public record TransferDetailResponse(
    Guid Id,
    Guid FacilityId,
    string ManifestNumber,
    string ShipperFacilityLicenseNumber,
    string ShipperFacilityName,
    string RecipientFacilityLicenseNumber,
    string RecipientFacilityName,
    string TransporterName,
    string DriverName,
    string VehicleLicensePlate,
    int PackageCount,
    DateTime EstimatedDepartureAt,
    DateTime EstimatedArrivalAt,
    DateTime? ActualDepartureAt,
    DateTime? ActualArrivalAt,
    string Status,
    DateTime CreatedAt,
    List<string> PackageTags);
