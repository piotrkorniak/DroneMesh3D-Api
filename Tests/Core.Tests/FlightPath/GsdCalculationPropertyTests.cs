using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Tests.FlightPath.Arbitraries;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DroneMesh3D.Core.Tests.FlightPath;

/// <summary>
///     Feature: flight-path-calculation, Property 1: GSD and footprint computation correctness
///     Feature: flight-path-calculation, Property 2: Spacing computation from overlap
///     **Validates: Requirements 2.2, 2.3, 2.4, 2.5**
/// </summary>
public sealed class GsdCalculationPropertyTests
{
    /// <summary>
    ///     Feature: flight-path-calculation, Property 1: GSD and footprint computation correctness
    ///     **Validates: Requirements 2.2, 2.3**
    ///     Property: For any valid camera parameters and altitude, the computed GSD equals
    ///     (altitude × sensorWidth) / (focalLength × imageWidth).
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(CameraParametersArbitrary)])]
    public bool Gsd_EqualsExpectedFormula(CameraParameters camera, double altitude)
    {
        var expectedGsd = altitude * camera.SensorWidthMm /
                          (camera.FocalLengthMm * camera.ImageWidthPx);

        var actualGsd = GeodesicMathService.ComputeGsd(
            altitude, camera.SensorWidthMm, camera.FocalLengthMm, camera.ImageWidthPx);

        return Math.Abs(actualGsd - expectedGsd) < 1e-12;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 1: GSD and footprint computation correctness
    ///     **Validates: Requirements 2.2, 2.3**
    ///     Property: For any valid camera parameters and altitude, the photo footprint equals
    ///     (GSD × imageWidthPx, GSD × imageHeightPx).
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(CameraParametersArbitrary)])]
    public bool Footprint_EqualsGsdTimesImageDimensions(CameraParameters camera, double altitude)
    {
        var gsd = GeodesicMathService.ComputeGsd(
            altitude, camera.SensorWidthMm, camera.FocalLengthMm, camera.ImageWidthPx);

        var expectedWidth = gsd * camera.ImageWidthPx;
        var expectedHeight = gsd * camera.ImageHeightPx;

        var (actualWidth, actualHeight) = GeodesicMathService.ComputePhotoFootprint(
            gsd, camera.ImageWidthPx, camera.ImageHeightPx);

        return Math.Abs(actualWidth - expectedWidth) < 1e-10 &&
               Math.Abs(actualHeight - expectedHeight) < 1e-10;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 1: GSD and footprint computation correctness
    ///     **Validates: Requirements 2.2, 2.3**
    ///     Property: GSD is always positive for valid inputs (positive altitude, sensor width, focal length, image width).
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(CameraParametersArbitrary)])]
    public bool Gsd_IsAlwaysPositive(CameraParameters camera, double altitude)
    {
        var gsd = GeodesicMathService.ComputeGsd(
            altitude, camera.SensorWidthMm, camera.FocalLengthMm, camera.ImageWidthPx);

        return gsd > 0;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 2: Spacing computation from overlap
    ///     **Validates: Requirements 2.4, 2.5**
    ///     Property: For any valid footprint and front overlap (75–80%), the photo spacing equals
    ///     footprintHeight × (1 - frontOverlap/100).
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(OverlapArbitrary)])]
    public bool PhotoSpacing_EqualsExpectedFormula(double footprintHeightM, double frontOverlapPercent)
    {
        var frontOverlapDecimal = frontOverlapPercent / 100.0;
        var expectedSpacing = footprintHeightM * (1.0 - frontOverlapDecimal);

        var actualSpacing = GeodesicMathService.ComputePhotoSpacing(footprintHeightM, frontOverlapDecimal);

        return Math.Abs(actualSpacing - expectedSpacing) < 1e-10;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 2: Spacing computation from overlap
    ///     **Validates: Requirements 2.4, 2.5**
    ///     Property: For any valid footprint and side overlap (65–75%), the line spacing equals
    ///     footprintWidth × (1 - sideOverlap/100).
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(OverlapArbitrary)])]
    public bool LineSpacing_EqualsExpectedFormula(double footprintWidthM, double sideOverlapPercent)
    {
        var sideOverlapDecimal = sideOverlapPercent / 100.0;
        var expectedSpacing = footprintWidthM * (1.0 - sideOverlapDecimal);

        var actualSpacing = GeodesicMathService.ComputeLineSpacing(footprintWidthM, sideOverlapDecimal);

        return Math.Abs(actualSpacing - expectedSpacing) < 1e-10;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 2: Spacing computation from overlap
    ///     **Validates: Requirements 2.4, 2.5**
    ///     Property: Photo spacing and line spacing are always positive for valid overlap values.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(OverlapArbitrary)])]
    public bool Spacing_IsAlwaysPositive(double footprintM, double frontOverlapPercent, double sideOverlapPercent)
    {
        var photoSpacing = GeodesicMathService.ComputePhotoSpacing(
            footprintM, frontOverlapPercent / 100.0);
        var lineSpacing = GeodesicMathService.ComputeLineSpacing(
            footprintM, sideOverlapPercent / 100.0);

        return photoSpacing > 0 && lineSpacing > 0;
    }
}

/// <summary>
///     Generates valid overlap percentages and footprint dimensions for property-based testing.
///     Front overlap: 75–80%, Side overlap: 65–75%, Footprint dimensions: realistic positive values.
/// </summary>
public sealed class OverlapArbitrary
{
    public static Arbitrary<double> FrontOverlapPercent()
    {
        // Front overlap: 75–80% (in tenths for finer granularity)
        var gen = Gen.Choose(750, 800).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }

    public static Arbitrary<double> SideOverlapPercent()
    {
        // Side overlap: 65–75% (in tenths for finer granularity)
        var gen = Gen.Choose(650, 750).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }

    public static Arbitrary<double> FootprintHeightM()
    {
        // Realistic footprint height: 10–500 meters
        var gen = Gen.Choose(100, 5000).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }

    public static Arbitrary<double> FootprintWidthM()
    {
        // Realistic footprint width: 10–500 meters
        var gen = Gen.Choose(100, 5000).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }

    public static Arbitrary<double> FootprintM()
    {
        // Generic footprint dimension: 10–500 meters
        var gen = Gen.Choose(100, 5000).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }
}
