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

            var anomalies = await detectionService.AnalyzeTransfersAsync(transfers, facilities);

            if (anomalies.Count > 0)
            {
                db.AnomalyFlags.AddRange(anomalies);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new ScanResponse(anomalies.Count, anomalies.Select(ToResponse).ToList()));
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

            var scanRequest = new ComplianceScanRequest(transferDtos, facilityId);

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

            if (flags.Count > 0)
            {
                db.AnomalyFlags.AddRange(flags);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new WorkflowScanResponse(
                report.Status,
                report.Summary,
                flags.Count,
                report.RiskAssessment,
                flags.Select(ToResponse).ToList()));
        });

        return routes;
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
