using DroneMesh3D.Core.Models;
using DroneMesh3D.Core.Validation;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace DroneMesh3D.Core.Tests.Validation;

/// <summary>
///     Property-based tests for GeoJsonValidator.IsValidPolygon.
///     **Validates: Requirements 5.2, 5.3**
/// </summary>
public sealed class GeoJsonValidatorPropertyTests
{
    private static readonly double[][] ValidRing =
    [
        [21.0122, 52.2297],
        [21.0130, 52.2297],
        [21.0130, 52.2290],
        [21.0122, 52.2290],
        [21.0122, 52.2297]
    ];

    /// <summary>
    ///     Property 1: Null or empty coordinates arrays should be rejected.
    /// </summary>
    [Property(Arbitrary = [typeof(NullOrEmptyCoordinatesArbitraries)])]
    public bool NullOrEmptyCoordinates_AlwaysReturnsFalse(double[][]?[]? coordinates) =>
        !GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

    /// <summary>
    ///     Property 2: Coordinates containing null or empty rings should be rejected.
    /// </summary>
    [Property(Arbitrary = [typeof(BadRingsArbitraries)])]
    public bool CoordinatesWithNullOrEmptyRings_AlwaysReturnsFalse(double[][]?[] coordinates) =>
        !GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

    /// <summary>
    ///     Property 3: Rings containing points with fewer than 2 coordinate values should be rejected.
    /// </summary>
    [Property(Arbitrary = [typeof(ShortPointsArbitraries)])]
    public bool PointsWithLessThanTwoValues_AlwaysReturnsFalse(double[][]?[] coordinates) =>
        !GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

    /// <summary>
    ///     Property 4: Valid structure (type=Polygon, non-empty rings, all points ≥2 values) should be accepted.
    /// </summary>
    [Property(Arbitrary = [typeof(ValidCoordinatesArbitraries)])]
    public bool ValidStructure_AlwaysReturnsTrue(double[][]?[] coordinates) =>
        GeoJsonValidator.IsValidPolygon(GeoJsonType.Polygon, coordinates);

    #region Custom Arbitrary Classes

    public sealed class NullOrEmptyCoordinatesArbitraries
    {
        public static Arbitrary<double[][]?[]?> NullableArray()
        {
            var gen = Gen.Elements(null, Array.Empty<double[][]?>());
            return gen.ToArbitrary();
        }
    }

    public sealed class BadRingsArbitraries
    {
        public static Arbitrary<double[][]?[]> Array()
        {
            // Generate coordinates where at least one ring is null or empty
            var badRing = Gen.Elements(null, System.Array.Empty<double[]>());

            // 1-3 total rings with at least one bad ring at a random position
            var gen = Gen.Choose(1, 3).SelectMany(ringCount =>
                Gen.Choose(0, ringCount - 1).SelectMany(badIndex =>
                    badRing.Select(bad =>
                    {
                        var result = new double[][]?[ringCount];
                        for (var i = 0; i < ringCount; i++)
                        {
                            result[i] = i == badIndex ? bad : ValidRing;
                        }

                        return result;
                    })));

            return gen.ToArbitrary();
        }
    }

    public sealed class ShortPointsArbitraries
    {
        public static Arbitrary<double[][]?[]> Array()
        {
            // Points with 0 or 1 coordinate values (less than the required 2)
            var emptyPoint = Gen.Constant(System.Array.Empty<double>());
            var singleValuePoint = Gen.Choose(-180, 180)
                .Select(v => new[] { (double)v });
            var shortPoint = Gen.OneOf(emptyPoint, singleValuePoint);

            // Normal points with 2 coordinates
            var normalPoint = Gen.Choose(-180, 180).Two()
                .Select(t => new[] { t.Item1, (double)t.Item2 });

            // Build a ring where at least one point is "short"
            var ringWithShortPoint = Gen.Choose(2, 5).SelectMany(normalCount =>
                shortPoint.SelectMany(sp =>
                    normalPoint.ArrayOf(normalCount).SelectMany(normalPoints =>
                        Gen.Choose(0, normalCount).Select(insertIdx =>
                        {
                            var allPoints = normalPoints.ToList();
                            allPoints.Insert(insertIdx, sp);
                            return allPoints.ToArray();
                        }))));

            var gen = ringWithShortPoint.Select(ring => new[] { ring });
            return gen.ToArbitrary();
        }
    }

    public sealed class ValidCoordinatesArbitraries
    {
        public static Arbitrary<double[][]?[]> Array()
        {
            // Points with at least 2 coordinate values (some with optional altitude)
            var point2d = Gen.Choose(-180, 180).Two()
                .Select(t => new[] { t.Item1, (double)t.Item2 });
            var point3d = Gen.Choose(-180, 180).Three()
                .Select(t => new[] { t.Item1, t.Item2, (double)t.Item3 });
            var validPoint = Gen.OneOf(point2d, point3d);

            // A valid ring: 3-8 points, each with ≥2 values
            var validRing = Gen.Choose(3, 8).SelectMany(count =>
                validPoint.ArrayOf(count));

            // 1-2 rings
            var gen = Gen.Choose(1, 2).SelectMany(ringCount =>
                validRing.ArrayOf(ringCount)
                    .Select(rings => rings.Select(r => (double[][]?)r).ToArray()));

            return gen.ToArbitrary();
        }
    }

    #endregion
}
