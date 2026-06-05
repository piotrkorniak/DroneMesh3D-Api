using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.FlightPath;

/// <summary>
///     Computes a grid (lawnmower) flight path over a polygon area.
///     Generates parallel scan lines clipped to the polygon boundary with
///     waypoints distributed at computed photo spacing intervals.
/// </summary>
public sealed class GridFlightPathStrategy
{
    private const double AssumedSpeedMs = 10.0; // 10 m/s assumed drone speed
    private const double DefaultGimbalPitchDegrees = -90.0; // nadir
    private const double MinGimbalPitch = -90.0;
    private const double MaxGimbalPitch = -45.0;

    /// <summary>
    ///     Calculates a grid flight plan for the given polygon and parameters.
    /// </summary>
    /// <param name="area">The polygon area to cover (WGS84, SRID 4326).</param>
    /// <param name="parameters">Grid mode flight parameters.</param>
    /// <returns>A flight plan result with ordered waypoints and statistics.</returns>
    public FlightPlanResult Calculate(Polygon area, GridModeParameters parameters)
    {
        // Step 1: Compute GSD, footprint, photo spacing, and line spacing
        var gsd = GeodesicMathService.ComputeGsd(
            parameters.AltitudeM,
            parameters.Camera.SensorWidthMm,
            parameters.Camera.FocalLengthMm,
            parameters.Camera.ImageWidthPx);

        var (footprintWidthM, footprintHeightM) = GeodesicMathService.ComputePhotoFootprint(
            gsd,
            parameters.Camera.ImageWidthPx,
            parameters.Camera.ImageHeightPx);

        var photoSpacing = GeodesicMathService.ComputePhotoSpacing(
            footprintHeightM,
            parameters.FrontOverlapPercent / 100.0);

        var lineSpacing = GeodesicMathService.ComputeLineSpacing(
            footprintWidthM,
            parameters.SideOverlapPercent / 100.0);

        // Step 2: Determine scan heading
        var heading = DetermineScanHeading(area, parameters.HeadingDegrees);

        // Step 3: Compute gimbal pitch (default nadir, no GimbalPitchDegrees on GridModeParameters)
        var gimbalPitch = DefaultGimbalPitchDegrees;

        // Step 4: Generate scan lines and waypoints
        var waypoints = GenerateGridWaypoints(
            area, heading, lineSpacing, photoSpacing, parameters.AltitudeM, gimbalPitch);

        // Step 5: Compute flight statistics
        var statistics = ComputeStatistics(waypoints, area, parameters.AltitudeM);

        return new FlightPlanResult(waypoints, statistics);
    }

    private static double DetermineScanHeading(Polygon polygon, double? userHeading)
    {
        if (userHeading.HasValue && userHeading.Value >= 0.0 && userHeading.Value <= 360.0)
        {
            return userHeading.Value;
        }

        return GeodesicMathService.LongestAxisHeading(polygon);
    }

