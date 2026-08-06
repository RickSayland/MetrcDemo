using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComplianceGuard.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CustodyEvent> CustodyEvents => Set<CustodyEvent>();
    public DbSet<AnomalyFlag> AnomalyFlags => Set<AnomalyFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>().HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<CustodyEvent>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<AnomalyFlag>().HasQueryFilter(a => a.TenantId == _tenantContext.TenantId);
    }
}
