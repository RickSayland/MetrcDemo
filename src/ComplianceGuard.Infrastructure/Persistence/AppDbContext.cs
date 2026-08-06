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

    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferPackage> TransferPackages => Set<TransferPackage>();
    public DbSet<LabTest> LabTests => Set<LabTest>();
    public DbSet<AnomalyFlag> AnomalyFlags => Set<AnomalyFlag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Package>().HasQueryFilter(p => p.FacilityId == _tenantContext.TenantId);
        modelBuilder.Entity<Transfer>().HasQueryFilter(t => t.FacilityId == _tenantContext.TenantId);
        modelBuilder.Entity<LabTest>().HasQueryFilter(l => l.FacilityId == _tenantContext.TenantId);
        modelBuilder.Entity<AnomalyFlag>().HasQueryFilter(a => a.FacilityId == _tenantContext.TenantId);
    }
}
