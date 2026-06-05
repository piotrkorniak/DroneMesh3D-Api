using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Tests.FlightPath.Arbitraries;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.Tests.FlightPath;

/// <summary>
///     Feature: flight-path-calculation, Property 7: Gimbal pitch bounded to [-90°, -45°] for all modes
///     **Validates: Requirements 4.1, 4.2, 4.3**
/// </summary>
public sealed class GimbalPitchPropertyTests
{
    private readonly GridFlightPathStrategy _gridStrategy = new();
    private readonly PoiFlightPathStrategy _poiStrategy = new();

    /// <summary>
    ///     Feature: flight-path-calculation, Property 7: Gimbal pitch bounded to [-90°, -45°] for all modes
    ///     **Validates: Requirements 4.1, 4.3**
    ///     Property: For any valid polygon and valid grid parameters, every generated waypoint's
    ///     gimbal pitch shall be within the range [-90°, -45°] inclusive.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ValidPolygonArbitrary), typeof(GridModeParametersArbitrary)])]
    public bool GridMode_AllWaypoints_GimbalPitchWithinBounds(
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

        return result.Waypoints.All(wp =>
            wp.GimbalPitchDegrees >= -90.0 && wp.GimbalPitchDegrees <= -45.0);
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 7: Gimbal pitch bounded to [-90°, -45°] for all modes
    ///     **Validates: Requirements 4.2, 4.3**
    ///     Property: For any valid POI parameters, every generated waypoint's
    ///     gimbal pitch shall be within the range [-90°, -45°] inclusive.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(PoiModeParametersArbitrary)])]
    public bool PoiMode_AllWaypoints_GimbalPitchWithinBounds(PoiModeParameters parameters)
    {
        var result = _poiStrategy.Calculate(parameters);

        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        return result.Waypoints.All(wp =>
            wp.GimbalPitchDegrees >= -90.0 && wp.GimbalPitchDegrees <= -45.0);
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 7: Gimbal pitch bounded to [-90°, -45°] for all modes
    ///     **Validates: Requirements 4.2, 4.3**
    ///     Property: For POI parameters with StructureHeightM (geometry-computed pitch),
    ///     every generated waypoint's gimbal pitch shall still be within [-90°, -45°] inclusive.
    ///     Tests that clamping works correctly for geometry-based pitch computations.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(PoiWithStructureHeightArbitrary)])]
    public bool PoiMode_WithStructureHeight_GimbalPitchClampedToBounds(PoiModeParameters parameters)
    {
        var result = _poiStrategy.Calculate(parameters);

        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        return result.Waypoints.All(wp =>
            wp.GimbalPitchDegrees >= -90.0 && wp.GimbalPitchDegrees <= -45.0);
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 7: Gimbal pitch bounded to [-90°, -45°] for all modes
    ///     **Validates: Requirements 4.2, 4.3**
    ///     Property: For POI parameters with extreme gimbal pitch values (outside [-90, -45]),
    ///     every generated waypoint's gimbal pitch shall be clamped to [-90°, -45°] inclusive.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(PoiWithExtremeGimbalPitchArbitrary)])]
    public bool PoiMode_WithExtremePitch_GimbalPitchClampedToBounds(PoiModeParameters parameters)
    {
        var result = _poiStrategy.Calculate(parameters);

        if (result.Waypoints.Count == 0)
        {
            return true;
        }

        return result.Waypoints.All(wp =>
            wp.GimbalPitchDegrees >= -90.0 && wp.GimbalPitchDegrees <= -45.0);
    }
}

/// <summary>
///     Generates PoiModeParameters with StructureHeightM always set (non-null)
///     to exercise the geometry-based gimbal pitch calculation.
///     Includes extreme structure heights that push the computed pitch outside bounds.
/// </summary>
public sealed class PoiWithStructureHeightArbitrary
{
    public static Arbitrary<PoiModeParameters> PoiModeParameters()
    {
        var gen =
            Gen.Choose(-8500, 8500).SelectMany(latHundredths =>
                Gen.Choose(-18000, 18000).SelectMany(lonHundredths =>
                    Gen.Choose(50, 5000).SelectMany(radiusTenths =>
                        Gen.Choose(10, 1200).SelectMany(altitudeTenths =>
                            Gen.Choose(4, 72).SelectMany(photoCount =>
                                Gen.Choose(1, 1500).Select(structureHeightTenths =>
                                {
                                    var altitude = altitudeTenths / 10.0;
                                    var structureHeight = structureHeightTenths / 10.0;

                                    return new PoiModeParameters(
                                        latHundredths / 100.0,
                                        lonHundredths / 100.0,
                                        radiusTenths / 10.0,
                                        altitude,
                                        -90.0, // Will be overridden by StructureHeightM calc
                                        photoCount,
                                        null,
                                        null,
                                        structureHeight);
                                }))))));

        return Arb.From(gen);
    }
}

/// <summary>
///     Generates PoiModeParameters with extreme GimbalPitchDegrees values (outside [-90, -45])
///     and no StructureHeightM, to verify clamping of user-specified pitch values.
/// </summary>
public sealed class PoiWithExtremeGimbalPitchArbitrary
{
    public static Arbitrary<PoiModeParameters> PoiModeParameters()
    {
        // Generate pitch values outside [-90, -45]: either < -90 or > -45
        var extremePitchGen = Gen.OneOf(
            Gen.Choose(-1800, -910).Select(v => v / 10.0), // -180 to -91
            Gen.Choose(-440, 0).Select(v => v / 10.0)); // -44 to 0

        var gen =
            Gen.Choose(-8500, 8500).SelectMany(latHundredths =>
                Gen.Choose(-18000, 18000).SelectMany(lonHundredths =>
                    Gen.Choose(50, 5000).SelectMany(radiusTenths =>
                        Gen.Choose(10, 1200).SelectMany(altitudeTenths =>
                            Gen.Choose(4, 72).SelectMany(photoCount =>
                                extremePitchGen.Select(gimbalPitch =>
                                    new PoiModeParameters(
                                        latHundredths / 100.0,
                                        lonHundredths / 100.0,
                                        radiusTenths / 10.0,
                                        altitudeTenths / 10.0,
                                        gimbalPitch,
                                        photoCount,
                                        null,
                                        null,
                                        null)))))));

        return Arb.From(gen);
    }
}
