using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.Handlers;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.Interfaces;
using DroneMesh3D.Core.Models;
using NSubstitute;

namespace DroneMesh3D.Api.Tests.Handlers;

public sealed class CreateAreaCommandHandlerTests
{
    // A valid closed polygon ring
    private static readonly double[][] ValidRing =
    [
        [21.0000, 52.0000],
        [21.0020, 52.0000],
        [21.0020, 52.0018],
        [21.0000, 52.0018],
        [21.0000, 52.0000]
    ];

    private readonly IAreaRepository _repository = Substitute.For<IAreaRepository>();
    private readonly CreateAreaCommandHandler _sut;
    private readonly IAreaValidator _validator = Substitute.For<IAreaValidator>();

    public CreateAreaCommandHandlerTests()
    {
        _sut = new CreateAreaCommandHandler(_validator, _repository);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsAreaResponse()
    {
        // Arrange
        var command = new CreateAreaCommand(GeoJsonType.Polygon, [ValidRing]);

        _validator.Validate(Arg.Any<double[][]>())
            .Returns(new ValidationResult(true, []));

        _repository.AddAsync(Arg.Any<AreaEntity>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsT0); // AreaResponse
        var areaResponse = result.AsT0;
        Assert.NotEqual(Guid.Empty, areaResponse.Id);
        Assert.Equal(GeoJsonType.Polygon, areaResponse.Geometry.Type);
        Assert.Equal(command.Coordinates, areaResponse.Geometry.Coordinates);
        await _repository.Received(1).AddAsync(Arg.Any<AreaEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultiRingPolygon_ReturnsErrorResponse()
    {
        // Arrange
        double[][] secondRing =
        [
            [21.0005, 52.0005],
            [21.0015, 52.0005],
            [21.0015, 52.0013],
            [21.0005, 52.0013],
            [21.0005, 52.0005]
        ];
        var command = new CreateAreaCommand(GeoJsonType.Polygon, [ValidRing, secondRing]);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsT2); // ErrorResponse
        var errorResponse = result.AsT2;
        Assert.Contains("Multi-ring", errorResponse.Message);
    }

    [Fact]
    public async Task Handle_FailedGeometryValidation_ReturnsValidationErrorResponse()
    {
        // Arrange
        var command = new CreateAreaCommand(GeoJsonType.Polygon, [ValidRing]);
        var errors = new List<string>
        {
            "Polygon must not self-intersect.",
            "Polygon area exceeds maximum of 5 hectares."
        };

        _validator.Validate(Arg.Any<double[][]>())
            .Returns(new ValidationResult(false, errors));

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsT1); // ValidationErrorResponse
        var validationResponse = result.AsT1;
        Assert.Equal(2, validationResponse.Errors.Count);
        Assert.Contains("self-intersect", validationResponse.Errors[0]);
        Assert.Contains("maximum", validationResponse.Errors[1]);
        await _repository.DidNotReceive().AddAsync(Arg.Any<AreaEntity>(), Arg.Any<CancellationToken>());
    }
}
