using DroneMesh3D.Core.FlightPath;
using FsCheck;
using FsCheck.Fluent;

namespace DroneMesh3D.Core.Tests.FlightPath.Arbitraries;

/// <summary>
///     Generates valid GridModeParameters for property-based testing.
///     Composes CameraParameters, altitude (1–120m), front overlap (75–80%),
///     side overlap (65–75%), and optional heading.
/// </summary>
public sealed class GridModeParametersArbitrary
{
    public static Arbitrary<GridModeParameters> GridModeParameters()
    {
        var gen =
            CameraParametersArbitrary.Camera().Generator.SelectMany(camera =>
                Gen.Choose(10, 1200).SelectMany(altitudeTenths =>
                    Gen.Choose(750, 800).SelectMany(frontOverlapTenths =>
                        Gen.Choose(650, 750).SelectMany(sideOverlapTenths =>
                            Gen.Choose(0, 1).SelectMany(hasHeading =>
                                Gen.Choose(0, 3600).Select(headingTenths =>
                                {
                                    var altitude = altitudeTenths / 10.0;
                                    var frontOverlap = frontOverlapTenths / 10.0;
                                    var sideOverlap = sideOverlapTenths / 10.0;
                                    double? heading = hasHeading == 1 ? headingTenths / 10.0 : null;

                                    return new GridModeParameters(
                                        altitude,
                                        camera,
                                        frontOverlap,
                                        sideOverlap,
                                        heading);
                                }))))));

        return Arb.From(gen);
    }

    /// <summary>
    ///     Generates GridModeParameters with no heading specified (null)
    ///     to test the default heading behavior.
    /// </summary>
    public static Arbitrary<GridModeParameters> GridModeParametersWithoutHeading()
    {
        var gen =
            CameraParametersArbitrary.Camera().Generator.SelectMany(camera =>
                Gen.Choose(10, 1200).SelectMany(altitudeTenths =>
                    Gen.Choose(750, 800).SelectMany(frontOverlapTenths =>
                        Gen.Choose(650, 750).Select(sideOverlapTenths =>
                        {
                            var altitude = altitudeTenths / 10.0;
                            var frontOverlap = frontOverlapTenths / 10.0;
                            var sideOverlap = sideOverlapTenths / 10.0;

                            return new GridModeParameters(
                                altitude,
                                camera,
                                frontOverlap,
                                sideOverlap,
                                null);
                        }))));

        return Arb.From(gen);
    }

    /// <summary>
    ///     Generates GridModeParameters with an invalid heading (outside 0–360°)
    ///     to test the fallback behavior.
    /// </summary>
    public static Arbitrary<GridModeParameters> GridModeParametersWithInvalidHeading()
    {
        // Generate headings outside 0-360: negative or > 360
        var invalidHeadingGen = Gen.OneOf(
            Gen.Choose(-3600, -1).Select(v => v / 10.0),
            Gen.Choose(3601, 7200).Select(v => v / 10.0));

        var gen =
            CameraParametersArbitrary.Camera().Generator.SelectMany(camera =>
                Gen.Choose(10, 1200).SelectMany(altitudeTenths =>
                    Gen.Choose(750, 800).SelectMany(frontOverlapTenths =>
                        Gen.Choose(650, 750).SelectMany(sideOverlapTenths =>
                            invalidHeadingGen.Select(heading =>
                            {
                                var altitude = altitudeTenths / 10.0;
                                var frontOverlap = frontOverlapTenths / 10.0;
                                var sideOverlap = sideOverlapTenths / 10.0;

                                return new GridModeParameters(
                                    altitude,
                                    camera,
                                    frontOverlap,
                                    sideOverlap,
                                    heading);
                            })))));

        return Arb.From(gen);
    }
}
