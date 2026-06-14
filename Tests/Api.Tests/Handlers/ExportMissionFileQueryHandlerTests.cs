using DroneMesh3D.Api.Handlers;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Api.Services;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Interfaces;
using DroneMesh3D.Core.MissionExport;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DroneMesh3D.Api.Tests.Handlers;

public sealed class ExportMissionFileQueryHandlerTests
{
    private static readonly Guid TestUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly IMissionFileGeneratorFactory _factory = Substitute.For<IMissionFileGeneratorFactory>();
    private readonly ILogger<ExportMissionFileQueryHandler> _logger = Substitute.For<ILogger<ExportMissionFileQueryHandler>>();
    private readonly IFlightPlanRepository _repository = Substitute.For<IFlightPlanRepository>();
    private readonly ICurrentUserAccessor _currentUser = Substitute.For<ICurrentUserAccessor>();
    private readonly ExportMissionFileQueryHandler _sut;

    public ExportMissionFileQueryHandlerTests()
    {
        _currentUser.UserId.Returns(TestUserId);
        _sut = new ExportMissionFileQueryHandler(_repository, _factory, _currentUser, _logger);
    }

    [Fact]
    public async Task Handle_FlightPlanNotFound_ReturnsErrorResponse()
    {
        var flightPlanId = Guid.NewGuid();
        var query = new ExportMissionFileQuery(flightPlanId, ExportFormat.LitchiCsv);

        _repository.GetByIdAsync(flightPlanId, TestUserId, Arg.Any<CancellationToken>())
            .Returns((FlightPlanEntity?)null);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.True(result.IsT2);
        Assert.Contains("not found", result.AsT2.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_EmptyWaypointsList_ReturnsValidationErrorResponse()
    {
        var flightPlanId = Guid.NewGuid();
        var query = new ExportMissionFileQuery(flightPlanId, ExportFormat.DjiWpml);
        var entity = new FlightPlanEntity { Id = flightPlanId, Waypoints = [] };

        _repository.GetByIdAsync(flightPlanId, TestUserId, Arg.Any<CancellationToken>()).Returns(entity);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.True(result.IsT1);
        Assert.Contains("no waypoints", result.AsT1.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ValidRequest_DelegatesToCorrectGeneratorAndReturnsMissionFileResult()
    {
        var flightPlanId = Guid.NewGuid();
        var query = new ExportMissionFileQuery(flightPlanId, ExportFormat.LitchiCsv);

        var waypoints = new List<Waypoint> { new(52.0, 21.0, 50.0, -45.0, 90.0), new(52.001, 21.001, 55.0, -30.0, 180.0) };
        var entity = new FlightPlanEntity { Id = flightPlanId, Waypoints = waypoints };
        var expectedFileData = new MissionFileData([0x01, 0x02, 0x03], "text/csv", $"mission_{flightPlanId}.csv");

        var generator = Substitute.For<IMissionFileGenerator>();
        generator.Generate(flightPlanId, Arg.Any<IReadOnlyList<Waypoint>>()).Returns(expectedFileData);

        _repository.GetByIdAsync(flightPlanId, TestUserId, Arg.Any<CancellationToken>()).Returns(entity);
        _factory.GetGenerator(ExportFormat.LitchiCsv).Returns(generator);

        var result = await _sut.Handle(query, CancellationToken.None);

        Assert.True(result.IsT0);
        Assert.Equal(expectedFileData.Content, result.AsT0.Content);
        _factory.Received(1).GetGenerator(ExportFormat.LitchiCsv);
    }
}
