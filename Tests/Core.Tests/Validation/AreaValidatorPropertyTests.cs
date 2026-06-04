using DroneMesh3D.Core.Validation;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DroneMesh3D.Core.Tests.Validation;

/// <summary>
///     Property 7: Zgodność walidacji frontend-backend.
///     **Validates: Requirements 5.4, 5.5**
///     Since both AreaValidator (C#) and PolygonValidatorService (TS) implement
///     identical algorithms, these property-based tests verify that AreaValidator
///     behaves according to the documented rules which are shared with the frontend:
///     - &lt;3 distinct vertices → invalid (MIN_VERTICES)
///     - Not closed → invalid (CLOSURE)
///     - Self-intersecting → invalid (SELF_INTERSECTION)
///     - Area &gt; 50000 m² → invalid (AREA_TOO_LARGE)
///     - Area &lt; 100 m² → invalid (AREA_TOO_SMALL)
///     - Valid polygon → valid
/// </summary>
public sealed class AreaValidatorPropertyTests
{
    private readonly AreaValidator _validator = new();

    /// <summary>
    ///     **Validates: Requirements 5.4, 5.5**
    ///     Property: Rings with fewer than 3 distinct vertices are always invalid (MIN_VERTICES rule).
    ///     Both frontend and backend must reject polygons with fewer than 3 distinct vertices.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(TooFewVerticesArbitrary)])]
    public bool TooFewVertices_AlwaysInvalid(double[][] ring)
    {
        var result = _validator.Validate(ring);
        return !result.IsValid &&
               result.Errors.Exists(e => e.Contains("at least 3 vertices"));
    }

    /// <summary>
    ///     **Validates: Requirements 5.4, 5.5**
    ///     Property: Rings that are not closed (first != last coordinate) are always invalid (CLOSURE rule).
    ///     Both frontend and backend must reject unclosed polygons.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(UnclosedRingArbitrary)])]
    public bool NotClosed_AlwaysInvalid(double[][] ring)
    {
        var result = _validator.Validate(ring);
        return !result.IsValid &&
               result.Errors.Exists(e => e.Contains("closed"));
    }

    /// <summary>
    ///     **Validates: Requirements 5.4, 5.5**
    ///     Property: Self-intersecting polygons are always invalid (SELF_INTERSECTION rule).
    ///     Both frontend and backend must reject self-intersecting polygons.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(SelfIntersectingArbitrary)])]
    public bool SelfIntersecting_AlwaysInvalid(double[][] ring)
    {
        var result = _validator.Validate(ring);
        return !result.IsValid &&
               result.Errors.Exists(e => e.Contains("self-intersect"));
    }

    /// <summary>
    ///     **Validates: Requirements 5.4, 5.5**
    ///     Property: Polygons with area &gt; 50 000 m² are always invalid (AREA_TOO_LARGE rule).
    ///     Both frontend and backend enforce max 5 hectares.
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(LargeAreaArbitrary)])]
    public bool AreaTooLarge_AlwaysInvalid(double[][] ring)
    {
        var result = _validator.Validate(ring);
        return !result.IsValid &&
               result.Errors.Exists(e => e.Contains("exceeds maximum"));
    }

    /// <summary>
    ///     **Validates: Requirements 5.4, 5.5**
    ///     Property: Polygons with area &lt; 100 m² are always invalid (AREA_TOO_SMALL rule).
    ///     Both frontend and backend enforce min 100 m².
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(SmallAreaArbitrary)])]
    public bool AreaTooSmall_AlwaysInvalid(double[][] ring)
    {
        var result = _validator.Validate(ring);
        return !result.IsValid &&
               result.Errors.Exists(e => e.Contains("below minimum"));
    }

    /// <summary>
    ///     **Validates: Requirements 5.4, 5.5**
    ///     Property: Valid polygons (closed, ≥3 vertices, no self-intersections, area in [100, 50000] m²)
    ///     are always accepted. Both frontend and backend must agree on valid polygons.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ValidRingArbitrary)])]
    public bool ValidPolygon_AlwaysValid(double[][] ring)
    {
        var result = _validator.Validate(ring);
        return result.IsValid;
    }
}

#region Arbitrary Classes

/// <summary>
///     Generates rings with fewer than 3 distinct vertices (0, 1, or 2 distinct points).
/// </summary>
public sealed class TooFewVerticesArbitrary
{
    public static Arbitrary<double[][]> Ring()
    {
        var empty = Gen.Constant(Array.Empty<double[]>());

        var singleVertex = Generators.GenCoordinate().Select(p => new[] { p, p });

        var twoVertices = Generators.GenCoordinate().Two()
            .Where(t => t.Item1[0] != t.Item2[0] || t.Item1[1] != t.Item2[1])
            .Select(t => new[] { t.Item1, t.Item2, t.Item1 });

        var gen = Gen.OneOf(empty, singleVertex, twoVertices);
        return Arb.From(gen);
    }
}

