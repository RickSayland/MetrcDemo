using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Api.Endpoints;

public static class DemoEndpoints
{
    public static IEndpointRouteBuilder MapDemoEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/demo").WithTags("Demo");

        group.MapPost("/reset", async (AppDbContext db) =>
        {
            await db.AnomalyFlags.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.TransferPackages.ExecuteDeleteAsync();
            await db.LabTests.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.Transfers.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.Packages.IgnoreQueryFilters().ExecuteDeleteAsync();
            await db.Facilities.ExecuteDeleteAsync();

            db.ChangeTracker.Clear();
            await DataSeeder.SeedAsync(db);

            return Results.Ok(new { message = "Database reset with demo data." });
        });

        return routes;
    }
}