    private static List<Waypoint> GenerateGridWaypoints(
        Polygon polygon,
        double headingDegrees,
        double lineSpacing,
        double photoSpacing,
        double altitudeM,
        double gimbalPitch)
    {
        var centroid = polygon.Centroid;
        var centerLat = centroid.Y;
        var centerLon = centroid.X;

        // Compute the width of the polygon perpendicular to heading
        var polygonWidth = ComputePolygonWidthAlongBearing(polygon, headingDegrees + 90.0);
        var polygonLength = ComputePolygonWidthAlongBearing(polygon, headingDegrees);

        // Number of lines needed to cover the polygon width
        var halfWidth = polygonWidth / 2.0;
        var lineCount = (int)Math.Ceiling(polygonWidth / lineSpacing) + 1;

        // The perpendicular bearing (cross-track direction)
        var crossTrackBearing = (headingDegrees + 90.0) % 360.0;

        // Scan line length should extend well beyond polygon to ensure full coverage
        var scanLineHalfLength = polygonLength / 2.0 + lineSpacing;

        var waypoints = new List<Waypoint>();
        var geometryFactory = polygon.Factory ?? new GeometryFactory(new PrecisionModel(), 4326);

        for (var i = 0; i < lineCount; i++)
        {
            // Compute offset from center for this line
            var offset = -halfWidth + i * lineSpacing;

            // Find the line center point by moving perpendicular from polygon centroid
            var (lineCenterLat, lineCenterLon) = GeodesicMathService.DestinationPoint(
                centerLat, centerLon, crossTrackBearing, offset);

            // Create a long scan line at this offset along the heading direction
            var (startLat, startLon) = GeodesicMathService.DestinationPoint(
                lineCenterLat, lineCenterLon, headingDegrees, -scanLineHalfLength);
            var (endLat, endLon) = GeodesicMathService.DestinationPoint(
                lineCenterLat, lineCenterLon, headingDegrees, scanLineHalfLength);

            // Create NTS line geometry
            var lineGeometry = geometryFactory.CreateLineString(new[]
            {
                new Coordinate(startLon, startLat),
                new Coordinate(endLon, endLat)
            });

            // Clip line to polygon boundary
            var clipped = polygon.Intersection(lineGeometry);

            if (clipped.IsEmpty)
            {
                continue;
            }

            // Process each clipped segment
            var segments = ExtractLineSegments(clipped);

            // Serpentine pattern: reverse direction on odd lines
            var reverseDirection = i % 2 == 1;

            foreach (var segment in segments)
            {
                var segmentWaypoints = DistributeWaypointsAlongSegment(
                    segment, photoSpacing, altitudeM, gimbalPitch, headingDegrees, reverseDirection);
                waypoints.AddRange(segmentWaypoints);
            }
        }

        return waypoints;
    }

    /// <summary>
    ///     Computes the approximate width of the polygon along a given bearing direction.
    ///     Projects all polygon vertices onto the bearing axis and returns the span.
    /// </summary>
    private static double ComputePolygonWidthAlongBearing(Polygon polygon, double bearingDegrees)
    {
        var centroid = polygon.Centroid;
        var centerLat = centroid.Y;
        var centerLon = centroid.X;
        var coordinates = polygon.ExteriorRing.Coordinates;

        var minProjection = double.MaxValue;
        var maxProjection = double.MinValue;

        foreach (var coord in coordinates)
        {
            // Distance from centroid to this vertex
            var distance = GeodesicMathService.DistanceBetween(
                centerLat, centerLon, coord.Y, coord.X);

            if (distance < 0.001) // Skip if at centroid
            {
                minProjection = Math.Min(minProjection, 0);
                maxProjection = Math.Max(maxProjection, 0);
                continue;
            }

            // Bearing from centroid to vertex
            var vertexBearing = GeodesicMathService.BearingBetween(
                centerLat, centerLon, coord.Y, coord.X);

            // Angle difference between vertex bearing and desired bearing
            var angleDiff = (vertexBearing - bearingDegrees) * Math.PI / 180.0;

            // Project distance onto the desired bearing axis
            var projection = distance * Math.Cos(angleDiff);

            minProjection = Math.Min(minProjection, projection);
            maxProjection = Math.Max(maxProjection, projection);
        }

        return maxProjection - minProjection;
    }

    /// <summary>
    ///     Extracts line segments from a geometry result of an intersection operation.
    ///     Handles LineString, MultiLineString, and Point/MultiPoint results.
    /// </summary>
    private static List<(Coordinate Start, Coordinate End)> ExtractLineSegments(Geometry geometry)
    {
        var segments = new List<(Coordinate Start, Coordinate End)>();

        switch (geometry)
        {
            case LineString ls when ls.NumPoints >= 2:
                segments.Add((ls.StartPoint.Coordinate, ls.EndPoint.Coordinate));
                break;

            case MultiLineString mls:
                for (var i = 0; i < mls.NumGeometries; i++)
                {
                    var part = (LineString)mls.GetGeometryN(i);
                    if (part.NumPoints >= 2)
                    {
                        segments.Add((part.StartPoint.Coordinate, part.EndPoint.Coordinate));
                    }
                }

                break;

            case GeometryCollection gc:
                for (var i = 0; i < gc.NumGeometries; i++)
                {
                    segments.AddRange(ExtractLineSegments(gc.GetGeometryN(i)));
                }

                break;
        }

        return segments;
    }

