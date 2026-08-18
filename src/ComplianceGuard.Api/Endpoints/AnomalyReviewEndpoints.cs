using System.Text.Json;
using ComplianceGuard.Application.Anomalies;
using ComplianceGuard.Domain.Entities;
using ComplianceGuard.Infrastructure.Ai;
using ComplianceGuard.Infrastructure.Ai.Plugins;
using ComplianceGuard.Infrastructure.Ai.Workflows;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.Agents.AI.Workflows;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Api.Endpoints;

public static class AnomalyReviewEndpoints
{
    public static IEndpointRouteBuilder MapAnomalyReviewEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/anomalies").WithTags("Anomalies");

        group.MapGet("/", async (bool? resolved, AppDbContext db) =>
        {
            var query = db.AnomalyFlags.AsQueryable();

            if (resolved.HasValue)
                query = query.Where(a => a.IsResolved == resolved.Value);

            var anomalies = await query
                .OrderByDescending(a => a.DetectedAt)
                .ToListAsync();
            return Results.Ok(anomalies.Select(ToResponse));
        });

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var anomaly = await db.AnomalyFlags.FindAsync(id);
            return anomaly is null ? Results.NotFound() : Results.Ok(ToResponse(anomaly));
        });

        group.MapPut("/{id:guid}/resolve", async (Guid id, ResolveAnomalyRequest request, ReviewAnomalyHandler handler, AppDbContext db) =>
        {
            var anomaly = await db.AnomalyFlags.FindAsync(id);
            if (anomaly is null)
                return Results.NotFound();

            handler.Resolve(anomaly, request.Resolution);
            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(anomaly));
        });

        group.MapPost("/scan", async (IAnomalyDetectionService detectionService, AppDbContext db) =>
        {
            var transfers = await db.Transfers
                .Include(t => t.Facility)
                .Include(t => t.TransferPackages).ThenInclude(tp => tp.Package)
                .ToListAsync();

            var facilities = await db.Facilities.ToListAsync();
            var allFlags = new List<AnomalyFlag>();

            var transferFlags = await detectionService.AnalyzeTransfersAsync(transfers, facilities);
            allFlags.AddRange(transferFlags);

            var transferredPackageIds = transfers
                .SelectMany(t => t.TransferPackages)
                .Select(tp => tp.PackageId)
                .Distinct()
                .ToList();

            var packages = await db.Packages
                .Where(p => transferredPackageIds.Contains(p.Id))
                .ToListAsync();

            var packageDtos = new List<PackageLabDto>();
            foreach (var pkg in packages)
            {
                var labTests = await db.LabTests
                    .Where(lt => lt.PackageId == pkg.Id)
                    .ToListAsync();

                packageDtos.Add(new PackageLabDto
                {
                    PackageId = pkg.Id,
                    Tag = pkg.Tag,
                    LabTestStatus = pkg.LabTestStatus,
                    HasBeenTransferred = true,
                    LabTests = labTests.Select(lt => new LabTestDto
                    {
                        TestType = lt.TestType,
                        OverallPassed = lt.OverallPassed,
                        ResultDate = lt.ResultDate
                    }).ToList()
                });
            }

            if (packageDtos.Count > 0)
            {
                var packagesJson = JsonSerializer.Serialize(packageDtos);
                var labResultsJson = await new CustodyAnomalyPlugin("[]", packagesJson).DetectLabTestAnomalyAsync();
                var labResults = JsonSerializer.Deserialize<List<AnomalyResult>>(labResultsJson, JsonDefaults.CaseInsensitive) ?? [];
                var facilityId = transfers[0].FacilityId;
                allFlags.AddRange(labResults.Select(r => AnomalyDetectionAgent.ToAnomalyFlag(r, facilityId)));
            }

            var newFlags = await DeduplicateAndSaveAsync(allFlags, db);

            return Results.Ok(new ScanResponse(newFlags.Count, newFlags.Select(ToResponse).ToList()));
        });

        group.MapPost("/workflow-scan", async (Workflow workflow, AppDbContext db) =>
        {
            var transfers = await db.Transfers
                .Include(t => t.Facility)
                .Include(t => t.TransferPackages).ThenInclude(tp => tp.Package)
                .ToListAsync();

            var facilities = await db.Facilities.ToListAsync();
            var facilityLookup = facilities.ToDictionary(f => f.LicenseNumber);

            var facilityId = transfers.FirstOrDefault()?.FacilityId ?? Guid.Empty;
            var transferDtos = transfers
                .Select(t => AnomalyDetectionAgent.MapToDto(t, facilityLookup))
                .ToList();

            var transferredPackageIds = transfers
                .SelectMany(t => t.TransferPackages)
                .Select(tp => tp.PackageId)
                .Distinct()
                .ToList();

            var packageDtos = new List<PackageLabDto>();
            foreach (var pkgId in transferredPackageIds)
            {
                var pkg = await db.Packages.FindAsync(pkgId);
                if (pkg is null) continue;

                var labTests = await db.LabTests
                    .Where(lt => lt.PackageId == pkgId)
                    .ToListAsync();

                packageDtos.Add(new PackageLabDto
                {
                    PackageId = pkg.Id,
                    Tag = pkg.Tag,
                    LabTestStatus = pkg.LabTestStatus,
                    HasBeenTransferred = true,
                    LabTests = labTests.Select(lt => new LabTestDto
                    {
                        TestType = lt.TestType,
                        OverallPassed = lt.OverallPassed,
                        ResultDate = lt.ResultDate
                    }).ToList()
                });
            }

            var scanRequest = new ComplianceScanRequest(transferDtos, facilityId, packageDtos);

            var run = await InProcessExecution.RunAsync(workflow, scanRequest);

            ComplianceReport? report = null;
            foreach (var evt in run.NewEvents)
            {
                if (evt is ExecutorCompletedEvent completed && completed.Data is ComplianceReport r)
                    report = r;
            }

            if (report is null)
                return Results.Problem("Workflow produced no output");

            var flags = report.Anomalies
                .Select(a => AnomalyDetectionAgent.ToAnomalyFlag(a, facilityId))
                .ToList();

            var newFlags = await DeduplicateAndSaveAsync(flags, db);

            return Results.Ok(new WorkflowScanResponse(
                report.Status,
                report.Summary,
                newFlags.Count,
                report.RiskAssessment,
                newFlags.Select(ToResponse).ToList()));
        });

        return routes;
    }

    private static async Task<List<AnomalyFlag>> DeduplicateAndSaveAsync(
        List<AnomalyFlag> flags, AppDbContext db)
    {
        var existing = await db.AnomalyFlags
            .Select(a => new { a.AnomalyType, a.TransferId, a.PackageId })
            .ToListAsync();

        var newFlags = flags
            .Where(f => !existing.Any(e =>
                e.AnomalyType == f.AnomalyType &&
                e.TransferId == f.TransferId &&
                e.PackageId == f.PackageId))
            .ToList();

        if (newFlags.Count > 0)
        {
            db.AnomalyFlags.AddRange(newFlags);
            await db.SaveChangesAsync();
        }

        return newFlags;
    }

    private static AnomalyResponse ToResponse(AnomalyFlag a) => new(
        a.Id, a.FacilityId, a.TransferId, a.PackageId,
        a.AnomalyType, a.Description, a.Severity,
        a.IsResolved, a.Resolution, a.DetectedAt, a.ResolvedAt);
}

public record ScanResponse(int AnomaliesDetected, List<AnomalyResponse> Anomalies);

public record WorkflowScanResponse(
    string Status,
    string Summary,
    int AnomaliesDetected,
    string? RiskAssessment,
    List<AnomalyResponse> Anomalies);

public record ResolveAnomalyRequest(string Resolution);

public record AnomalyResponse(
    Guid Id,
    Guid FacilityId,
    Guid? TransferId,
    Guid? PackageId,
    string AnomalyType,
    string Description,
    string Severity,
    bool IsResolved,
    string? Resolution,
    DateTime DetectedAt,
    DateTime? ResolvedAt);
