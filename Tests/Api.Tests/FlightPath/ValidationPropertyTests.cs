using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.Validators;
using DroneMesh3D.Core.FlightPath;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DroneMesh3D.Api.Tests.FlightPath;

/// <summary>
///     Feature: flight-path-calculation, Property 8: Altitude validation boundary at 120m
///     Feature: flight-path-calculation, Property 9: Overlap validation boundaries
///     Feature: flight-path-calculation, Property 12: Invalid parameters produce validation error
///     **Validates: Requirements 5.1, 5.2, 5.3, 6.1, 6.2, 6.3, 6.4, 9.4**
/// </summary>
public sealed class ValidationPropertyTests
{
    private readonly CalculateFlightPathCommandValidator _validator = new();

    private static CalculateFlightPathCommand ValidGridCommand(
        double? altitudeM = null,
        double? frontOverlap = null,
        double? sideOverlap = null,
        CameraParameters? camera = null) =>
        new(
            Guid.NewGuid(),
            FlightMode.Grid,
            new GridModeParameters(
                altitudeM ?? 80,
                camera ?? new CameraParameters(13.2, 8.8, 5472, 3648),
                frontOverlap ?? 78,
                sideOverlap ?? 70,
                null),
            null);

    #region Property 8: Altitude validation boundary at 120m

