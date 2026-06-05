using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneMesh3D.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class FlightPlansEndpointTests : IClassFixture<FlightPlansEndpointTests.FlightPlansApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    // A valid closed polygon ring (~200m x 200m square in Wroclaw)
    private static readonly double[][][] ValidPolygonCoordinates =
    [
        [
            [17.0300, 51.1000],
            [17.0320, 51.1000],
            [17.0320, 51.1018],
            [17.0300, 51.1018],
            [17.0300, 51.1000]
        ]
    ];

    private readonly HttpClient _client;
    private readonly FlightPlansApiFactory _factory;

    public FlightPlansEndpointTests(FlightPlansApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Post_ValidGridRequest_Returns201WithFlightPlanResponse()
    {
        // Arrange — create an area first
        var areaId = await CreateAreaAsync();

        var request = new CalculateFlightPathRequest(
            areaId,
            FlightMode.Grid,
            new GridModeParametersDto(
                80,
                new CameraParametersDto(
                    13.2,
                    8.8,
                    5472,
                    3648),
                78,
                70,
                null),
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FlightPlanResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(areaId, body.AreaId);
        Assert.Equal("Grid", body.Mode);
        Assert.NotEmpty(body.Waypoints);
        Assert.True(body.Statistics.TotalDistanceM > 0);
        Assert.True(body.Statistics.EstimatedFlightTimeS > 0);
        Assert.True(body.Statistics.PhotoCount > 0);
        Assert.True(body.Statistics.CoveredAreaM2 > 0);
        Assert.True(body.CreatedAt > DateTimeOffset.MinValue);

        // Verify Location header
        Assert.Contains($"/api/flight-plans/{body.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_ValidPoiRequest_Returns201WithFlightPlanResponse()
    {
        // Arrange — create an area first
        var areaId = await CreateAreaAsync();

        var request = new CalculateFlightPathRequest(
            areaId,
            FlightMode.Poi,
            null,
            new PoiModeParametersDto(
                51.1,
                17.04,
                100,
                60,
                -60,
                12,
                null,
                null,
                null));

        // Act
        var response = await _client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FlightPlanResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.Equal(areaId, body.AreaId);
        Assert.Equal("Poi", body.Mode);
        Assert.NotEmpty(body.Waypoints);
        Assert.Equal(12, body.Waypoints.Count);
        Assert.True(body.Statistics.TotalDistanceM > 0);
        Assert.True(body.Statistics.EstimatedFlightTimeS > 0);
        Assert.Equal(12, body.Statistics.PhotoCount);
        Assert.True(body.Statistics.CoveredAreaM2 > 0);
        Assert.True(body.CreatedAt > DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task Post_InvalidParameters_AltitudeAbove120_Returns400()
    {
        // Arrange — create an area first
        var areaId = await CreateAreaAsync();

        var request = new CalculateFlightPathRequest(
            areaId,
            FlightMode.Grid,
            new GridModeParametersDto(
                150, // exceeds 120m limit
                new CameraParametersDto(
                    13.2,
                    8.8,
                    5472,
                    3648),
                78,
                70,
                null),
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

        // Assert
        // The ValidationBehavior throws a FluentValidation.ValidationException which is caught
        // by the GlobalExceptionHandler middleware and returned as HTTP 400 BadRequest.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body.Errors);
        Assert.Equal("Validation failed.", body.Message);
    }

    [Fact]
    public async Task Post_NonExistentArea_Returns404()
    {
        // Arrange — use a random non-existent area ID
        var nonExistentAreaId = Guid.NewGuid();

        var request = new CalculateFlightPathRequest(
            nonExistentAreaId,
            FlightMode.Grid,
            new GridModeParametersDto(
                80,
                new CameraParametersDto(
                    13.2,
                    8.8,
                    5472,
                    3648),
                78,
                70,
                null),
            null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingFlightPlan_Returns200WithFlightPlanResponse()
    {
        // Arrange — create an area and a flight plan
        var areaId = await CreateAreaAsync();

        var createRequest = new CalculateFlightPathRequest(
            areaId,
            FlightMode.Poi,
            null,
            new PoiModeParametersDto(
                51.1,
                17.04,
                100,
                60,
                -60,
                12,
                null,
                null,
                null));

        var createResponse = await _client.PostAsJsonAsync("/api/flight-plans", createRequest, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<FlightPlanResponse>(JsonOptions);
        Assert.NotNull(created);

        // Act
        var response = await _client.GetAsync($"/api/flight-plans/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<FlightPlanResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal(created.AreaId, body.AreaId);
        Assert.Equal("Poi", body.Mode);
        Assert.NotEmpty(body.Waypoints);
        Assert.Equal(12, body.Waypoints.Count);
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/flight-plans/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    ///     Helper method that creates an area and returns its ID.
    /// </summary>
    private async Task<Guid> CreateAreaAsync()
    {
        var areaRequest = new CreateAreaRequest(GeoJsonType.Polygon, ValidPolygonCoordinates);
        var areaResponse = await _client.PostAsJsonAsync("/api/areas", areaRequest, JsonOptions);
        areaResponse.EnsureSuccessStatusCode();
        var area = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(area);
        return area.Id;
    }

    /// <summary>
    ///     Custom WebApplicationFactory that uses the real PostgreSQL/PostGIS database
    ///     from the test connection string (CI service container or local Docker).
    /// </summary>
    public sealed class FlightPlansApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registration
                var descriptorsToRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                        || d.ServiceType == typeof(AppDbContext)
                        || (d.ServiceType.IsGenericType
                            && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                        || d.ServiceType == typeof(DbContextOptions))
                    .ToList();

                foreach (var d in descriptorsToRemove)
                {
                    services.Remove(d);
                }

                // Remove Npgsql provider services
                var efInternalDescriptors = services
                    .Where(d =>
                        d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                        || d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var d in efInternalDescriptors)
                {
                    services.Remove(d);
                }

                // Use PostgreSQL with a test-specific database name to avoid conflicts
                var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                                       ?? "Host=localhost;Database=dronemesh3d_test;Username=postgres;Password=YourStr0ngP@ssword";

                services.AddDbContext<AppDbContext>(options => { options.UseNpgsql(connectionString, x => x.UseNetTopologySuite()); });

                // Ensure schema is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            });

            builder.UseEnvironment("Testing");
        }
    }
}
