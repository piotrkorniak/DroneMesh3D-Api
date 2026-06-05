using DroneMesh3D.Core.FlightPath;
using FsCheck;
using FsCheck.Fluent;

namespace DroneMesh3D.Core.Tests.FlightPath.Arbitraries;

/// <summary>
///     Generates valid PoiModeParameters for property-based testing.
///     Center points: lat ∈ [-85, 85], lon ∈ [-180, 180] (avoids poles).
///     Radii: 5–500m, Altitudes: 1–120m, GimbalPitch: -90 to -45.
///     PhotoCount: 4–72 (or null with overlap). OverlapPercent: 60–90% when PhotoCount is null.
///     CameraHorizontalFovDegrees: 60–90° when overlap is specified.
///     StructureHeightM: null or 1–50m.
/// </summary>
public sealed class PoiModeParametersArbitrary
{
    /// <summary>
    ///     Generates PoiModeParameters with an explicit PhotoCount (no overlap-based derivation).
    /// </summary>
    public static Arbitrary<PoiModeParameters> PoiModeParameters()
    {
        var genWithPhotoCount =
            Gen.Choose(-8500, 8500).SelectMany(latHundredths =>
                Gen.Choose(-18000, 18000).SelectMany(lonHundredths =>
                    Gen.Choose(50, 5000).SelectMany(radiusTenths =>
                        Gen.Choose(10, 1200).SelectMany(altitudeTenths =>
                            Gen.Choose(-900, -450).SelectMany(gimbalPitchTenths =>
                                Gen.Choose(4, 72).SelectMany(photoCount =>
                                    Gen.Elements<double?>(null, 5, 10, 20, 30, 40, 50).Select(structureHeight =>
                                        new PoiModeParameters(
                                            latHundredths / 100.0,
                                            lonHundredths / 100.0,
                                            radiusTenths / 10.0,
                                            altitudeTenths / 10.0,
                                            gimbalPitchTenths / 10.0,
                                            photoCount,
                                            null,
                                            null,
                                            structureHeight))))))));

        var genWithOverlap =
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

        // Bias towards explicit photo count (more common usage)
        var combined = Gen.Frequency(
            (7, genWithPhotoCount),
            (3, genWithOverlap));

        return Arb.From(combined);
    }
}
