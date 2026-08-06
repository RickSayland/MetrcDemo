using ComplianceGuard.Domain.Abstractions;

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
        if (context.RequestServices.GetService<ITenantContext>() is HttpTenantContext tenantCtx)
        {
            if (context.Request.Headers.TryGetValue("X-License-Number", out var licenseHeader)
                && !string.IsNullOrWhiteSpace(licenseHeader))
            {
                // In production, resolve LicenseNumber → FacilityId via DB lookup.
                // For now, parse directly if a Guid is passed, or look up by license number.
                if (Guid.TryParse(licenseHeader, out var facilityId))
                {
                    tenantCtx.SetTenantId(facilityId);
                }
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