    /// <summary>
    ///     Distributes waypoints along a line segment at the computed photo spacing interval.
    /// </summary>
    private static List<Waypoint> DistributeWaypointsAlongSegment(
        (Coordinate Start, Coordinate End) segment,
        double photoSpacing,
        double altitudeM,
        double gimbalPitch,
        double headingDegrees,
        bool reverseDirection)
    {
        var startLat = segment.Start.Y;
        var startLon = segment.Start.X;
        var endLat = segment.End.Y;
        var endLon = segment.End.X;

        if (reverseDirection)
        {
            (startLat, endLat) = (endLat, startLat);
            (startLon, endLon) = (endLon, startLon);
        }

        var segmentDistance = GeodesicMathService.DistanceBetween(startLat, startLon, endLat, endLon);

        if (segmentDistance < photoSpacing * 0.1)
        {
            return [];
        }

        var bearing = GeodesicMathService.BearingBetween(startLat, startLon, endLat, endLon);
        var gimbalYaw = reverseDirection
            ? (headingDegrees + 180.0) % 360.0
            : headingDegrees;

        var waypoints = new List<Waypoint>();
        var traveled = 0.0;

        while (traveled <= segmentDistance)
        {
            var (lat, lon) = GeodesicMathService.DestinationPoint(startLat, startLon, bearing, traveled);

            waypoints.Add(new Waypoint(lat, lon, altitudeM, gimbalPitch, gimbalYaw));

            traveled += photoSpacing;
        }

        // Ensure we have at least the endpoint if no waypoints were added at the end
        if (waypoints.Count > 0)
        {
            var lastWp = waypoints[^1];
            var distToEnd = GeodesicMathService.DistanceBetween(lastWp.Latitude, lastWp.Longitude, endLat, endLon);

            // Add endpoint as turnaround if it's significantly far from last waypoint
            if (distToEnd > photoSpacing * 0.5)
            {
                waypoints.Add(new Waypoint(endLat, endLon, altitudeM, gimbalPitch, gimbalYaw));
            }
        }

        return waypoints;
    }

    private static FlightStatistics ComputeStatistics(
        List<Waypoint> waypoints,
        Polygon polygon,
        double altitudeM)
    {
        if (waypoints.Count == 0)
        {
            return new FlightStatistics(0, 0, 0, 0);
        }

        // Total distance: sum of distances between consecutive waypoints
        var totalDistance = 0.0;
        for (var i = 1; i < waypoints.Count; i++)
        {
            totalDistance += GeodesicMathService.DistanceBetween(
                waypoints[i - 1].Latitude, waypoints[i - 1].Longitude,
                waypoints[i].Latitude, waypoints[i].Longitude);
        }

        // Estimated flight time at assumed speed
        var estimatedTime = totalDistance / AssumedSpeedMs;

        // Photo count = number of waypoints
        var photoCount = waypoints.Count;

        // Covered area: approximate using polygon area in coordinate units
        // For WGS84, approximate with a cos(lat) correction
        var centroidLat = polygon.Centroid.Y;
        var cosLat = Math.Cos(centroidLat * Math.PI / 180.0);
        // 1 degree latitude ≈ 111,320 m, 1 degree longitude ≈ 111,320 * cos(lat) m
        var metersPerDegreeLat = 111_320.0;
        var metersPerDegreeLon = 111_320.0 * cosLat;
        // polygon.Area is in degree² — convert to m²
        var coveredAreaM2 = polygon.Area * metersPerDegreeLat * metersPerDegreeLon;

        return new FlightStatistics(totalDistance, estimatedTime, photoCount, coveredAreaM2);
    }

    /// <summary>
    ///     Clamps gimbal pitch to the valid range [-90, -45].
    ///     Used when a user-provided value needs clamping.
    /// </summary>
    internal static double ClampGimbalPitch(double pitchDegrees) => Math.Clamp(pitchDegrees, MinGimbalPitch, MaxGimbalPitch);
}
