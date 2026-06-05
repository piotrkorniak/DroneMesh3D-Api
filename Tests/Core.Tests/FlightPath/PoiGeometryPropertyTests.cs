using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Tests.FlightPath.Arbitraries;
using FsCheck.Xunit;

namespace DroneMesh3D.Core.Tests.FlightPath;

/// <summary>
///     Feature: flight-path-calculation, Property 5: POI waypoints form equidistant closed orbit
///     Feature: flight-path-calculation, Property 6: POI gimbal yaw points toward center
///     **Validates: Requirements 3.2, 3.3, 3.4**
/// </summary>
public sealed class PoiGeometryPropertyTests
{
    private readonly PoiFlightPathStrategy _strategy = new();

    /// <summary>
    ///     Feature: flight-path-calculation, Property 5: POI waypoints form equidistant closed orbit
    ///     **Validates: Requirements 3.2, 3.4**
    ///     Property: For any valid POI parameters, all generated waypoints are at the same distance
    ///     from the center point (within floating-point tolerance).
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PoiModeParametersArbitrary)])]
    public bool PoiWaypoints_AllEquidistantFromCenter(PoiModeParameters parameters)
    {
        var result = _strategy.Calculate(parameters);

        if (result.Waypoints.Count < 2)
        {
            return true;
        }

        // Compute distance from center to each waypoint
        var distances = result.Waypoints.Select(wp =>
            GeodesicMathService.DistanceBetween(
                parameters.CenterLatitude, parameters.CenterLongitude,
                wp.Latitude, wp.Longitude)).ToList();

        // All distances should be equal (within tolerance proportional to the radius)
        // Use 0.5% of radius as tolerance to account for Haversine approximation
        var tolerance = parameters.RadiusM * 0.005;
        var referenceDistance = distances[0];

        return distances.All(d => Math.Abs(d - referenceDistance) < tolerance);
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 5: POI waypoints form equidistant closed orbit
    ///     **Validates: Requirements 3.2, 3.4**
    ///     Property: For any valid POI parameters, consecutive waypoints are equally spaced angularly
    ///     (360° / photoCount), and the angular distance from the last waypoint to the first equals
    ///     the spacing between any other consecutive pair.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PoiModeParametersArbitrary)])]
    public bool PoiWaypoints_EquallySpacedAngularly(PoiModeParameters parameters)
    {
        var result = _strategy.Calculate(parameters);

        if (result.Waypoints.Count < 2)
        {
            return true;
        }

        var waypointCount = result.Waypoints.Count;
        var expectedAngularStep = 360.0 / waypointCount;

        // Compute bearing from center to each waypoint
        var bearings = result.Waypoints.Select(wp =>
            GeodesicMathService.BearingBetween(
                parameters.CenterLatitude, parameters.CenterLongitude,
                wp.Latitude, wp.Longitude)).ToList();

        // Check angular differences between consecutive waypoints
        var angularDifferences = new List<double>();
        for (var i = 0; i < waypointCount; i++)
        {
            var nextIndex = (i + 1) % waypointCount;
            var diff = bearings[nextIndex] - bearings[i];

            // Normalize to [0, 360)
            while (diff < 0)
            {
                diff += 360.0;
            }

            while (diff >= 360.0)
            {
                diff -= 360.0;
            }

            angularDifferences.Add(diff);
        }

        // All angular differences should equal the expected step (within tolerance)
        // Use 0.1° tolerance for floating-point imprecision in geodesic calculations
        const double angleTolerance = 0.1;

        return angularDifferences.All(d =>
            Math.Abs(d - expectedAngularStep) < angleTolerance);
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 5: POI waypoints form equidistant closed orbit
    ///     **Validates: Requirements 3.2, 3.4**
    ///     Property: The generated waypoint count matches the expected photo count.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PoiModeParametersArbitrary)])]
    public bool PoiWaypoints_CountMatchesExpectedPhotoCount(PoiModeParameters parameters)
    {
        var result = _strategy.Calculate(parameters);

        if (parameters.PhotoCount.HasValue)
        {
            // Explicit photo count: must match (clamped to minimum of 3)
            var expectedCount = Math.Max(parameters.PhotoCount.Value, 3);
            return result.Waypoints.Count == expectedCount;
        }

        // Overlap-based: must have at least 3 waypoints
        return result.Waypoints.Count >= 3;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 6: POI gimbal yaw points toward center
    ///     **Validates: Requirements 3.3**
    ///     Property: For any waypoint generated in POI mode, the gimbal yaw value equals the
    ///     geodesic bearing from that waypoint's position to the center point (within ±0.01° tolerance).
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PoiModeParametersArbitrary)])]
    public bool PoiGimbalYaw_PointsTowardCenter(PoiModeParameters parameters)
    {
        var result = _strategy.Calculate(parameters);

        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        const double yawTolerance = 0.01;

        foreach (var waypoint in result.Waypoints)
        {
            // Expected yaw: bearing from waypoint to center
            var expectedYaw = GeodesicMathService.BearingBetween(
                waypoint.Latitude, waypoint.Longitude,
                parameters.CenterLatitude, parameters.CenterLongitude);

            var diff = Math.Abs(NormalizeAngleDifference(waypoint.GimbalYawDegrees, expectedYaw));

            if (diff > yawTolerance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Normalizes the difference between two angles to the range [-180, 180].
    /// </summary>
    private static double NormalizeAngleDifference(double angle1, double angle2)
    {
        var diff = angle1 - angle2;
        while (diff > 180.0)
        {
            diff -= 360.0;
        }

        while (diff < -180.0)
        {
            diff += 360.0;
        }

        return diff;
    }
}
