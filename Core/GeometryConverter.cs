using DroneMesh3D.Core.Models;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core;

/// <summary>
///     Converts between GeoJSON coordinate arrays and NTS geometry types.
/// </summary>
public static class GeometryConverter
{
    private const int Srid = 4326;

    /// <summary>
    ///     Converts a GeoJSON outer ring (double[][]) to an NTS Polygon with SRID 4326.
    /// </summary>
    public static Polygon ToPolygon(double[][] outerRing)
    {
        var factory = new GeometryFactory(new PrecisionModel(), Srid);
        var coordinates = outerRing
            .Select(point => new Coordinate(point[0], point[1]))
            .ToArray();

        var linearRing = factory.CreateLinearRing(coordinates);
        return factory.CreatePolygon(linearRing);
    }

    /// <summary>
    ///     Converts an NTS Polygon back to a GeoJsonGeometry record.
    /// </summary>
    public static GeoJsonGeometry ToGeoJson(Geometry geometry)
    {
        var coordinates = geometry.Coordinates
            .Select(c => new[] { c.X, c.Y })
            .ToArray();

        return new GeoJsonGeometry("Polygon", [coordinates]);
    }
}
