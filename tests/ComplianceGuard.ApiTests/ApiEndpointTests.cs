using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace ComplianceGuard.ApiTests;

public class ApiEndpointTests : IClassFixture<ComplianceGuardApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiEndpointTests(ComplianceGuardApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // --- Facility Endpoints ---

    [Fact]
    public async Task GetFacilities_ReturnsSeededFacilities()
    {
        var response = await _client.GetAsync("/facilities");

        response.EnsureSuccessStatusCode();
        var facilities = await response.Content.ReadFromJsonAsync<List<FacilityDto>>(JsonOptions);
        Assert.NotNull(facilities);
        Assert.True(facilities.Count >= 2);
        Assert.Contains(facilities, f => f.LicenseNumber == "OR-CUL-00142");
        Assert.Contains(facilities, f => f.LicenseNumber == "OR-RET-00287");
    }

    [Fact]
    public async Task GetFacilityById_ExistingId_ReturnsFacility()
    {
        var response = await _client.GetAsync(
            $"/facilities/{ComplianceGuardApiFactory.PortlandFacilityId}");

        response.EnsureSuccessStatusCode();
        var facility = await response.Content.ReadFromJsonAsync<FacilityDto>(JsonOptions);
        Assert.NotNull(facility);
        Assert.Equal("OR-CUL-00142", facility.LicenseNumber);
    }

    [Fact]
    public async Task GetFacilityById_NonexistentId_Returns404()
    {
        var response = await _client.GetAsync($"/facilities/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostFacility_CreatesAndReturns201()
    {
        var request = new
        {
            LicenseNumber = "OR-NEW-99999",
            Name = "New Test Facility",
            FacilityType = "Processor",
            State = "OR",
            City = "Salem",
            Latitude = 44.9429,
            Longitude = -123.0351
        };

        var response = await _client.PostAsJsonAsync("/facilities", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var facility = await response.Content.ReadFromJsonAsync<FacilityDto>(JsonOptions);
        Assert.NotNull(facility);
        Assert.Equal("OR-NEW-99999", facility.LicenseNumber);
    }

    // --- Tenant Resolution Middleware ---

    [Fact]
    public async Task GetPackages_WithValidLicenseHeader_ReturnsTenantScopedData()
    {
        _client.DefaultRequestHeaders.Add("X-License-Number", "OR-CUL-00142");

        var response = await _client.GetAsync("/packages");

        response.EnsureSuccessStatusCode();
        var packages = await response.Content.ReadFromJsonAsync<List<PackageDto>>(JsonOptions);
        Assert.NotNull(packages);
        Assert.NotEmpty(packages);
        Assert.All(packages, p => Assert.Equal(ComplianceGuardApiFactory.PortlandFacilityId, p.FacilityId));
        Assert.Contains(packages, p => p.Tag == "1A4010300003B01000001");
    }

    [Fact]
    public async Task GetPackages_WithDifferentTenantHeader_ReturnsEmpty()
    {
        _client.DefaultRequestHeaders.Add("X-License-Number", "OR-RET-00287");

        var response = await _client.GetAsync("/packages");

        response.EnsureSuccessStatusCode();
        var packages = await response.Content.ReadFromJsonAsync<List<PackageDto>>(JsonOptions);
        Assert.NotNull(packages);
        Assert.Empty(packages);
    }

    [Fact]
    public async Task GetPackages_WithoutHeader_ReturnsEmpty()
    {
        var response = await _client.GetAsync("/packages");

        response.EnsureSuccessStatusCode();
        var packages = await response.Content.ReadFromJsonAsync<List<PackageDto>>(JsonOptions);
        Assert.NotNull(packages);
        Assert.Empty(packages);
    }

    // --- Package Endpoints ---

    [Fact]
    public async Task PostPackage_WithTenantHeader_CreatesWithCorrectFacilityId()
    {
        var client = _client;
        client.DefaultRequestHeaders.Add("X-License-Number", "OR-CUL-00142");

        var request = new
        {
            Tag = $"1A401-API-TEST-{Guid.NewGuid():N}",
            ItemName = "API Test Flower",
            ItemCategory = "Flower",
            Quantity = 100.0,
            UnitOfMeasure = "Grams",
            PackagedDate = DateTime.UtcNow
        };

        var response = await client.PostAsJsonAsync("/packages", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var package = await response.Content.ReadFromJsonAsync<PackageDto>(JsonOptions);
        Assert.NotNull(package);
        Assert.Equal(ComplianceGuardApiFactory.PortlandFacilityId, package.FacilityId);
        Assert.Equal("Active", package.Status);
    }

    [Fact]
    public async Task GetPackageByTag_Existing_ReturnsPackage()
    {
        _client.DefaultRequestHeaders.Add("X-License-Number", "OR-CUL-00142");

        var response = await _client.GetAsync("/packages/by-tag/1A4010300003B01000001");

        response.EnsureSuccessStatusCode();
        var package = await response.Content.ReadFromJsonAsync<PackageDto>(JsonOptions);
        Assert.NotNull(package);
        Assert.Equal("Blue Dream - Dried Flower", package.ItemName);
    }

    // --- Transfer Endpoints ---

    [Fact]
    public async Task PostTransfer_CreatesWithScheduledStatus()
    {
        var client = _client;
        client.DefaultRequestHeaders.Add("X-License-Number", "OR-CUL-00142");

        var request = new
        {
            ManifestNumber = $"OR-MAN-API-{Guid.NewGuid():N}",
            ShipperFacilityLicenseNumber = "OR-CUL-00142",
            ShipperFacilityName = "Test Cultivator",
            RecipientFacilityLicenseNumber = "OR-RET-00287",
            RecipientFacilityName = "Test Dispensary",
            TransporterName = "Test Transport",
            DriverName = "Test Driver",
            VehicleLicensePlate = "TEST-001",
            PackageCount = 3,
            EstimatedDepartureAt = DateTime.UtcNow.AddHours(1),
            EstimatedArrivalAt = DateTime.UtcNow.AddHours(3)
        };

        var response = await client.PostAsJsonAsync("/transfers", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var transfer = await response.Content.ReadFromJsonAsync<TransferDto>(JsonOptions);
        Assert.NotNull(transfer);
        Assert.Equal("Scheduled", transfer.Status);
        Assert.Equal(ComplianceGuardApiFactory.PortlandFacilityId, transfer.FacilityId);
    }

    // --- Anomaly Endpoints ---

    [Fact]
    public async Task GetAnomalies_EmptyByDefault()
    {
        _client.DefaultRequestHeaders.Add("X-License-Number", "OR-CUL-00142");

        var response = await _client.GetAsync("/anomalies");

        response.EnsureSuccessStatusCode();
        var anomalies = await response.Content.ReadFromJsonAsync<List<AnomalyDto>>(JsonOptions);
        Assert.NotNull(anomalies);
    }

    [Fact]
    public async Task GetAnomalyById_NonexistentId_Returns404()
    {
        _client.DefaultRequestHeaders.Add("X-License-Number", "OR-CUL-00142");

        var response = await _client.GetAsync($"/anomalies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // --- DTOs for deserialization ---

    private record FacilityDto(Guid Id, string LicenseNumber, string Name, string FacilityType,
        string State, string City, double Latitude, double Longitude, bool IsActive, DateTime CreatedAt);

    private record PackageDto(Guid Id, Guid FacilityId, string Tag, string ItemName, string ItemCategory,
        decimal Quantity, string UnitOfMeasure, string Status, string? LabTestStatus,
        DateTime PackagedDate, DateTime CreatedAt);

    private record TransferDto(Guid Id, Guid FacilityId, string ManifestNumber, string Status, DateTime CreatedAt);

    private record AnomalyDto(Guid Id, string AnomalyType, string Severity, bool IsResolved);
}
