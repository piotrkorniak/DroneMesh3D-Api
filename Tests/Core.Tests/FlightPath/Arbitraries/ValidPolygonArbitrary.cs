using FsCheck;
using FsCheck.Fluent;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.Tests.FlightPath.Arbitraries;

/// <summary>
///     Generates valid closed polygons with WGS84 coordinates for property-based testing.
///     Polygons are small (within a few hundred meters of a reference point near Wrocław)
///     with 3–8 vertices, ensuring they form valid rings (closed, no self-intersections).
///     Uses convex hull of random points to guarantee valid simple polygons.
/// </summary>
public sealed class ValidPolygonArbitrary
{
    // Reference point near Wrocław, Poland
    private const double CenterLat = 51.1;
    private const double CenterLon = 17.04;

    // Approximate degrees per meter at this latitude
    private const double DegreesPerMeterLat = 1.0 / 111_320.0;
    private static readonly double DegreesPerMeterLon = 1.0 / (111_320.0 * Math.Cos(CenterLat * Math.PI / 180.0));

    private static readonly GeometryFactory Factory = new(new PrecisionModel(), 4326);

    public static Arbitrary<Polygon> Polygon()
    {
        var gen = Gen.Choose(4, 10).SelectMany(pointCount =>
            GenerateConvexHullPolygon(pointCount));

        return Arb.From(gen);
    }

    /// <summary>
    ///     Generates a valid polygon by creating random points and taking their convex hull.
    ///     This guarantees a valid, simple, convex polygon with at least 3 vertices.
    /// </summary>
    private static Gen<Polygon> GenerateConvexHullPolygon(int pointCount)
    {
        // Generate random offsets in meters from center (-150 to +150m in each direction)
        var xOffsetsGen = Gen.Choose(-150, 150).ArrayOf(pointCount);
        var yOffsetsGen = Gen.Choose(-150, 150).ArrayOf(pointCount);

        return xOffsetsGen.SelectMany(xOffsets =>
            yOffsetsGen.Select(yOffsets =>
            {
                var points = new Coordinate[pointCount];
                for (var i = 0; i < pointCount; i++)
                {
                    var lat = CenterLat + yOffsets[i] * DegreesPerMeterLat;
                    var lon = CenterLon + xOffsets[i] * DegreesPerMeterLon;
                    points[i] = new Coordinate(lon, lat);
                }

                // Take convex hull to ensure a valid simple polygon
                var multiPoint = Factory.CreateMultiPointFromCoords(points);
                var hull = multiPoint.ConvexHull();

                // ConvexHull returns a Polygon if there are at least 3 non-collinear points
                if (hull is Polygon poly && poly.IsValid && !poly.IsEmpty && poly.Area > 0)
                {
                    return poly;
                }

                // Fallback: create a simple triangle if points were collinear
                return CreateFallbackTriangle();
            }));
    }

    /// <summary>
    ///     Creates a simple triangle as fallback for degenerate cases.
    /// </summary>
    private static Polygon CreateFallbackTriangle()
    {
        var coords = new[]
        {
            new Coordinate(CenterLon, CenterLat),
            new Coordinate(CenterLon + 100 * DegreesPerMeterLon, CenterLat),
            new Coordinate(CenterLon + 50 * DegreesPerMeterLon, CenterLat + 80 * DegreesPerMeterLat),
            new Coordinate(CenterLon, CenterLat) // close the ring
        };

        var ring = Factory.CreateLinearRing(coords);
        return Factory.CreatePolygon(ring);
    }
}
