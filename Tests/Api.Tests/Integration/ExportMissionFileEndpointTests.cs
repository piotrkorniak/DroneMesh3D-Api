using System.Net;

namespace DroneMesh3D.Api.Tests.Integration;

public sealed class ExportMissionFileEndpointTests : IntegrationTestBase
{
    public ExportMissionFileEndpointTests(DroneMesh3DApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Export_CsvFormat_Returns200WithCorrectHeaders()
    {
        // Arrange
        var flightPlanId = await CreateFlightPlanForExportAsync();

        // Act
        var response = await Client.GetAsync($"/api/flight-plans/{flightPlanId}/export?format=LitchiCsv");

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
        var flightPlanId = await CreateFlightPlanForExportAsync();

        // Act
        var response = await Client.GetAsync($"/api/flight-plans/{flightPlanId}/export?format=Kml");

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
        var flightPlanId = await CreateFlightPlanForExportAsync();

        // Act
        var response = await Client.GetAsync($"/api/flight-plans/{flightPlanId}/export?format=DjiWpml");

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
        var response = await Client.GetAsync($"/api/flight-plans/{nonExistentId}/export?format=LitchiCsv");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_MissingFormatParameter_Returns422()
    {
        // Arrange
        var flightPlanId = await CreateFlightPlanForExportAsync();

        // Act
        var response = await Client.GetAsync($"/api/flight-plans/{flightPlanId}/export");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>
    ///     Creates an area + flight plan and returns the flight plan ID.
    /// </summary>
    private async Task<Guid> CreateFlightPlanForExportAsync()
    {
        var areaId = await CreateAreaAsync();
        var flightPlan = await CreateFlightPlanAsync(areaId);
        return flightPlan.Id;
    }
}
