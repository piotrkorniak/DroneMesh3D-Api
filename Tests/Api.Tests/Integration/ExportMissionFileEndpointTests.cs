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
public sealed class ExportMissionFileEndpointTests
    : IClassFixture<ExportMissionFileEndpointTests.ExportApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

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
    private readonly ExportApiFactory _factory;

    public ExportMissionFileEndpointTests(ExportApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Export_CsvFormat_Returns200WithCorrectHeaders()
    {
        // Arrange
        var flightPlanId = await CreateFlightPlanAsync();

        // Act
        var response = await _client.GetAsync($"/api/flight-plans/{flightPlanId}/export?format=LitchiCsv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(".csv", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
    }

    [Fact]
    public async Task Export_KmlFormat_Returns200WithCorrectHeaders()
    {
        // Arrange
        var flightPlanId = await CreateFlightPlanAsync();

        // Act
        var response = await _client.GetAsync($"/api/flight-plans/{flightPlanId}/export?format=Kml");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.google-earth.kml+xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(".kml", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
    }

    [Fact]
    public async Task Export_DjiWpmlFormat_Returns200WithCorrectHeaders()
    {
        // Arrange
        var flightPlanId = await CreateFlightPlanAsync();

        // Act
        var response = await _client.GetAsync($"/api/flight-plans/{flightPlanId}/export?format=DjiWpml");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.google-earth.kmz", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(".kmz", response.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition?.DispositionType);
    }

    [Fact]
    public async Task Export_NonExistentFlightPlan_Returns404()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/flight-plans/{nonExistentId}/export?format=LitchiCsv");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_MissingFormatParameter_Returns422()
    {
        // Arrange
        var flightPlanId = await CreateFlightPlanAsync();

        // Act
        var response = await _client.GetAsync($"/api/flight-plans/{flightPlanId}/export");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }


    /// <summary>
    ///     Helper that creates an area and a flight plan, returning the flight plan ID.
    /// </summary>
    private async Task<Guid> CreateFlightPlanAsync()
    {
        // Create area
        var areaRequest = new CreateAreaRequest(GeoJsonType.Polygon, ValidPolygonCoordinates);
        var areaResponse = await _client.PostAsJsonAsync("/api/areas", areaRequest, JsonOptions);
        areaResponse.EnsureSuccessStatusCode();
        var area = await areaResponse.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(area);

        // Create flight plan using POI mode (produces a fixed number of waypoints)
        var flightPlanRequest = new CalculateFlightPathRequest(
            area.Id,
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

        var flightPlanResponse = await _client.PostAsJsonAsync("/api/flight-plans", flightPlanRequest, JsonOptions);
        flightPlanResponse.EnsureSuccessStatusCode();
        var flightPlan = await flightPlanResponse.Content.ReadFromJsonAsync<FlightPlanResponse>(JsonOptions);
        Assert.NotNull(flightPlan);

        return flightPlan.Id;
    }

    /// <summary>
    ///     Custom WebApplicationFactory that uses the real PostgreSQL/PostGIS database.
    /// </summary>
    public sealed class ExportApiFactory : WebApplicationFactory<Program>
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
