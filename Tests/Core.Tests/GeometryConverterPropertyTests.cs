using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.Tests.Validation;
using FsCheck.Xunit;

namespace DroneMesh3D.Core.Tests;

/// <summary>
///     Property 8: Round-trip persystencji obszaru.
///     **Validates: Requirements 6.1, 6.3**
///     Verifies that the GeometryConverter round-trip preserves coordinates:
///     1. Generate valid polygon ring (closed, ≥3 vertices, no self-intersections)
///     2. Convert to NTS Polygon via GeometryConverter.ToPolygon()
///     3. Convert back to GeoJSON via GeometryConverter.ToGeoJson()
///     4. Verify: coordinates match original, resulting polygon has valid SRID, geometry is non-empty
///     This validates the core persistence contract: what goes in must come out identical.
///     The NTS Polygon is exactly what gets persisted to PostGIS (Requirement 6.1),
///     and the round-trip ensures the stored geometry can be faithfully reconstructed (Requirement 6.3).
/// </summary>
public sealed class GeometryConverterPropertyTests
{
    /// <summary>
    ///     **Validates: Requirements 6.1, 6.3**
    ///     Property: Converting a valid ring to NTS Polygon and back to GeoJSON preserves all coordinates
    ///     in their original order. This proves that persistence (which stores the NTS Polygon) does not
    ///     lose or corrupt coordinate data.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ValidRingArbitrary)])]
    public bool RoundTrip_PreservesCoordinates(double[][] ring)
    {
        // Act: Convert to NTS Polygon (what gets persisted)
        var polygon = GeometryConverter.ToPolygon(ring);

        // Act: Convert back to GeoJSON (what gets read back)
        var geoJson = GeometryConverter.ToGeoJson(polygon);

        // Assert: Coordinates array has exactly one ring
        if (geoJson.Coordinates.Length != 1) return false;

        var resultRing = geoJson.Coordinates[0];

        // Assert: Same number of coordinate pairs
        if (resultRing.Length != ring.Length) return false;

        // Assert: Each coordinate pair matches the original
        for (var i = 0; i < ring.Length; i++)
        {
            if (Math.Abs(resultRing[i][0] - ring[i][0]) > 1e-10) return false;
            if (Math.Abs(resultRing[i][1] - ring[i][1]) > 1e-10) return false;
        }

        return true;
    }

    /// <summary>
    ///     **Validates: Requirements 6.1, 6.3**
    ///     Property: Converting a valid ring to NTS Polygon produces a geometry with SRID 4326 (WGS 84),
    ///     which is required for PostGIS spatial storage. The polygon must be non-empty and valid.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ValidRingArbitrary)])]
    public bool ToPolygon_ProducesValidSpatialGeometry(double[][] ring)
    {
        var polygon = GeometryConverter.ToPolygon(ring);

        // SRID must be 4326 for PostGIS compatibility (Requirement 6.2 context)
        if (polygon.SRID != 4326) return false;

        // Geometry must not be empty
        if (polygon.IsEmpty) return false;

        // Must be a Polygon type
        if (polygon.GeometryType != "Polygon") return false;

        // Must have the correct number of coordinates (ring is closed, NTS preserves this)
        if (polygon.Coordinates.Length != ring.Length) return false;

        return true;
    }

    /// <summary>
    ///     **Validates: Requirements 6.1, 6.3**
    ///     Property: The GeoJSON output always has type "Polygon" and non-empty coordinates,
    ///     ensuring the stored entity can always be reconstructed into a valid API response
    ///     with unique ID and timestamp context.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ValidRingArbitrary)])]
    public bool ToGeoJson_ProducesValidGeoJsonStructure(double[][] ring)
    {
        var polygon = GeometryConverter.ToPolygon(ring);
        var geoJson = GeometryConverter.ToGeoJson(polygon);

        // Type must be "Polygon"
        if (geoJson.Type != "Polygon") return false;

        // Must have exactly one ring (outer ring, no holes)
        if (geoJson.Coordinates.Length != 1) return false;

        // Ring must not be empty
        if (geoJson.Coordinates[0].Length == 0) return false;

        // Each coordinate must have exactly 2 elements (longitude, latitude)
        foreach (var coord in geoJson.Coordinates[0])
            if (coord.Length != 2)
                return false;

        return true;
    }

    /// <summary>
    ///     **Validates: Requirements 6.1, 6.3**
    ///     Property: Simulated persistence round-trip — creating an AreaEntity with the converted
    ///     polygon, then reading it back via GeometryConverter, produces identical coordinates.
    ///     This simulates the full save/load cycle without requiring a database.
    /// </summary>
    [Property(MaxTest = 200, Arbitrary = [typeof(ValidRingArbitrary)])]
    public bool SimulatedPersistence_IdAndTimestampAreValid(double[][] ring)
    {
        // Simulate what CreateAreaCommandHandler does
        var polygon = GeometryConverter.ToPolygon(ring);
        var entity = new AreaEntity
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            Geometry = polygon
        };

        // Simulate what GetAreaQueryHandler does when reading back
        var geoJson = GeometryConverter.ToGeoJson(entity.Geometry);
        var resultRing = geoJson.Coordinates[0];

        // ID must be non-empty
        if (entity.Id == Guid.Empty) return false;

        // CreatedAt must be a valid (recent) timestamp
        if (entity.CreatedAt == default) return false;
        if (entity.CreatedAt > DateTimeOffset.UtcNow.AddSeconds(1)) return false;

        // Coordinates must match original
        if (resultRing.Length != ring.Length) return false;
        for (var i = 0; i < ring.Length; i++)
        {
            if (Math.Abs(resultRing[i][0] - ring[i][0]) > 1e-10) return false;
            if (Math.Abs(resultRing[i][1] - ring[i][1]) > 1e-10) return false;
        }

        return true;
    }
}
