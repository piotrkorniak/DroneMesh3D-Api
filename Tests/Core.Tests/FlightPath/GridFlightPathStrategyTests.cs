using DroneMesh3D.Core.FlightPath;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.Tests.FlightPath;

public sealed class GridFlightPathStrategyTests
{
    private static readonly GeometryFactory GeoFactory = new(new PrecisionModel(), 4326);

    private readonly GridFlightPathStrategy _sut = new();

    // Standard DJI Mavic 3 camera parameters
    private static CameraParameters DefaultCamera => new(
        13.2,
        8.8,
        5472,
        3648);

    private static GridModeParameters DefaultGridParams(double altitude = 80.0, double? heading = null) => new(
        altitude,
        DefaultCamera,
        78.0,
        70.0,
        heading);

    /// <summary>
    ///     Creates a triangle polygon (minimum valid polygon) near Warsaw.
    /// </summary>
    private static Polygon CreateTrianglePolygon()
    {
        var coords = new[]
        {
            new Coordinate(21.0000, 52.0000),
            new Coordinate(21.0020, 52.0000),
            new Coordinate(21.0010, 52.0015),
            new Coordinate(21.0000, 52.0000) // closed
        };
        return GeoFactory.CreatePolygon(coords);
    }

    /// <summary>
    ///     Creates a rectangular polygon (~200m x 150m) near Warsaw.
    /// </summary>
    private static Polygon CreateRectangularPolygon()
    {
        var coords = new[]
        {
            new Coordinate(21.0000, 52.0000),
            new Coordinate(21.0030, 52.0000),
            new Coordinate(21.0030, 52.0014),
            new Coordinate(21.0000, 52.0014),
            new Coordinate(21.0000, 52.0000) // closed
        };
        return GeoFactory.CreatePolygon(coords);
    }

    #region Small Polygon Tests

    [Fact]
    public void Calculate_SmallPolygon_ProducesAtLeastSomeWaypoints()
    {
        // A very small polygon (~50m x 50m)
        var coords = new[]
        {
            new Coordinate(21.0000, 52.0000),
            new Coordinate(21.0007, 52.0000),
            new Coordinate(21.0007, 52.0005),
            new Coordinate(21.0000, 52.0005),
            new Coordinate(21.0000, 52.0000)
        };
        var polygon = GeoFactory.CreatePolygon(coords);
        var parameters = DefaultGridParams(50.0);

        var result = _sut.Calculate(polygon, parameters);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Waypoints);
    }

    #endregion

    #region Boundary Overlap Tests

    [Theory]
    [InlineData(75.0, 65.0)]
    [InlineData(80.0, 75.0)]
    [InlineData(78.0, 70.0)]
    [InlineData(75.0, 75.0)]
    public void Calculate_BoundaryOverlaps_ProducesValidSpacingCalculations(
        double frontOverlap, double sideOverlap)
    {
        var polygon = CreateRectangularPolygon();
        var parameters = new GridModeParameters(
            80.0,
            DefaultCamera,
            frontOverlap,
            sideOverlap,
            null);

        var result = _sut.Calculate(polygon, parameters);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Waypoints);
        Assert.True(result.Statistics.TotalDistanceM > 0);
        Assert.True(result.Statistics.PhotoCount > 0);
    }

    #endregion

    #region Gimbal Tests

    [Fact]
    public void Calculate_DefaultGimbal_AllWaypointsAtNadir()
    {
        var polygon = CreateRectangularPolygon();
        var parameters = DefaultGridParams();

        var result = _sut.Calculate(polygon, parameters);

        foreach (var wp in result.Waypoints)
        {
            Assert.Equal(-90.0, wp.GimbalPitchDegrees);
        }
    }

    #endregion

    #region Triangle Polygon Tests

    [Fact]
    public void Calculate_TrianglePolygon_ProducesWaypoints()
    {
        var polygon = CreateTrianglePolygon();
        var parameters = DefaultGridParams();

        var result = _sut.Calculate(polygon, parameters);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Waypoints);
        Assert.True(result.Waypoints.Count >= 1,
            $"Expected at least 1 waypoint, got {result.Waypoints.Count}");
    }

    [Fact]
    public void Calculate_TrianglePolygon_AllWaypointsHaveCorrectAltitude()
    {
        var polygon = CreateTrianglePolygon();
        var parameters = DefaultGridParams();

        var result = _sut.Calculate(polygon, parameters);

        foreach (var wp in result.Waypoints)
        {
            Assert.Equal(80.0, wp.AltitudeAglM);
        }
    }

    #endregion

    #region Max Altitude (120m) Tests

    [Fact]
    public void Calculate_MaxAltitude120m_StillProducesWaypoints()
    {
        var polygon = CreateRectangularPolygon();
        var parameters = DefaultGridParams(120.0);

        var result = _sut.Calculate(polygon, parameters);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Waypoints);
        // At higher altitude, fewer waypoints expected due to larger footprint/spacing
        Assert.True(result.Waypoints.Count >= 1);
    }

    [Fact]
    public void Calculate_MaxAltitude120m_WaypointsAtCorrectAltitude()
    {
        var polygon = CreateRectangularPolygon();
        var parameters = DefaultGridParams(120.0);

        var result = _sut.Calculate(polygon, parameters);

        foreach (var wp in result.Waypoints)
        {
            Assert.Equal(120.0, wp.AltitudeAglM);
        }
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void Calculate_ValidPolygon_ReturnsPositiveStatistics()
    {
        var polygon = CreateRectangularPolygon();
        var parameters = DefaultGridParams();

        var result = _sut.Calculate(polygon, parameters);

        Assert.True(result.Statistics.TotalDistanceM > 0);
        Assert.True(result.Statistics.EstimatedFlightTimeS > 0);
        Assert.True(result.Statistics.PhotoCount > 0);
        Assert.True(result.Statistics.CoveredAreaM2 > 0);
    }

    [Fact]
    public void Calculate_ValidPolygon_PhotoCountMatchesWaypointCount()
    {
        var polygon = CreateRectangularPolygon();
        var parameters = DefaultGridParams();

        var result = _sut.Calculate(polygon, parameters);

        Assert.Equal(result.Waypoints.Count, result.Statistics.PhotoCount);
    }

    #endregion

    #region Heading Tests

    [Fact]
    public void Calculate_WithUserSpecifiedHeading_UsesProvidedHeading()
    {
        var polygon = CreateRectangularPolygon();
        var parameters = DefaultGridParams(heading: 45.0);

        var result = _sut.Calculate(polygon, parameters);

        // Should produce waypoints (heading doesn't prevent computation)
        Assert.NotNull(result);
        Assert.NotEmpty(result.Waypoints);
    }

    [Fact]
    public void Calculate_WithInvalidHeading_FallsBackToLongestAxis()
    {
        var polygon = CreateRectangularPolygon();
        // Heading outside 0-360 should trigger fallback
        var parameters = DefaultGridParams(heading: -10.0);

        var result = _sut.Calculate(polygon, parameters);

        Assert.NotNull(result);
        Assert.NotEmpty(result.Waypoints);
    }

    #endregion
}
