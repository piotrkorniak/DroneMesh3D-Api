using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Tests.FlightPath.Arbitraries;
using FsCheck.Xunit;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.Tests.FlightPath;

/// <summary>
///     Feature: flight-path-calculation, Property 3: Grid heading defaults to longest polygon axis
///     Feature: flight-path-calculation, Property 4: All grid waypoints lie within polygon boundary
///     **Validates: Requirements 2.6, 2.7**
/// </summary>
public sealed class GridGeometryPropertyTests
{
    private readonly GridFlightPathStrategy _strategy = new();

    /// <summary>
    ///     Feature: flight-path-calculation, Property 3: Grid heading defaults to longest polygon axis
    ///     **Validates: Requirements 2.6**
    ///     Property: For any valid polygon, when no heading is specified (null),
    ///     the computed scan line heading aligns with the longest axis of the polygon's OMBR.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ValidPolygonArbitrary), typeof(GridModeParametersArbitrary)])]
    public bool GridHeading_WithNoHeading_AlignsTolongestPolygonAxis(
        Polygon polygon, GridModeParameters parameters)
    {
        // Override parameters to have no heading
        var paramsNoHeading = parameters with { HeadingDegrees = null };

        if (!polygon.IsValid || polygon.IsEmpty)
        {
            return true; // Skip invalid polygons
        }

        var result = _strategy.Calculate(polygon, paramsNoHeading);

        // The expected heading is the longest axis heading of the polygon
        var expectedHeading = GeodesicMathService.LongestAxisHeading(polygon);

        // If no waypoints were generated, the property trivially holds
        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        // The gimbal yaw of waypoints should reflect the scan heading direction.
        // In the grid strategy, waypoints alternate heading/reverse heading (serpentine).
        // Check that the first waypoint's yaw aligns with the expected heading (±180° for reverse lines).
        var firstWaypointYaw = result.Waypoints[0].GimbalYawDegrees;

        // Normalize to compare: heading can be H or H+180 due to serpentine pattern
        var diff = Math.Abs(NormalizeAngleDifference(firstWaypointYaw, expectedHeading));

        // Allow tolerance of 0.1° for floating-point imprecision
        return diff < 0.1 || Math.Abs(diff - 180.0) < 0.1;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 3: Grid heading defaults to longest polygon axis
    ///     **Validates: Requirements 2.6**
    ///     Property: For any valid polygon, when an invalid heading (outside 0–360°) is provided,
    ///     the engine falls back to the longest axis heading.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ValidPolygonArbitrary), typeof(GridModeParametersArbitrary)])]
    public bool GridHeading_WithInvalidHeading_FallsBackToLongestAxis(
        Polygon polygon, GridModeParameters parameters)
    {
        // Use an invalid heading (negative)
        var paramsInvalidHeading = parameters with { HeadingDegrees = -45.0 };

        if (!polygon.IsValid || polygon.IsEmpty)
        {
            return true;
        }

        var resultInvalid = _strategy.Calculate(polygon, paramsInvalidHeading);

        // Also calculate with null heading to get the default behavior
        var paramsNoHeading = parameters with { HeadingDegrees = null };
        var resultDefault = _strategy.Calculate(polygon, paramsNoHeading);

        // Both should produce the same waypoints (same heading was used)
        if (resultInvalid.Waypoints.Count == 0 && resultDefault.Waypoints.Count == 0)
        {
            return true;
        }

        if (resultInvalid.Waypoints.Count != resultDefault.Waypoints.Count)
        {
            return false;
        }

        // Compare first waypoint yaw to verify same heading was used
        if (resultInvalid.Waypoints.Count > 0)
        {
            var yawInvalid = resultInvalid.Waypoints[0].GimbalYawDegrees;
            var yawDefault = resultDefault.Waypoints[0].GimbalYawDegrees;
            var diff = Math.Abs(NormalizeAngleDifference(yawInvalid, yawDefault));
            return diff < 0.1;
        }

        return true;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 4: All grid waypoints lie within polygon boundary
    ///     **Validates: Requirements 2.7**
    ///     Property: For any valid polygon and valid grid parameters, every generated waypoint's
    ///     (latitude, longitude) coordinate lies within or on the boundary of the source polygon
    ///     (using NTS spatial containment with a small tolerance for floating-point precision).
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ValidPolygonArbitrary), typeof(GridModeParametersArbitrary)])]
    public bool AllGridWaypoints_LieWithinPolygonBoundary(
        Polygon polygon, GridModeParameters parameters)
    {
        if (!polygon.IsValid || polygon.IsEmpty)
        {
            return true;
        }

        var result = _strategy.Calculate(polygon, parameters);

        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        // Buffer the polygon by a small tolerance (~1e-6 degrees ≈ 0.1m) to account
        // for floating-point precision in the clipping and waypoint distribution.
        var tolerance = 1e-6;
        var bufferedPolygon = polygon.Buffer(tolerance);

        foreach (var waypoint in result.Waypoints)
        {
            var point = new Point(waypoint.Longitude, waypoint.Latitude)
            {
                SRID = 4326
            };

            if (!bufferedPolygon.Contains(point))
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