/// <summary>
///     Generates rings with ≥3 distinct vertices that are NOT closed (first != last).
/// </summary>
public sealed class UnclosedRingArbitrary
{
    public static Arbitrary<double[][]> Ring()
    {
        var gen = Gen.Choose(3, 8).SelectMany(vertexCount =>
            Generators.GenCoordinate().ArrayOf(vertexCount)
                .Where(coords =>
                {
                    if (coords.Length < 3) return false;
                    var first = coords[0];
                    var last = coords[^1];
                    return first[0] != last[0] || first[1] != last[1];
                }));

        return Arb.From(gen);
    }
}

/// <summary>
///     Generates self-intersecting (bowtie) polygons.
/// </summary>
public sealed class SelfIntersectingArbitrary
{
    public static Arbitrary<double[][]> Ring()
    {
        var gen = Gen.Choose(-1790, 1790).SelectMany(lonInt =>
            Gen.Choose(-800, 800).SelectMany(latInt =>
                Gen.Choose(5, 30).Select(offsetInt =>
                {
                    var baseLon = lonInt / 10.0;
                    var baseLat = latInt / 10.0;
                    var offset = offsetInt / 10000.0;

                    // Bowtie shape: path BL→TR→BR→TL→BL creates crossing edges.
                    // Edge 0 (BL→TR) and Edge 2 (BR→TL) intersect.
                    var bl = new[] { baseLon - offset, baseLat - offset };
                    var tr = new[] { baseLon + offset, baseLat + offset };
                    var br = new[] { baseLon + offset, baseLat - offset };
                    var tl = new[] { baseLon - offset, baseLat + offset };

                    return new[] { bl, tr, br, tl, bl };
                })));

        return Arb.From(gen);
    }
}

/// <summary>
///     Generates closed, simple (non-self-intersecting) rectangles with area &gt; 50 000 m².
/// </summary>
public sealed class LargeAreaArbitrary
{
    public static Arbitrary<double[][]> Ring()
    {
        var gen = Gen.Choose(-1790, 1790).SelectMany(lonInt =>
            Gen.Choose(-600, 600).SelectMany(latInt =>
                Gen.Choose(50, 200).SelectMany(lonSteps =>
                    Gen.Choose(50, 200).Select(latSteps =>
                    {
                        var baseLon = lonInt / 10.0;
                        var baseLat = latInt / 10.0;
                        var lonOffset = lonSteps / 10000.0;
                        var latOffset = latSteps / 10000.0;

                        return new[]
                        {
                            new[] { baseLon, baseLat },
                            new[] { baseLon + lonOffset, baseLat },
                            new[] { baseLon + lonOffset, baseLat + latOffset },
                            new[] { baseLon, baseLat + latOffset },
                            new[] { baseLon, baseLat }
                        };
                    }))));

        return Arb.From(gen);
    }
}

/// <summary>
///     Generates closed, simple rectangles with area &lt; 100 m².
/// </summary>
public sealed class SmallAreaArbitrary
{
    public static Arbitrary<double[][]> Ring()
    {
        var gen = Gen.Choose(-1790, 1790).SelectMany(lonInt =>
            Gen.Choose(-600, 600).SelectMany(latInt =>
                Gen.Choose(1, 5).SelectMany(lonSteps =>
                    Gen.Choose(1, 5).Select(latSteps =>
                    {
                        var baseLon = lonInt / 10.0;
                        var baseLat = latInt / 10.0;
                        var lonOffset = lonSteps / 100000.0;
                        var latOffset = latSteps / 100000.0;

                        return new[]
                        {
                            new[] { baseLon, baseLat },
                            new[] { baseLon + lonOffset, baseLat },
                            new[] { baseLon + lonOffset, baseLat + latOffset },
                            new[] { baseLon, baseLat + latOffset },
                            new[] { baseLon, baseLat }
                        };
                    }))));

        return Arb.From(gen);
    }
}

/// <summary>
///     Generates valid polygons: closed, ≥3 vertices, non-self-intersecting, area in [100, 50000] m².
/// </summary>
public sealed class ValidRingArbitrary
{
    public static Arbitrary<double[][]> Ring()
    {
        var gen = Gen.Choose(-1790, 1790).SelectMany(lonInt =>
                Gen.Choose(-600, 600).SelectMany(latInt =>
                    Gen.Choose(3, 40).SelectMany(lonSteps =>
                        Gen.Choose(3, 40).Select(latSteps =>
                        {
                            var baseLon = lonInt / 10.0;
                            var baseLat = latInt / 10.0;
                            var lonOffset = lonSteps / 10000.0;
                            var latOffset = latSteps / 10000.0;

                            return new[]
                            {
                                new[] { baseLon, baseLat },
                                new[] { baseLon + lonOffset, baseLat },
                                new[] { baseLon + lonOffset, baseLat + latOffset },
                                new[] { baseLon, baseLat + latOffset },
                                new[] { baseLon, baseLat }
                            };
                        }))))
            .Where(ring =>
            {
                var validator = new AreaValidator();
                var area = validator.CalculateAreaSqm(ring);
                return area >= 100 && area <= 50000;
            });

        return Arb.From(gen);
    }
}

#endregion

#region Shared Generators

internal static class Generators
{
    public static Gen<double[]> GenCoordinate()
    {
        return Gen.Choose(-1800, 1800).Two()
            .Select(t => new[] { t.Item1 / 10.0, t.Item2 / 10.0 });
    }
}

#endregion
