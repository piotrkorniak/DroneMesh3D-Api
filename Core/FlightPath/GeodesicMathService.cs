using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.FlightPath;

/// <summary>
///     Pure static utility functions for geodesic calculations, GSD computation,
///     and polygon axis analysis. All angle inputs/outputs are in degrees.
/// </summary>
public static class GeodesicMathService
{
    private const double EarthRadiusM = 6_371_000.0;
    private const double DegToRad = Math.PI / 180.0;
    private const double RadToDeg = 180.0 / Math.PI;

    /// <summary>
    ///     Computes the destination point given a start point, bearing, and distance
    ///     using the Haversine formula on a spherical Earth model.
    /// </summary>
    /// <param name="lat">Start latitude in degrees.</param>
    /// <param name="lon">Start longitude in degrees.</param>
    /// <param name="bearingDeg">Bearing in degrees [0, 360).</param>
    /// <param name="distanceM">Distance in meters.</param>
    /// <returns>Tuple of (latitude, longitude) in degrees.</returns>
    public static (double Latitude, double Longitude) DestinationPoint(
        double lat, double lon, double bearingDeg, double distanceM)
    {
        var lat1 = lat * DegToRad;
        var lon1 = lon * DegToRad;
        var brng = bearingDeg * DegToRad;
        var angularDistance = distanceM / EarthRadiusM;

        var sinLat1 = Math.Sin(lat1);
        var cosLat1 = Math.Cos(lat1);
        var sinAngDist = Math.Sin(angularDistance);
        var cosAngDist = Math.Cos(angularDistance);

        var lat2 = Math.Asin(
            sinLat1 * cosAngDist + cosLat1 * sinAngDist * Math.Cos(brng));

        var lon2 = lon1 + Math.Atan2(
            Math.Sin(brng) * sinAngDist * cosLat1,
            cosAngDist - sinLat1 * Math.Sin(lat2));

        // Normalize longitude to [-180, 180]
        var lonDeg = lon2 * RadToDeg;
        lonDeg = (lonDeg + 540) % 360 - 180;

        return (lat2 * RadToDeg, lonDeg);
    }

    /// <summary>
    ///     Computes the initial bearing (forward azimuth) from point 1 to point 2.
    /// </summary>
    /// <returns>Bearing in degrees [0, 360).</returns>
    public static double BearingBetween(
        double lat1, double lon1, double lat2, double lon2)
    {
        var phi1 = lat1 * DegToRad;
        var phi2 = lat2 * DegToRad;
        var deltaLambda = (lon2 - lon1) * DegToRad;

        var y = Math.Sin(deltaLambda) * Math.Cos(phi2);
        var x = Math.Cos(phi1) * Math.Sin(phi2) -
                Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(deltaLambda);

        var theta = Math.Atan2(y, x);

        return (theta * RadToDeg + 360) % 360;
    }

    /// <summary>
    ///     Computes the great-circle distance between two points using the Haversine formula.
    /// </summary>
    /// <returns>Distance in meters.</returns>
    public static double DistanceBetween(
        double lat1, double lon1, double lat2, double lon2)
    {
        var phi1 = lat1 * DegToRad;
        var phi2 = lat2 * DegToRad;
        var deltaPhi = (lat2 - lat1) * DegToRad;
        var deltaLambda = (lon2 - lon1) * DegToRad;

        var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                Math.Cos(phi1) * Math.Cos(phi2) *
                Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusM * c;
    }

    /// <summary>
    ///     Computes Ground Sample Distance (GSD) in meters per pixel.
    ///     Formula: GSD = (altitude × sensorWidth) / (focalLength × imageWidth)
    /// </summary>
    /// <param name="altitudeM">Flight altitude in meters.</param>
    /// <param name="sensorWidthMm">Camera sensor width in millimeters.</param>
    /// <param name="focalLengthMm">Camera focal length in millimeters.</param>
    /// <param name="imageWidthPx">Image width in pixels.</param>
    /// <returns>GSD in meters per pixel.</returns>
    public static double ComputeGsd(
        double altitudeM, double sensorWidthMm, double focalLengthMm, int imageWidthPx) =>
        altitudeM * sensorWidthMm / (focalLengthMm * imageWidthPx);

    /// <summary>
    ///     Computes the photo footprint on the ground.
    /// </summary>
    /// <param name="gsd">Ground Sample Distance in meters per pixel.</param>
    /// <param name="imageWidthPx">Image width in pixels.</param>
    /// <param name="imageHeightPx">Image height in pixels.</param>
    /// <returns>Tuple of (widthM, heightM) representing ground coverage of one photo.</returns>
    public static (double WidthM, double HeightM) ComputePhotoFootprint(
        double gsd, int imageWidthPx, int imageHeightPx) =>
        (gsd * imageWidthPx, gsd * imageHeightPx);

    /// <summary>
    ///     Computes the spacing between consecutive photos along the flight line.
    ///     Formula: footprintHeight × (1 - frontOverlap)
    /// </summary>
    /// <param name="footprintHeightM">Photo footprint height in meters (along-track).</param>
    /// <param name="frontOverlap">Front overlap as a decimal (e.g., 0.78 for 78%).</param>
    /// <returns>Photo spacing in meters.</returns>
    public static double ComputePhotoSpacing(double footprintHeightM, double frontOverlap) => footprintHeightM * (1.0 - frontOverlap);

    /// <summary>
    ///     Computes the spacing between adjacent flight lines.
    ///     Formula: footprintWidth × (1 - sideOverlap)
    /// </summary>
    /// <param name="footprintWidthM">Photo footprint width in meters (cross-track).</param>
    /// <param name="sideOverlap">Side overlap as a decimal (e.g., 0.70 for 70%).</param>
    /// <returns>Line spacing in meters.</returns>
    public static double ComputeLineSpacing(double footprintWidthM, double sideOverlap) => footprintWidthM * (1.0 - sideOverlap);

    /// <summary>
    ///     Determines the heading (in degrees) of the longest axis of the polygon's
    ///     oriented minimum bounding rectangle.
    /// </summary>
    /// <param name="polygon">A NetTopologySuite Polygon geometry.</param>
    /// <returns>Heading in degrees [0, 360) aligned with the longest side of the OMBR.</returns>
    public static double LongestAxisHeading(Polygon polygon)
    {
        var minimumDiameter = new MinimumDiameter(polygon);
        var ombr = minimumDiameter.GetMinimumRectangle();

        var coordinates = ombr.Coordinates;

        // The minimum rectangle has 5 coordinates (closed ring).
        // Find the longest side among the first 4 edges.
        var maxLength = 0.0;
        var longestEdgeStart = coordinates[0];
        var longestEdgeEnd = coordinates[1];

        for (var i = 0; i < 4; i++)
        {
            var start = coordinates[i];
            var end = coordinates[i + 1];
            var length = start.Distance(end);

            if (length > maxLength)
            {
                maxLength = length;
                longestEdgeStart = start;
                longestEdgeEnd = end;
            }
        }

        // Compute bearing of the longest edge using geographic coordinates
        var heading = BearingBetween(
            longestEdgeStart.Y, longestEdgeStart.X,
            longestEdgeEnd.Y, longestEdgeEnd.X);

        // Normalize to [0, 360)
        return heading % 360;
    }
}
