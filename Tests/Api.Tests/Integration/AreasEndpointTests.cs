using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace DroneMesh3D.Api.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class AreasEndpointTests : IClassFixture<AreasEndpointTests.AreasApiFactory>, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
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
        var request = new CreateAreaRequest(GeoJsonType.Polygon, ValidPolygonCoordinates);

        // Act
        var response = await _client.PostAsJsonAsync("/api/areas", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.Id);
        Assert.True(body.CreatedAt > DateTimeOffset.MinValue);
        Assert.Equal(GeoJsonType.Polygon, body.Geometry.Type);
        Assert.Single(body.Geometry.Coordinates);

        // Verify Location header
        Assert.Contains($"/api/areas/{body.Id}", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task Post_InvalidGeoJson_Returns400BadRequest()
    {
        // Arrange — send raw JSON with invalid type (can't construct via enum)
        var json = """{"type":"Point","coordinates":[[[21.0,52.0],[21.002,52.0],[21.002,52.0018],[21.0,52.0018],[21.0,52.0]]]}""";
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/areas", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
        var request = new CreateAreaRequest(GeoJsonType.Polygon, degenerateCoordinates);

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
        var request = new CreateAreaRequest(GeoJsonType.Polygon, ValidPolygonCoordinates);
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
        Assert.Equal(GeoJsonType.Polygon, body.Geometry.Type);
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
    ///     Custom WebApplicationFactory that uses the real PostgreSQL/PostGIS database
    ///     from the test connection string (CI service container or local Docker).
    /// </summary>
    public sealed class AreasApiFactory : WebApplicationFactory<Program>
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
