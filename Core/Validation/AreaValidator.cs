using DroneMesh3D.Core.Interfaces;

namespace DroneMesh3D.Core.Validation;

/// <summary>
///     Validates geometric properties of a polygon ring.
///     Rules mirror the frontend PolygonValidatorService:
///     ≥3 distinct vertices, closure, no self-intersections, area in [100 m², 50 000 m²].
/// </summary>
public sealed class AreaValidator : IAreaValidator
{
    private const double MaxAreaHectares = 5.0;
    private const double MinAreaSqm = 100.0;
    private const double EarthRadius = 6371008.8;

    public ValidationResult Validate(double[][] ring)
    {
        var errors = new List<string>();

        if (!HasMinimumVertices(ring))
        {
            errors.Add("Polygon must have at least 3 vertices.");
        }

        if (!IsClosed(ring))
        {
            errors.Add("Polygon must be closed.");
        }

        if (HasSelfIntersection(ring))
        {
            errors.Add("Polygon must not self-intersect.");
        }

        var areaSqm = CalculateAreaSqm(ring);

        if (areaSqm > MaxAreaHectares * 10000)
        {
            errors.Add($"Polygon area exceeds maximum of {MaxAreaHectares} hectares.");
        }

        if (areaSqm < MinAreaSqm)
        {
            errors.Add($"Polygon area is below minimum of {MinAreaSqm} square meters.");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    public bool HasMinimumVertices(double[][] ring)
    {
        var distinctCount = IsClosed(ring) ? ring.Length - 1 : ring.Length;
        return distinctCount >= 3;
    }

    public bool IsClosed(double[][] ring)
    {
        if (ring.Length < 2)
        {
            return false;
        }

        var first = ring[0];
        var last = ring[^1];
        return first[0] == last[0] && first[1] == last[1];
    }

    public bool HasSelfIntersection(double[][] ring)
    {
        var n = ring.Length;
        if (n < 4)
        {
            return false;
        }

        var edgeCount = n - 1;

        for (var i = 0; i < edgeCount; i++)
        {
            for (var j = i + 2; j < edgeCount; j++)
            {
                // Skip adjacent edges (first and last edge are adjacent in a closed polygon)
                if (i == 0 && j == edgeCount - 1 && IsClosed(ring))
                {
                    continue;
                }

                if (SegmentsIntersect(ring[i], ring[i + 1], ring[j], ring[j + 1]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public double CalculateAreaSqm(double[][] ring)
    {
        if (ring.Length < 3)
        {
            return 0;
        }

        // Get the ring without the closing point if it's closed
        var points = IsClosed(ring) ? ring[..^1] : ring;

        if (points.Length < 3)
        {
            return 0;
        }

        return ComputeSphericalArea(points);
    }

    private static double ComputeSphericalArea(double[][] ring)
    {
        // Spherical polygon area: A = R² * |Σ (λ_{i+1} - λ_{i-1}) * sin(φ_i)| / 2
        var n = ring.Length;
        var sum = 0.0;

        for (var i = 0; i < n; i++)
        {
            var prev = (i + n - 1) % n;
            var next = (i + 1) % n;

            var lonPrev = ToRadians(ring[prev][0]);
            var lonNext = ToRadians(ring[next][0]);
            var latCurr = ToRadians(ring[i][1]);

            sum += (lonNext - lonPrev) * Math.Sin(latCurr);
        }

        return Math.Abs(sum * EarthRadius * EarthRadius * 0.5);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    ///     Checks if two line segments (p1-p2) and (p3-p4) properly intersect.
    /// </summary>
    private static bool SegmentsIntersect(double[] p1, double[] p2, double[] p3, double[] p4)
    {
        var d1 = CrossProduct(p3, p4, p1);
        var d2 = CrossProduct(p3, p4, p2);
        var d3 = CrossProduct(p1, p2, p3);
        var d4 = CrossProduct(p1, p2, p4);

        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) &&
            ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0)))
        {
            return true;
        }

        // Check collinear cases
        if (d1 == 0 && OnSegment(p3, p4, p1))
        {
            return true;
        }

        if (d2 == 0 && OnSegment(p3, p4, p2))
        {
            return true;
        }

        if (d3 == 0 && OnSegment(p1, p2, p3))
        {
            return true;
        }

        if (d4 == 0 && OnSegment(p1, p2, p4))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Computes the cross product of vectors (b - a) and (c - a).
    /// </summary>
    private static double CrossProduct(double[] a, double[] b, double[] c) =>
        (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);

    /// <summary>
    ///     Checks if point p lies on segment (a, b), assuming collinearity.
    /// </summary>
    private static bool OnSegment(double[] a, double[] b, double[] p) =>
        Math.Min(a[0], b[0]) <= p[0] &&
        p[0] <= Math.Max(a[0], b[0]) &&
        Math.Min(a[1], b[1]) <= p[1] &&
        p[1] <= Math.Max(a[1], b[1]);
}
