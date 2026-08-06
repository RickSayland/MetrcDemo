using System.Data;
using ComplianceGuard.Domain.Abstractions;
using ComplianceGuard.Domain.Entities;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ComplianceGuard.Infrastructure.Persistence;

public class DapperTransferRepository : ITransferRepository
{
    private readonly string _connectionString;
    private readonly ITenantContext _tenantContext;

    public DapperTransferRepository(IConfiguration configuration, ITenantContext tenantContext)
    {
        _connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not configured.");
        _tenantContext = tenantContext;
    }

    public async Task<Transfer?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, FacilityId, ManifestNumber,
                   ShipperFacilityLicenseNumber, ShipperFacilityName,
                   RecipientFacilityLicenseNumber, RecipientFacilityName,
                   TransporterName, DriverName, VehicleLicensePlate,
                   PackageCount, EstimatedDepartureAt, EstimatedArrivalAt,
                   ActualDepartureAt, ActualArrivalAt, Status, CreatedAt
            FROM Transfers
            WHERE Id = @Id AND FacilityId = @FacilityId
            """;

        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Transfer>(
            sql, new { Id = id, FacilityId = _tenantContext.TenantId });
    }

    public async Task<IReadOnlyList<Transfer>> GetByPackageTagAsync(string packageTag, CancellationToken ct = default)
    {
        const string sql = """
            SELECT t.Id, t.FacilityId, t.ManifestNumber,
                   t.ShipperFacilityLicenseNumber, t.ShipperFacilityName,
                   t.RecipientFacilityLicenseNumber, t.RecipientFacilityName,
                   t.TransporterName, t.DriverName, t.VehicleLicensePlate,
                   t.PackageCount, t.EstimatedDepartureAt, t.EstimatedArrivalAt,
                   t.ActualDepartureAt, t.ActualArrivalAt, t.Status, t.CreatedAt
            FROM Transfers t
            INNER JOIN TransferPackages tp ON tp.TransferId = t.Id
            INNER JOIN Packages p ON p.Id = tp.PackageId
            WHERE p.Tag = @PackageTag AND t.FacilityId = @FacilityId
            ORDER BY t.EstimatedDepartureAt
            """;

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<Transfer>(
            sql, new { PackageTag = packageTag, FacilityId = _tenantContext.TenantId });
        return results.ToList();
    }

    public async Task<IReadOnlyList<Transfer>> GetByFacilityAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT Id, FacilityId, ManifestNumber,
                   ShipperFacilityLicenseNumber, ShipperFacilityName,
                   RecipientFacilityLicenseNumber, RecipientFacilityName,
                   TransporterName, DriverName, VehicleLicensePlate,
                   PackageCount, EstimatedDepartureAt, EstimatedArrivalAt,
                   ActualDepartureAt, ActualArrivalAt, Status, CreatedAt
            FROM Transfers
            WHERE FacilityId = @FacilityId
            ORDER BY CreatedAt DESC
            """;

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<Transfer>(
            sql, new { FacilityId = _tenantContext.TenantId });
        return results.ToList();
    }

    public async Task AddAsync(Transfer transfer, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO Transfers
                (Id, FacilityId, ManifestNumber,
                 ShipperFacilityLicenseNumber, ShipperFacilityName,
                 RecipientFacilityLicenseNumber, RecipientFacilityName,
                 TransporterName, DriverName, VehicleLicensePlate,
                 PackageCount, EstimatedDepartureAt, EstimatedArrivalAt,
                 ActualDepartureAt, ActualArrivalAt, Status, CreatedAt)
            VALUES
                (@Id, @FacilityId, @ManifestNumber,
                 @ShipperFacilityLicenseNumber, @ShipperFacilityName,
                 @RecipientFacilityLicenseNumber, @RecipientFacilityName,
                 @TransporterName, @DriverName, @VehicleLicensePlate,
                 @PackageCount, @EstimatedDepartureAt, @EstimatedArrivalAt,
                 @ActualDepartureAt, @ActualArrivalAt, @Status, @CreatedAt)
            """;

        using var connection = CreateConnection();
        await connection.ExecuteAsync(sql, transfer);
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
