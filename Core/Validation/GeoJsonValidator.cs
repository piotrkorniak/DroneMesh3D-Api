namespace DroneMesh3D.Core.Validation;

/// <summary>
///     Validates the structural correctness of GeoJSON Polygon geometry.
///     Checks: type field equals "Polygon", coordinates are present and non-empty,
///     at least one ring exists, and the outer ring contains coordinate pairs.
/// </summary>
public static class GeoJsonValidator
{
    private const string PolygonType = "Polygon";
    private const int MinCoordinatesPerPoint = 2;

    /// <summary>
    ///     Validates that the given type and coordinates represent a structurally valid GeoJSON Polygon.
    /// </summary>
    /// <param name="type">The GeoJSON geometry type (must be "Polygon").</param>
    /// <param name="coordinates">The coordinate rings array [ring[vertex[lng, lat]]].</param>
    /// <returns>True if the structure is valid; false otherwise.</returns>
    public static bool IsValidPolygon(string? type, double[][]?[]? coordinates)
    {
        if (!IsValidType(type))
            return false;

        if (!HasCoordinates(coordinates))
            return false;

        if (!HasNonEmptyRing(coordinates!))
            return false;

        if (!AllRingsHaveValidCoordinates(coordinates!))
            return false;

        return true;
    }

    private static bool IsValidType(string? type) =>
        string.Equals(type, PolygonType, StringComparison.Ordinal);

    private static bool HasCoordinates(double[][]?[]? coordinates) =>
        coordinates is { Length: > 0 };

    private static bool HasNonEmptyRing(double[][]?[] coordinates) =>
        coordinates[0] is { Length: > 0 };

    private static bool AllRingsHaveValidCoordinates(double[][]?[] coordinates)
    {
        foreach (var ring in coordinates)
        {
            if (ring is null || ring.Length == 0)
                return false;

            foreach (var point in ring)
                if (point is null || point.Length < MinCoordinatesPerPoint)
                    return false;
        }

        return true;
    }
}