    /// <summary>
    ///     Feature: flight-path-calculation, Property 8: Altitude validation boundary at 120m
    ///     **Validates: Requirements 5.1, 5.2, 5.3**
    ///     Property: For any altitude value > 120m, the engine shall reject the flight plan with a validation error.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(AltitudeAbove120Arbitrary)])]
    public bool Altitude_Above120m_ValidationFails(double altitude)
    {
        var command = ValidGridCommand(altitude);
        var result = _validator.Validate(command);
        return !result.IsValid;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 8: Altitude validation boundary at 120m
    ///     **Validates: Requirements 5.1, 5.2, 5.3**
    ///     Property: For any altitude value in the range (0, 120] m, the engine shall accept the altitude as valid.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(AltitudeValid0To120Arbitrary)])]
    public bool Altitude_InRange0To120_ValidationPasses(double altitude)
    {
        var command = ValidGridCommand(altitude);
        var result = _validator.Validate(command);
        // Check that there are no altitude-related errors
        return !result.Errors.Exists(e =>
            e.PropertyName.Contains("AltitudeM", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 8: Altitude validation boundary at 120m
    ///     **Validates: Requirements 5.1, 5.2, 5.3**
    ///     Property: For any altitude value <= 0, the engine shall reject with a validation error.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(AltitudeZeroOrNegativeArbitrary)])]
    public bool Altitude_ZeroOrNegative_ValidationFails(double altitude)
    {
        var command = ValidGridCommand(altitude);
        var result = _validator.Validate(command);
        return !result.IsValid;
    }

    #endregion

    #region Property 9: Overlap validation boundaries

    /// <summary>
    ///     Feature: flight-path-calculation, Property 9: Overlap validation boundaries
    ///     **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    ///     Property: For any front overlap value outside [75, 80]%, the engine shall reject with a validation error.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(FrontOverlapOutOfRangeArbitrary)])]
    public bool FrontOverlap_OutsideRange_ValidationFails(double frontOverlap)
    {
        var command = ValidGridCommand(frontOverlap: frontOverlap);
        var result = _validator.Validate(command);
        return !result.IsValid;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 9: Overlap validation boundaries
    ///     **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    ///     Property: For any side overlap value outside [65, 75]%, the engine shall reject with a validation error.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(SideOverlapOutOfRangeArbitrary)])]
    public bool SideOverlap_OutsideRange_ValidationFails(double sideOverlap)
    {
        var command = ValidGridCommand(sideOverlap: sideOverlap);
        var result = _validator.Validate(command);
        return !result.IsValid;
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 9: Overlap validation boundaries
    ///     **Validates: Requirements 6.1, 6.2, 6.3, 6.4**
    ///     Property: For any front overlap in [75, 80]% and side overlap in [65, 75]%,
    ///     the engine shall accept both values.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(OverlapValidRangeArbitrary)])]
    public bool Overlap_InValidRange_ValidationPasses(ValidOverlapPair overlapPair)
    {
        var command = ValidGridCommand(frontOverlap: overlapPair.FrontOverlap, sideOverlap: overlapPair.SideOverlap);
        var result = _validator.Validate(command);
        // Check no overlap-related errors exist
        return !result.Errors.Exists(e =>
            e.PropertyName.Contains("OverlapPercent", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Property 12: Invalid parameters produce validation error

    /// <summary>
    ///     Feature: flight-path-calculation, Property 12: Invalid parameters produce validation error
    ///     **Validates: Requirements 9.4**
    ///     Property: For any request with altitude > 120m, the engine shall return a validation error
    ///     (never a success or unhandled exception).
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(AltitudeAbove120Arbitrary)])]
    public bool InvalidAltitude_ProducesValidationError_NeverThrows(double altitude)
    {
        try
        {
            var command = ValidGridCommand(altitude);
            var result = _validator.Validate(command);
            return !result.IsValid;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 12: Invalid parameters produce validation error
    ///     **Validates: Requirements 9.4**
    ///     Property: For any request with invalid camera parameters (values <= 0),
    ///     the engine shall return a validation error.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(InvalidCameraArbitrary)])]
    public bool InvalidCamera_ProducesValidationError_NeverThrows(CameraParameters camera)
    {
        try
        {
            var command = ValidGridCommand(camera: camera);
            var result = _validator.Validate(command);
            return !result.IsValid;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 12: Invalid parameters produce validation error
    ///     **Validates: Requirements 9.4**
    ///     Property: For any request with missing mode parameters (Grid mode but no grid params),
    ///     the engine shall return a validation error.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool MissingGridParams_ProducesValidationError_NeverThrows()
    {
        try
        {
            var command = new CalculateFlightPathCommand(
                Guid.NewGuid(),
                FlightMode.Grid,
                null, // missing grid params
                null);
            var result = _validator.Validate(command);
            return !result.IsValid;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Feature: flight-path-calculation, Property 12: Invalid parameters produce validation error
    ///     **Validates: Requirements 9.4**
    ///     Property: For any request with missing POI params when mode is POI,
    ///     the engine shall return a validation error.
    /// </summary>
    [Property(MaxTest = 200)]
    public bool MissingPoiParams_ProducesValidationError_NeverThrows()
    {
        try
        {
            var command = new CalculateFlightPathCommand(
                Guid.NewGuid(),
                FlightMode.Poi,
                null,
                null); // missing poi params
            var result = _validator.Validate(command);
            return !result.IsValid;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

#region Arbitrary Classes

/// <summary>
///     Generates altitude values strictly greater than 120m.
/// </summary>
public sealed class AltitudeAbove120Arbitrary
{
    public static Arbitrary<double> Altitude()
    {
        // Generate altitudes from 120.1 to 500m (in tenths for precision)
        var gen = Gen.Choose(1201, 5000).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }
}

/// <summary>
///     Generates valid altitude values in the range (0, 120].
/// </summary>
public sealed class AltitudeValid0To120Arbitrary
{
    public static Arbitrary<double> Altitude()
    {
        // Generate altitudes from 0.1 to 120.0m (in tenths for precision)
        var gen = Gen.Choose(1, 1200).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }
}

/// <summary>
///     Generates altitude values that are zero or negative.
/// </summary>
public sealed class AltitudeZeroOrNegativeArbitrary
{
    public static Arbitrary<double> Altitude()
    {
        // Generate altitudes from -100 to 0 (in tenths for precision)
        var gen = Gen.Choose(-1000, 0).Select(tenths => tenths / 10.0);
        return Arb.From(gen);
    }
}

/// <summary>
///     Generates front overlap values outside the valid range [75, 80]%.
/// </summary>
public sealed class FrontOverlapOutOfRangeArbitrary
{
    public static Arbitrary<double> FrontOverlap()
    {
        var belowRange = Gen.Choose(0, 749).Select(tenths => tenths / 10.0);
        var aboveRange = Gen.Choose(801, 1000).Select(tenths => tenths / 10.0);
        var gen = Gen.OneOf(belowRange, aboveRange);
        return Arb.From(gen);
    }
}

/// <summary>
///     Generates side overlap values outside the valid range [65, 75]%.
/// </summary>
public sealed class SideOverlapOutOfRangeArbitrary
{
    public static Arbitrary<double> SideOverlap()
    {
        var belowRange = Gen.Choose(0, 649).Select(tenths => tenths / 10.0);
        var aboveRange = Gen.Choose(751, 1000).Select(tenths => tenths / 10.0);
        var gen = Gen.OneOf(belowRange, aboveRange);
        return Arb.From(gen);
    }
}

/// <summary>
///     Wrapper type for a valid pair of front and side overlap values.
/// </summary>
public sealed record ValidOverlapPair(double FrontOverlap, double SideOverlap);

/// <summary>
///     Generates valid front overlap in [75, 80]% and side overlap in [65, 75]%.
/// </summary>
public sealed class OverlapValidRangeArbitrary
{
    public static Arbitrary<ValidOverlapPair> OverlapPair()
    {
        var gen = Gen.Choose(750, 800).SelectMany(frontTenths =>
            Gen.Choose(650, 750).Select(sideTenths =>
                new ValidOverlapPair(frontTenths / 10.0, sideTenths / 10.0)));
        return Arb.From(gen);
    }
}

/// <summary>
///     Generates invalid camera parameters where at least one value is <= 0.
/// </summary>
public sealed class InvalidCameraArbitrary
{
    public static Arbitrary<CameraParameters> Camera()
    {
        // Generate camera params where at least one value is invalid (<= 0)
        var gen = Gen.Choose(0, 3).SelectMany(invalidField =>
        {
            // Default valid values
            var sensorGen = Gen.Choose(40, 360).Select(t => t / 10.0);
            var focalGen = Gen.Choose(40, 500).Select(t => t / 10.0);
            var widthGen = Gen.Choose(1000, 8000);
            var heightGen = Gen.Choose(1000, 8000);

            // Make one field invalid
            return invalidField switch
            {
                0 => Gen.Choose(-100, 0).Select(v => v / 10.0).SelectMany(invalidSensor =>
                    focalGen.SelectMany(focal =>
                        widthGen.SelectMany(width =>
                            heightGen.Select(height =>
                                new CameraParameters(invalidSensor, focal, width, height))))),
                1 => sensorGen.SelectMany(sensor =>
                    Gen.Choose(-100, 0).Select(v => v / 10.0).SelectMany(invalidFocal =>
                        widthGen.SelectMany(width =>
                            heightGen.Select(height =>
                                new CameraParameters(sensor, invalidFocal, width, height))))),
                2 => sensorGen.SelectMany(sensor =>
                    focalGen.SelectMany(focal =>
                        Gen.Choose(-100, 0).SelectMany(invalidWidth =>
                            heightGen.Select(height =>
                                new CameraParameters(sensor, focal, invalidWidth, height))))),
                _ => sensorGen.SelectMany(sensor =>
                    focalGen.SelectMany(focal =>
                        widthGen.SelectMany(width =>
                            Gen.Choose(-100, 0).Select(invalidHeight =>
                                new CameraParameters(sensor, focal, width, invalidHeight)))))
            };
        });

        return Arb.From(gen);
    }
}

#endregion
