using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.RequestServices.GetService<ITenantContext>() is HttpTenantContext tenantCtx
            && context.Request.Headers.TryGetValue("X-License-Number", out var licenseHeader)
            && !string.IsNullOrWhiteSpace(licenseHeader))
        {
            var db = context.RequestServices.GetRequiredService<AppDbContext>();
            var facility = await db.Facilities
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(f => f.LicenseNumber == licenseHeader.ToString());

            if (facility is not null)
            {
                tenantCtx.SetTenantId(facility.Id);
            }
        }

        await _next(context);
    }
}

public class HttpTenantContext : ITenantContext
{
    private Guid _tenantId;

    public Guid TenantId => _tenantId;

    public void SetTenantId(Guid tenantId) => _tenantId = tenantId;
}
