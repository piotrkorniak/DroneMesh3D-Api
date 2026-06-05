using DroneMesh3D.Core.FlightPath;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DroneMesh3D.Core.Tests.FlightPath;

/// <summary>
///     Feature: flight-path-calculation, Property 10: POI photo count satisfies desired overlap
///     **Validates: Requirements 6.5**
/// </summary>
public sealed class PoiPhotoCountPropertyTests
{
    private readonly PoiFlightPathStrategy _strategy = new();

    /// <summary>
    ///     Feature: flight-path-calculation, Property 10: POI photo count satisfies desired overlap
    ///     **Validates: Requirements 6.5**
    ///     Property: For any valid POI parameters specifying overlap (instead of explicit photo count),
    ///     given a radius and camera horizontal FOV, the computed photo count shall be sufficient such
    ///     that adjacent photos overlap by at least the requested percentage.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(PoiOverlapParametersArbitrary)])]
    public bool PoiPhotoCount_SatisfiesDesiredOverlap(PoiModeParameters parameters)
    {
        var result = _strategy.Calculate(parameters);

        // The photo count must produce sufficient overlap
        var photoCount = result.Waypoints.Count;
        var horizontalFov = parameters.CameraHorizontalFovDegrees!.Value;
        var requestedOverlap = parameters.OverlapPercent!.Value;

        // Step angle between adjacent photos
        var stepAngle = 360.0 / photoCount;

        // Each photo covers horizontalFov degrees of the orbit
        var photoCoverage = horizontalFov;

        // Actual overlap percentage between adjacent photos
        var actualOverlap = (photoCoverage - stepAngle) / photoCoverage * 100.0;

        // The actual overlap must be at least the requested overlap
        return actualOverlap >= requestedOverlap;
    }
}

/// <summary>
///     Generates PoiModeParameters with overlap-based photo count derivation
///     (PhotoCount = null, OverlapPercent and CameraHorizontalFovDegrees specified).
///     OverlapPercent: 60–90% (realistic range)
///     CameraHorizontalFovDegrees: 60–90° (realistic camera FOV)
/// </summary>
public sealed class PoiOverlapParametersArbitrary
{
    public static Arbitrary<PoiModeParameters> PoiModeParameters()
    {
        var gen =
            Gen.Choose(-8500, 8500).SelectMany(latHundredths =>
                Gen.Choose(-18000, 18000).SelectMany(lonHundredths =>
                    Gen.Choose(50, 5000).SelectMany(radiusTenths =>
                        Gen.Choose(10, 1200).SelectMany(altitudeTenths =>
                            Gen.Choose(-900, -450).SelectMany(gimbalPitchTenths =>
                                Gen.Choose(600, 900).SelectMany(overlapTenths =>
                                    Gen.Choose(600, 900).SelectMany(fovTenths =>
                                        Gen.Elements<double?>(null, 5, 10, 20, 30, 40, 50).Select(structureHeight =>
                                            new PoiModeParameters(
                                                latHundredths / 100.0,
                                                lonHundredths / 100.0,
                                                radiusTenths / 10.0,
                                                altitudeTenths / 10.0,
                                                gimbalPitchTenths / 10.0,
                                                null,
                                                overlapTenths / 10.0,
                                                fovTenths / 10.0,
                                                structureHeight)))))))));

        return Arb.From(gen);
    }
}
