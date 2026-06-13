using DroneMesh3D.Api.Handlers;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Interfaces;
using NSubstitute;

namespace DroneMesh3D.Api.Tests.Handlers;

public sealed class ListFlightPlansQueryHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly IFlightPlanRepository _repository = Substitute.For<IFlightPlanRepository>();
    private readonly ListFlightPlansQueryHandler _sut;

    public ListFlightPlansQueryHandlerTests()
    {
        _sut = new ListFlightPlansQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoFlightPlansExist()
    {
        // Arrange
        var query = new ListFlightPlansQuery(null, TestUserId);
        _repository.ListAsync(null, TestUserId, 100, 0, Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ReturnsMappedFlightPlans_WhenEntitiesExist()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var flightPlanId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var waypoints = new List<Waypoint>
        {
            new(52.0, 21.0, 50.0, -45.0, 0.0),
            new(52.001, 21.001, 50.0, -45.0, 90.0)
        };

        var entity = new FlightPlanEntity
        {
            Id = flightPlanId,
            AreaId = areaId,
            Mode = FlightMode.Grid,
            Waypoints = waypoints,
            TotalDistanceM = 150.5,
            EstimatedFlightTimeS = 60.0,
            PhotoCount = 10,
            CoveredAreaM2 = 5000.0,
            CreatedAt = createdAt
        };

        var query = new ListFlightPlansQuery(areaId, TestUserId, 50, 10);
        _repository.ListAsync(areaId, TestUserId, 50, 10, Arg.Any<CancellationToken>())
            .Returns([entity]);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        var response = result[0];
        Assert.Equal(flightPlanId, response.Id);
        Assert.Equal(areaId, response.AreaId);
        Assert.Equal("Grid", response.Mode);
        Assert.Equal(2, response.Waypoints.Count);
        Assert.Equal(150.5, response.Statistics.TotalDistanceM);
        Assert.Equal(60.0, response.Statistics.EstimatedFlightTimeS);
        Assert.Equal(10, response.Statistics.PhotoCount);
        Assert.Equal(5000.0, response.Statistics.CoveredAreaM2);
        Assert.Equal(createdAt, response.CreatedAt);
    }

    [Fact]
    public async Task Handle_PassesParametersToRepository()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var query = new ListFlightPlansQuery(areaId, TestUserId, 25, 5);
        _repository.ListAsync(areaId, TestUserId, 25, 5, Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        await _sut.Handle(query, CancellationToken.None);

        // Assert
        await _repository.Received(1).ListAsync(areaId, TestUserId, 25, 5, Arg.Any<CancellationToken>());
    }
}
