using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DroneMesh3D.Api.Tests.Integration;

public sealed class AreasEndpointTests : IClassFixture<AreasEndpointTests.AreasApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // A valid closed polygon ring (~200m x 200m square in Warsaw)
    private static readonly double[][][] ValidPolygonCoordinates =
    [
        [
            [21.0000, 52.0000],
            [21.0020, 52.0000],
            [21.0020, 52.0018],
            [21.0000, 52.0018],
            [21.0000, 52.0000]
        ]
    ];

    private readonly HttpClient _client;
    private readonly AreasApiFactory _factory;

    public AreasEndpointTests(AreasApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public void Dispose() => _client.Dispose();

    [Fact]
    public async Task Post_ValidPolygon_Returns201WithAreaResponse()
    {
        // Arrange
        var request = new CreateAreaRequest("Polygon", ValidPolygonCoordinates);

        // Act
        var response = await _client.PostAsJsonAsync("/api/areas", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.True(body.CreatedAt > DateTimeOffset.MinValue);
        Assert.Equal("Polygon", body.Geometry.Type);
        Assert.Single(body.Geometry.Coordinates);

        // Verify Location header
        Assert.Contains($"/api/areas/{body.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_InvalidGeoJson_Returns400BadRequest()
    {
        // Arrange — "Point" is not a valid polygon type
        var request = new CreateAreaRequest("Point", ValidPolygonCoordinates);

        // Act
        var response = await _client.PostAsJsonAsync("/api/areas", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid GeoJSON", body);
    }

    [Fact]
    public async Task Post_FailedGeometryValidation_Returns422UnprocessableEntity()
    {
        // Arrange — polygon with only 2 distinct vertices (degenerate line, not a valid polygon)
        double[][][] degenerateCoordinates =
        [
            [
                [21.0000, 52.0000],
                [21.0020, 52.0000],
                [21.0000, 52.0000]
            ]
        ];
        var request = new CreateAreaRequest("Polygon", degenerateCoordinates);

        // Act
        var response = await _client.PostAsJsonAsync("/api/areas", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body.Errors);
        Assert.Equal("Validation failed.", body.Message);
    }

    [Fact]
    public async Task Get_ExistingArea_Returns200WithAreaResponse()
    {
        // Arrange — create an area first
        var request = new CreateAreaRequest("Polygon", ValidPolygonCoordinates);
        var createResponse = await _client.PostAsJsonAsync("/api/areas", request, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(created);

        // Act
        var response = await _client.GetAsync($"/api/areas/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body.Id);
        Assert.Equal("Polygon", body.Geometry.Type);
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/areas/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    ///     Custom WebApplicationFactory that replaces the PostgreSQL/PostGIS database
    ///     with an in-memory SQLite database using NetTopologySuite for spatial support.
    /// </summary>
    public sealed class AreasApiFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove ALL DbContext-related registrations to prevent dual-provider conflict.
                // AddDbContext registers DbContextOptions, the context itself, and provider-specific services.
                var descriptorsToRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                        || d.ServiceType == typeof(AppDbContext)
                        || (d.ServiceType.IsGenericType
                            && d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))
                        || d.ServiceType == typeof(DbContextOptions))
                    .ToList();

                foreach (var d in descriptorsToRemove)
                    services.Remove(d);

                // Also remove all internal EF Core provider services (the Npgsql registrations)
                var efInternalDescriptors = services
                    .Where(d =>
                        d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                        || d.ImplementationType?.FullName?.Contains("Npgsql") == true)
                    .ToList();

                foreach (var d in efInternalDescriptors)
                    services.Remove(d);

                // Create and open a persistent SQLite connection for the test lifetime
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                // Register AppDbContext with SQLite + NTS (AppDbContext auto-detects provider)
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection, x => x.UseNetTopologySuite());
                });

                // Ensure the database schema is created
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // SpatiaLite registers the geometry column with SRID=0 and type=GEOMETRY by default.
                // Re-register it with SRID=4326 and type=POLYGON to match the app's NTS geometries.
                db.Database.ExecuteSqlRaw(
                    "SELECT DiscardGeometryColumn('Areas', 'Geometry')");
                db.Database.ExecuteSqlRaw(
                    "SELECT RecoverGeometryColumn('Areas', 'Geometry', 4326, 'POLYGON', 'XY')");
            });

            builder.UseEnvironment("Testing");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _connection?.Close();
                _connection?.Dispose();
            }
        }
    }
}
