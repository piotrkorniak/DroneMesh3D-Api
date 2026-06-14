using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace DroneMesh3D.Api.Tests.Integration;

/// <summary>
///     Base class for integration tests. Provides a shared HttpClient,
///     JSON options, helper methods, and per-test database cleanup.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    protected static readonly double[][][] ValidPolygonCoordinates =
    [
        [
            [17.0300, 51.1000],
            [17.0320, 51.1000],
            [17.0320, 51.1018],
            [17.0300, 51.1018],
            [17.0300, 51.1000]
        ]
    ];

    protected IntegrationTestBase(DroneMesh3DApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected DroneMesh3DApiFactory Factory { get; }
    protected HttpClient Client { get; }

    public async Task InitializeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.FlightPlans.RemoveRange(db.FlightPlans);
        db.Areas.RemoveRange(db.Areas);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Creates a valid area and returns its ID.
    /// </summary>
    protected async Task<Guid> CreateAreaAsync()
    {
        var request = new CreateAreaRequest(GeoJsonType.Polygon, ValidPolygonCoordinates);
        var response = await Client.PostAsJsonAsync("/api/areas", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        var area = await response.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(area);
        return area.Id;
    }

    /// <summary>
    ///     Creates a flight plan (POI mode, 12 waypoints) for the given area.
    /// </summary>
    protected async Task<FlightPlanResponse> CreateFlightPlanAsync(Guid areaId)
    {
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

        var response = await Client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        var flightPlan = await response.Content.ReadFromJsonAsync<FlightPlanResponse>(JsonOptions);
        Assert.NotNull(flightPlan);
        return flightPlan;
    }
}
