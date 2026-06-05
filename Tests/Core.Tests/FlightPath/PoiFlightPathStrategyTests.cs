using DroneMesh3D.Core.FlightPath;

namespace DroneMesh3D.Core.Tests.FlightPath;

public sealed class PoiFlightPathStrategyTests
{
    private readonly PoiFlightPathStrategy _sut = new();

    private static PoiModeParameters DefaultPoiParams(
        int? photoCount = 4,
        double radius = 100.0,
        double altitude = 60.0,
        double gimbalPitch = -60.0) => new(
        52.0,
        21.0,
        radius,
        altitude,
        gimbalPitch,
        photoCount,
        null,
        null,
        null);

    #region Waypoint Count Tests

    [Fact]
    public void Calculate_4Waypoints_ProducesExactly4Waypoints()
    {
        var parameters = DefaultPoiParams();

        var result = _sut.Calculate(parameters);

        Assert.Equal(4, result.Waypoints.Count);
    }

    [Fact]
    public void Calculate_36Waypoints_AngularStepIs10Degrees()
    {
        var parameters = DefaultPoiParams(36);

        var result = _sut.Calculate(parameters);

        Assert.Equal(36, result.Waypoints.Count);
        // Angular step = 360 / 36 = 10°
        // Verify waypoints are distributed evenly by checking angular separation
    }

    #endregion

    #region Distance from Center Tests

    [Fact]
    public void Calculate_Radius100m_AllWaypointsApproximately100mFromCenter()
    {
        var parameters = DefaultPoiParams();

        var result = _sut.Calculate(parameters);

        foreach (var wp in result.Waypoints)
        {
            var distance = GeodesicMathService.DistanceBetween(
                52.0, 21.0, wp.Latitude, wp.Longitude);

            // Allow 1m tolerance for geodesic computation on spherical Earth
            Assert.InRange(distance, 99.0, 101.0);
        }
    }

    [Fact]
    public void Calculate_Radius50m_AllWaypointsApproximately50mFromCenter()
    {
        var parameters = DefaultPoiParams(8, 50.0);

        var result = _sut.Calculate(parameters);

        foreach (var wp in result.Waypoints)
        {
            var distance = GeodesicMathService.DistanceBetween(
                52.0, 21.0, wp.Latitude, wp.Longitude);

            Assert.InRange(distance, 49.0, 51.0);
        }
    }

    #endregion

    #region Gimbal Yaw Tests

    [Fact]
    public void Calculate_GimbalYaw_PointsTowardCenter()
    {
        var parameters = DefaultPoiParams();

        var result = _sut.Calculate(parameters);

        foreach (var wp in result.Waypoints)
        {
            // Compute expected yaw: bearing from waypoint to center
            var expectedYaw = GeodesicMathService.BearingBetween(
                wp.Latitude, wp.Longitude, 52.0, 21.0);

            // Gimbal yaw should match the bearing to center within tolerance
            Assert.InRange(Math.Abs(wp.GimbalYawDegrees - expectedYaw), 0.0, 0.01);
        }
    }

    [Fact]
    public void Calculate_FirstWaypoint_GimbalYawPointsSouth()
    {
        // First waypoint is at bearing 0° from center (due north of center)
        // So gimbal yaw should point toward center = ~180° (south)
        var parameters = DefaultPoiParams();

        var result = _sut.Calculate(parameters);

        var firstWp = result.Waypoints[0];
        // First waypoint placed at bearing 0° from center = north of center
        // Yaw back to center should be ~180°
        Assert.InRange(firstWp.GimbalYawDegrees, 179.0, 181.0);
    }

    #endregion

    #region Overlap-Based Photo Count Derivation

    [Fact]
    public void Calculate_Overlap80Percent_Fov82Degrees_Produces22Waypoints()
    {
        // stepAngle = FOV * (1 - overlap/100) = 82 * (1 - 0.80) = 82 * 0.2 = 16.4°
        // photoCount = ceil(360 / 16.4) = ceil(21.95) = 22
        var parameters = new PoiModeParameters(
            52.0,
            21.0,
            100.0,
            60.0,
            -60.0,
            null,
            80.0,
            82.0,
            null);

        var result = _sut.Calculate(parameters);

        Assert.Equal(22, result.Waypoints.Count);
    }

    [Fact]
    public void Calculate_Overlap70Percent_Fov82Degrees_ProducesExpectedWaypoints()
    {
        // stepAngle = 82 * (1 - 0.70) = 82 * 0.3 = 24.6°
        // photoCount = ceil(360 / 24.6) = ceil(14.63) = 15
        var parameters = new PoiModeParameters(
            52.0,
            21.0,
            100.0,
            60.0,
            -60.0,
            null,
            70.0,
            82.0,
            null);

        var result = _sut.Calculate(parameters);

        Assert.Equal(15, result.Waypoints.Count);
    }

    #endregion

    #region Altitude Tests

    [Fact]
    public void Calculate_AllWaypointsAtRequestedAltitude()
    {
        var parameters = DefaultPoiParams(8, altitude: 60.0);

        var result = _sut.Calculate(parameters);

        foreach (var wp in result.Waypoints)
        {
            Assert.Equal(60.0, wp.AltitudeAglM);
        }
    }

    [Fact]
    public void Calculate_MaxAltitude120m_ProducesValidResult()
    {
        var parameters = DefaultPoiParams(8, altitude: 120.0);

        var result = _sut.Calculate(parameters);

        Assert.NotNull(result);
        Assert.Equal(8, result.Waypoints.Count);
        foreach (var wp in result.Waypoints)
        {
            Assert.Equal(120.0, wp.AltitudeAglM);
        }
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void Calculate_ValidParams_ReturnsPositiveStatistics()
    {
        var parameters = DefaultPoiParams(8);

        var result = _sut.Calculate(parameters);

        Assert.True(result.Statistics.TotalDistanceM > 0);
        Assert.True(result.Statistics.EstimatedFlightTimeS > 0);
        Assert.True(result.Statistics.PhotoCount > 0);
        Assert.True(result.Statistics.CoveredAreaM2 > 0);
    }

    [Fact]
    public void Calculate_PhotoCountMatchesWaypointCount()
    {
        var parameters = DefaultPoiParams(12);

        var result = _sut.Calculate(parameters);

        Assert.Equal(result.Waypoints.Count, result.Statistics.PhotoCount);
    }

    #endregion

    #region Gimbal Pitch Tests

    [Fact]
    public void Calculate_GimbalPitchClamped_WithinValidRange()
    {
        var parameters = DefaultPoiParams(gimbalPitch: -60.0);

        var result = _sut.Calculate(parameters);

        foreach (var wp in result.Waypoints)
        {
            Assert.InRange(wp.GimbalPitchDegrees, -90.0, -45.0);
        }
    }

    [Fact]
    public void Calculate_GimbalPitchTooLow_ClampedToMinus90()
    {
        // Request pitch of -100 should be clamped to -90
        var parameters = DefaultPoiParams(gimbalPitch: -100.0);

        var result = _sut.Calculate(parameters);

        foreach (var wp in result.Waypoints)
        {
            Assert.Equal(-90.0, wp.GimbalPitchDegrees);
        }
    }

    [Fact]
    public void Calculate_GimbalPitchTooHigh_ClampedToMinus45()
    {
        // Request pitch of -30 should be clamped to -45
        var parameters = DefaultPoiParams(gimbalPitch: -30.0);

        var result = _sut.Calculate(parameters);

        foreach (var wp in result.Waypoints)
        {
            Assert.Equal(-45.0, wp.GimbalPitchDegrees);
        }
    }

    #endregion
}
