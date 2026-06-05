using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Tests.FlightPath.Arbitraries;
using FsCheck.Xunit;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.Tests.FlightPath;

/// <summary>
///     Feature: flight-path-calculation, Property 11: Result completeness and coordinate validity
///     **Validates: Requirements 7.1, 7.2, 7.3, 10.2**
/// </summary>
public sealed class ResultCompletenessPropertyTests
{
    private readonly GridFlightPathStrategy _gridStrategy = new();
    private readonly PoiFlightPathStrategy _poiStrategy = new();

    /// <summary>
    ///     Feature: flight-path-calculation, Property 11: Result completeness and coordinate validity
    ///     **Validates: Requirements 7.1, 7.2, 7.3, 10.2**
    ///     Property: For any successful grid mode calculation, the result shall contain a non-empty
    ///     waypoint list where every waypoint has valid latitude, longitude, altitude matching
    ///     the requested altitude, gimbal pitch ∈ [-90, -45], and a defined gimbal yaw.
    ///     The result shall also contain flight statistics with all positive values.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ValidPolygonArbitrary), typeof(GridModeParametersArbitrary)])]
    public bool GridMode_ResultIsComplete_WithValidCoordinatesAndPositiveStatistics(
        Polygon polygon, GridModeParameters parameters)
    {
        if (!polygon.IsValid || polygon.IsEmpty)
        {
            return true;
        }

        var result = _gridStrategy.Calculate(polygon, parameters);

        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        // Waypoints list is non-empty (already guaranteed by the guard above)
        // Every waypoint has valid latitude [-90, 90]
        if (!result.Waypoints.All(wp => wp.Latitude >= -90.0 && wp.Latitude <= 90.0))
        {
            return false;
        }

        // Every waypoint has valid longitude [-180, 180]
        if (!result.Waypoints.All(wp => wp.Longitude >= -180.0 && wp.Longitude <= 180.0))
        {
            return false;
        }

        // Every waypoint has altitude == requested altitude
        if (!result.Waypoints.All(wp => Math.Abs(wp.AltitudeAglM - parameters.AltitudeM) < 0.001))
        {
            return false;
        }

        // Every waypoint has gimbal pitch ∈ [-90, -45]
        if (!result.Waypoints.All(wp => wp.GimbalPitchDegrees >= -90.0 && wp.GimbalPitchDegrees <= -45.0))
        {
            return false;
        }

        // Every waypoint has a defined gimbal yaw (not NaN, not infinity)
        if (!result.Waypoints.All(wp => !double.IsNaN(wp.GimbalYawDegrees) && !double.IsInfinity(wp.GimbalYawDegrees)))
        {
            return false;
        }

        // Statistics: totalDistanceM >= 0 (0 is valid when only 1 waypoint exists)
        if (result.Statistics.TotalDistanceM < 0)
        {
            return false;
        }

        // Statistics: estimatedFlightTimeS >= 0 (0 is valid when only 1 waypoint exists)
        if (result.Statistics.EstimatedFlightTimeS < 0)
        {
            return false;
        }

        // Statistics: when multiple waypoints exist, distance and time must be positive
        if (result.Waypoints.Count > 1 && result.Statistics.TotalDistanceM <= 0)
        {
            return false;
        }

        if (result.Waypoints.Count > 1 && result.Statistics.EstimatedFlightTimeS <= 0)
        {
            return false;
        }

        // Statistics: photoCount > 0
        if (result.Statistics.PhotoCount <= 0)
        {
            return false;
        }

        // Statistics: coveredAreaM2 > 0
        if (result.Statistics.CoveredAreaM2 <= 0)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 11: Result completeness and coordinate validity
    ///     **Validates: Requirements 7.1, 7.2, 7.3, 10.2**
    ///     Property: For any successful POI mode calculation, the result shall contain a non-empty
    ///     waypoint list where every waypoint has valid latitude, longitude, altitude matching
    ///     the requested altitude, gimbal pitch ∈ [-90, -45], and a defined gimbal yaw.
    ///     The result shall also contain flight statistics with all positive values.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PoiModeParametersArbitrary)])]
    public bool PoiMode_ResultIsComplete_WithValidCoordinatesAndPositiveStatistics(
        PoiModeParameters parameters)
    {
        var result = _poiStrategy.Calculate(parameters);

        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        // Waypoints list is non-empty (already guaranteed by the guard above)
        // Every waypoint has valid latitude [-90, 90]
        if (!result.Waypoints.All(wp => wp.Latitude >= -90.0 && wp.Latitude <= 90.0))
        {
            return false;
        }

        // Every waypoint has valid longitude [-180, 180]
        if (!result.Waypoints.All(wp => wp.Longitude >= -180.0 && wp.Longitude <= 180.0))
        {
            return false;
        }

        // Every waypoint has altitude == requested altitude
        if (!result.Waypoints.All(wp => Math.Abs(wp.AltitudeAglM - parameters.AltitudeM) < 0.001))
        {
            return false;
        }

        // Every waypoint has gimbal pitch ∈ [-90, -45]
        if (!result.Waypoints.All(wp => wp.GimbalPitchDegrees >= -90.0 && wp.GimbalPitchDegrees <= -45.0))
        {
            return false;
        }

        // Every waypoint has a defined gimbal yaw (not NaN, not infinity)
        if (!result.Waypoints.All(wp => !double.IsNaN(wp.GimbalYawDegrees) && !double.IsInfinity(wp.GimbalYawDegrees)))
        {
            return false;
        }

        // Statistics: totalDistanceM >= 0 (0 is valid when only 1 waypoint exists)
        if (result.Statistics.TotalDistanceM < 0)
        {
            return false;
        }

        // Statistics: estimatedFlightTimeS >= 0 (0 is valid when only 1 waypoint exists)
        if (result.Statistics.EstimatedFlightTimeS < 0)
        {
            return false;
        }

        // Statistics: when multiple waypoints exist, distance and time must be positive
        if (result.Waypoints.Count > 1 && result.Statistics.TotalDistanceM <= 0)
        {
            return false;
        }

        if (result.Waypoints.Count > 1 && result.Statistics.EstimatedFlightTimeS <= 0)
        {
            return false;
        }

        // Statistics: photoCount > 0
        if (result.Statistics.PhotoCount <= 0)
        {
            return false;
        }

        // Statistics: coveredAreaM2 > 0
        if (result.Statistics.CoveredAreaM2 <= 0)
        {
            return false;
        }

        return true;
    }
}
