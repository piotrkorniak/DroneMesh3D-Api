using DroneMesh3D.Core.FlightPath;
using FsCheck;
using FsCheck.Fluent;

namespace DroneMesh3D.Core.Tests.FlightPath.Arbitraries;

/// <summary>
///     Generates valid CameraParameters for property-based testing.
///     Sensor widths: 4–36mm, focal lengths: 4–50mm, resolutions: 1000–8000px.
///     Also generates valid altitudes (0 < alt ≤ 120 m) for GSD calculations.
/// </summary>
public sealed class CameraParametersArbitrary
{
    public static Arbitrary<CameraParameters> Camera()
    {
        var gen =
            Gen.Choose(40, 360).SelectMany(sensorWidthTenths =>
                Gen.Choose(40, 500).SelectMany(focalLengthTenths =>
                    Gen.Choose(1000, 8000).SelectMany(imageWidthPx =>
                        Gen.Choose(1000, 8000).Select(imageHeightPx =>
                            new CameraParameters(
                                sensorWidthTenths / 10.0,
                                focalLengthTenths / 10.0,
                                imageWidthPx,
                                imageHeightPx)))));

        return Arb.From(gen);
    }

    public static Arbitrary<double> Altitude()
    {
        // Generate altitudes from 1 to 120m (in tenths for precision)
        var gen = Gen.Choose(10, 1200).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }
}
