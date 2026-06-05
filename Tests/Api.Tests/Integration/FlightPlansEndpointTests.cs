using System.Net;
using System.Net.Http.Json;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.FlightPath;

namespace DroneMesh3D.Api.Tests.Integration;

public sealed class FlightPlansEndpointTests : IntegrationTestBase
{
    public FlightPlansEndpointTests(DroneMesh3DApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Post_ValidGridRequest_Returns201WithFlightPlanResponse()
    {
        // Arrange
        var areaId = await CreateAreaAsync();

        var request = new CalculateFlightPathRequest(
            areaId,
            FlightMode.Grid,
            new GridModeParametersDto(
                80,
                new CameraParametersDto(13.2, 8.8, 5472, 3648),
                78,
                70,
                null),
            null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

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
        // Arrange
        var areaId = await CreateAreaAsync();

        var request = new CalculateFlightPathRequest(
            areaId,
            FlightMode.Poi,
            null,
            new PoiModeParametersDto(51.1, 17.04, 100, 60, -60, 12, null, null, null));

        // Act
        var response = await Client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

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
        // Arrange
        var areaId = await CreateAreaAsync();

        var request = new CalculateFlightPathRequest(
            areaId,
            FlightMode.Grid,
            new GridModeParametersDto(
                150, // exceeds 120m limit
                new CameraParametersDto(13.2, 8.8, 5472, 3648),
                78,
                70,
                null),
            null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body.Errors);
        Assert.Equal("Validation failed.", body.Message);
    }

    [Fact]
    public async Task Post_NonExistentArea_Returns404()
    {
        // Arrange
        var nonExistentAreaId = Guid.NewGuid();

        var request = new CalculateFlightPathRequest(
            nonExistentAreaId,
            FlightMode.Grid,
            new GridModeParametersDto(
                80,
                new CameraParametersDto(13.2, 8.8, 5472, 3648),
                78,
                70,
                null),
            null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/flight-plans", request, JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_ExistingFlightPlan_Returns200WithFlightPlanResponse()
    {
        // Arrange
        var areaId = await CreateAreaAsync();
        var created = await CreateFlightPlanAsync(areaId);

        // Act
        var response = await Client.GetAsync($"/api/flight-plans/{created.Id}");

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
        var response = await Client.GetAsync($"/api/flight-plans/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #region GET /api/flight-plans (list endpoint)

    [Fact]
    public async Task List_NoFlightPlansExist_Returns200WithEmptyArray()
    {
        // Arrange — use a non-existent area ID so no plans match
        var emptyAreaId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/flight-plans?areaId={emptyAreaId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<FlightPlanResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task List_FilterByAreaId_ReturnsOnlyMatchingFlightPlans()
    {
        // Arrange
        var areaId1 = await CreateAreaAsync();
        var areaId2 = await CreateAreaAsync();

        await CreateFlightPlanAsync(areaId1);
        await CreateFlightPlanAsync(areaId2);

        // Act
        var response = await Client.GetAsync($"/api/flight-plans?areaId={areaId1}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<FlightPlanResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body);
        Assert.All(body, fp => Assert.Equal(areaId1, fp.AreaId));
    }

    [Fact]
    public async Task List_InvalidGuidAreaId_Returns422WithValidationErrorResponse()
    {
        // Act
        var response = await Client.GetAsync("/api/flight-plans?areaId=not-a-guid");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotEmpty(body.Errors);
        Assert.Contains(body.Errors, e => e.Contains("GUID", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task List_OrderedByCreatedAtDescending()
    {
        // Arrange
        var areaId = await CreateAreaAsync();

        await CreateFlightPlanAsync(areaId);
        await Task.Delay(50);
        await CreateFlightPlanAsync(areaId);
        await Task.Delay(50);
        await CreateFlightPlanAsync(areaId);

        // Act
        var response = await Client.GetAsync($"/api/flight-plans?areaId={areaId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<List<FlightPlanResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(3, body.Count);

        for (var i = 0; i < body.Count - 1; i++)
        {
            Assert.True(body[i].CreatedAt >= body[i + 1].CreatedAt,
                $"Expected descending order: item[{i}].CreatedAt ({body[i].CreatedAt}) should be >= item[{i + 1}].CreatedAt ({body[i + 1].CreatedAt})");
        }
    }

    [Fact]
    public async Task List_PaginationWithLimitAndOffset_ReturnsCorrectSubset()
    {
        // Arrange
        var areaId = await CreateAreaAsync();

        for (var i = 0; i < 4; i++)
        {
            await CreateFlightPlanAsync(areaId);
            await Task.Delay(50);
        }

        // Get all first
        var allResponse = await Client.GetAsync($"/api/flight-plans?areaId={areaId}");
        allResponse.EnsureSuccessStatusCode();
        var allPlans = await allResponse.Content.ReadFromJsonAsync<List<FlightPlanResponse>>(JsonOptions);
        Assert.NotNull(allPlans);
        Assert.Equal(4, allPlans.Count);

        // Act — paginate with limit=2, offset=1
        var paginatedResponse = await Client.GetAsync($"/api/flight-plans?areaId={areaId}&limit=2&offset=1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, paginatedResponse.StatusCode);

        var paginatedBody = await paginatedResponse.Content.ReadFromJsonAsync<List<FlightPlanResponse>>(JsonOptions);
        Assert.NotNull(paginatedBody);
        Assert.Equal(2, paginatedBody.Count);
        Assert.Equal(allPlans[1].Id, paginatedBody[0].Id);
        Assert.Equal(allPlans[2].Id, paginatedBody[1].Id);
    }

    #endregion
}
