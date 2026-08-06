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
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader)
                && Guid.TryParse(tenantHeader, out var tenantId))
            {
                tenantCtx.SetTenantId(tenantId);
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
