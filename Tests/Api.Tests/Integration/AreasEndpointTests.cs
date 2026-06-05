using System.Net;
using System.Net.Http.Json;
using System.Text;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.Models;

namespace DroneMesh3D.Api.Tests.Integration;

public sealed class AreasEndpointTests : IntegrationTestBase
{
    public AreasEndpointTests(DroneMesh3DApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Post_ValidPolygon_Returns201WithAreaResponse()
    {
        // Arrange
        var request = new CreateAreaRequest(GeoJsonType.Polygon, ValidPolygonCoordinates);

        // Act
        var response = await Client.PostAsJsonAsync("/api/areas", request, JsonOptions);

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
        var response = await Client.PostAsync("/api/areas", content);

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
        var response = await Client.PostAsJsonAsync("/api/areas", request, JsonOptions);

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
        // Arrange
        var areaId = await CreateAreaAsync();

        // Act
        var response = await Client.GetAsync($"/api/areas/{areaId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(areaId, body.Id);
        Assert.Equal(GeoJsonType.Polygon, body.Geometry.Type);
    }

    [Fact]
    public async Task Get_NonExistentId_Returns404NotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/areas/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListAreas_EmptyDatabase_Returns200WithEmptyArray()
    {
        // Database is cleaned before each test via IntegrationTestBase.InitializeAsync

        // Act
        var response = await Client.GetAsync("/api/areas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<AreaResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task ListAreas_MultipleAreas_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange — create multiple areas with different timestamps
        var request = new CreateAreaRequest(GeoJsonType.Polygon, ValidPolygonCoordinates);

        var response1 = await Client.PostAsJsonAsync("/api/areas", request, JsonOptions);
        response1.EnsureSuccessStatusCode();
        var area1 = await response1.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(area1);

        await Task.Delay(50);

        var response2 = await Client.PostAsJsonAsync("/api/areas", request, JsonOptions);
        response2.EnsureSuccessStatusCode();
        var area2 = await response2.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(area2);

        await Task.Delay(50);

        var response3 = await Client.PostAsJsonAsync("/api/areas", request, JsonOptions);
        response3.EnsureSuccessStatusCode();
        var area3 = await response3.Content.ReadFromJsonAsync<AreaResponse>(JsonOptions);
        Assert.NotNull(area3);

        // Act
        var listResponse = await Client.GetAsync("/api/areas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var areas = await listResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(JsonOptions);
        Assert.NotNull(areas);
        Assert.Equal(3, areas.Count);

        // Verify ordering: CreatedAt should be in descending order (newest first)
        for (var i = 0; i < areas.Count - 1; i++)
        {
            Assert.True(areas[i].CreatedAt >= areas[i + 1].CreatedAt,
                $"Areas not ordered by CreatedAt descending at index {i}: {areas[i].CreatedAt} should be >= {areas[i + 1].CreatedAt}");
        }

        // Verify the most recently created area is first
        Assert.Equal(area3.Id, areas[0].Id);
    }

    [Fact]
    public async Task ListAreas_ResponseShapeMatchesAreaResponseContract()
    {
        // Arrange — create an area
        await CreateAreaAsync();

        // Act
        var listResponse = await Client.GetAsync("/api/areas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var areas = await listResponse.Content.ReadFromJsonAsync<List<AreaResponse>>(JsonOptions);
        Assert.NotNull(areas);
        Assert.NotEmpty(areas);

        foreach (var area in areas)
        {
            Assert.NotEqual(Guid.Empty, area.Id);
            Assert.True(area.CreatedAt > DateTimeOffset.MinValue);
            Assert.NotNull(area.Geometry);
            Assert.Equal(GeoJsonType.Polygon, area.Geometry.Type);
            Assert.NotNull(area.Geometry.Coordinates);
            Assert.NotEmpty(area.Geometry.Coordinates);
        }
    }
}
