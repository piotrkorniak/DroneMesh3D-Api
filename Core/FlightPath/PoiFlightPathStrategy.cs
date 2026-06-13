namespace DroneMesh3D.Core.FlightPath;

/// <summary>
///     Computes a POI (Point of Interest) orbital flight path around a center point.
///     Generates waypoints equally distributed on a circle at the specified radius and altitude,
///     with gimbal yaw directed toward the center point.
/// </summary>
public sealed class PoiFlightPathStrategy
{
    private const double AssumedSpeedMs = 10.0; // 10 m/s assumed drone speed
    private const double MinGimbalPitch = -90.0;
    private const double MaxGimbalPitch = -45.0;
    private const int MinPhotoCount = 3;

    /// <summary>
    /// <summary>
    ///     Calculates a POI orbital flight plan for the given request.
    /// </summary>
    public FlightPlanResult Calculate(PoiCalculationRequest request)
    {
        var parameters = request.Parameters;

        return request.OrbitShape switch
        {
            OrbitShape.Rectangular when request.AreaCoordinates is { Length: >= 4 }
                => CalculateRectangular(parameters, request.AreaCoordinates),
            OrbitShape.PolygonFollowing when request.AreaCoordinates is { Length: >= 3 }
                => CalculatePolygonFollowing(parameters, request.AreaCoordinates),
            _ => CalculateCircular(parameters)
        };
    }

    private FlightPlanResult CalculateCircular(PoiModeParameters parameters)
    {
        // Step 1: Determine photo count
        var photoCount = DeterminePhotoCount(parameters);

        // Step 2: Distribute waypoints equally on the circle
        var angularStep = 360.0 / photoCount;
        var waypoints = new List<Waypoint>(photoCount);

        for (var i = 0; i < photoCount; i++)
        {
            var bearing = i * angularStep;

            // Compute geographic position from center + radius + bearing
            var (lat, lon) = GeodesicMathService.DestinationPoint(
                parameters.CenterLatitude,
                parameters.CenterLongitude,
                bearing,
                parameters.RadiusM);

            // Compute gimbal yaw: bearing FROM waypoint TO center
            var gimbalYaw = GeodesicMathService.BearingBetween(
                lat, lon,
                parameters.CenterLatitude, parameters.CenterLongitude);

            // Compute gimbal pitch
            var gimbalPitch = ComputeGimbalPitch(parameters);

            waypoints.Add(new Waypoint(lat, lon, parameters.AltitudeM, gimbalPitch, gimbalYaw));
        }

        // Step 3: Compute flight statistics
        var statistics = ComputeStatistics(waypoints, parameters);

        return new FlightPlanResult(waypoints, statistics);
    }

    /// <summary>
    ///     Determines the photo count from explicit parameter or derived from overlap and FOV.
    /// </summary>
    private static int DeterminePhotoCount(PoiModeParameters parameters)
    {
        if (parameters.PhotoCount.HasValue)
        {
            return Math.Max(parameters.PhotoCount.Value, MinPhotoCount);
        }

        // Derive from overlap and camera horizontal FOV
        if (parameters.OverlapPercent.HasValue && parameters.CameraHorizontalFovDegrees.HasValue)
        {
            var horizontalFov = parameters.CameraHorizontalFovDegrees.Value;
            var overlap = parameters.OverlapPercent.Value;

            // stepAngle = HorizontalFOV * (1 - overlap/100)
            var stepAngle = horizontalFov * (1.0 - overlap / 100.0);

            if (stepAngle <= 0)
            {
                return MinPhotoCount;
            }

            var photoCount = (int)Math.Ceiling(360.0 / stepAngle);
            return Math.Max(photoCount, MinPhotoCount);
        }

        // Fallback: minimum valid orbit
        return MinPhotoCount;
    }

    /// <summary>
    ///     Computes the gimbal pitch based on geometry or user-specified value, clamped to [-90, -45].
    /// </summary>
    private static double ComputeGimbalPitch(PoiModeParameters parameters)
    {
        double pitch;

        if (parameters.StructureHeightM.HasValue)
        {
            // pitch = -atan2(altitude - structureHeight, radius) * 180/PI
            var altitudeDiff = parameters.AltitudeM - parameters.StructureHeightM.Value;
            pitch = -Math.Atan2(altitudeDiff, parameters.RadiusM) * (180.0 / Math.PI);
        }
        else
        {
            pitch = parameters.GimbalPitchDegrees;
        }

        // Clamp to [-90, -45]
        return Math.Clamp(pitch, MinGimbalPitch, MaxGimbalPitch);
    }

    /// <summary>
    ///     Computes flight statistics for the POI orbit.
    /// </summary>
    private static FlightStatistics ComputeStatistics(
        List<Waypoint> waypoints, PoiModeParameters parameters)
    {
        if (waypoints.Count == 0)
        {
            return new FlightStatistics(0, 0, 0, 0);
        }

        // Total distance: circumference = 2 * PI * radius (approximate for small circles)
        var totalDistance = 2.0 * Math.PI * parameters.RadiusM;

        // Estimated flight time at assumed speed
        var estimatedFlightTime = totalDistance / AssumedSpeedMs;

        // Photo count
        var photoCount = waypoints.Count;

        // Covered area: approximate as PI * radius²
        var coveredArea = Math.PI * parameters.RadiusM * parameters.RadiusM;

        return new FlightStatistics(totalDistance, estimatedFlightTime, photoCount, coveredArea);
    }

    /// <summary>
    ///     Calculates waypoints distributed along rectangle edges proportional to area bounding box.
    /// </summary>
    private FlightPlanResult CalculateRectangular(PoiModeParameters parameters, double[][] areaCoordinates)
    {
        var photoCount = DeterminePhotoCount(parameters);
        var gimbalPitch = ComputeGimbalPitch(parameters);

        // Compute bounding box of area
        var lats = areaCoordinates.Select(c => c[1]).ToArray();
        var lons = areaCoordinates.Select(c => c[0]).ToArray();
        var minLat = lats.Min();
        var maxLat = lats.Max();
        var minLon = lons.Min();
        var maxLon = lons.Max();

        // Scale bounding box by radius offset from center
        var centerLat = parameters.CenterLatitude;
        var centerLon = parameters.CenterLongitude;
        var latOffset = parameters.RadiusM / 111320.0; // approx meters to degrees
        var lonOffset = parameters.RadiusM / (111320.0 * Math.Cos(centerLat * Math.PI / 180.0));

        // Rectangle corners offset from center
        var corners = new[]
        {
            (centerLat - latOffset, centerLon - lonOffset),
            (centerLat - latOffset, centerLon + lonOffset),
            (centerLat + latOffset, centerLon + lonOffset),
            (centerLat + latOffset, centerLon - lonOffset)
        };

        // Distribute waypoints along rectangle perimeter
        var perimeter = 4.0 * 2.0 * (latOffset + lonOffset) * 111320.0; // approximate
        var waypoints = new List<Waypoint>(photoCount);

        for (var i = 0; i < photoCount; i++)
        {
            var t = (double)i / photoCount;
            var perimPos = t * 4.0; // 0-4 maps to 4 edges
            var edge = (int)perimPos;
            var edgeT = perimPos - edge;
            edge %= 4;

            var (lat1, lon1) = corners[edge];
            var (lat2, lon2) = corners[(edge + 1) % 4];
            var lat = lat1 + edgeT * (lat2 - lat1);
            var lon = lon1 + edgeT * (lon2 - lon1);

            var gimbalYaw = GeodesicMathService.BearingBetween(lat, lon, centerLat, centerLon);
            waypoints.Add(new Waypoint(lat, lon, parameters.AltitudeM, gimbalPitch, gimbalYaw));
        }

        var statistics = ComputeStatistics(waypoints, parameters);
        return new FlightPlanResult(waypoints, statistics);
    }

    /// <summary>
    ///     Calculates waypoints along an offset polygon boundary at the configured radius.
    /// </summary>
    private FlightPlanResult CalculatePolygonFollowing(PoiModeParameters parameters, double[][] areaCoordinates)
    {
        var photoCount = DeterminePhotoCount(parameters);
        var gimbalPitch = ComputeGimbalPitch(parameters);
        var centerLat = parameters.CenterLatitude;
        var centerLon = parameters.CenterLongitude;

        // Compute total perimeter of area polygon
        var totalPerimeter = 0.0;
        var edgeLengths = new double[areaCoordinates.Length - 1];
        for (var i = 0; i < areaCoordinates.Length - 1; i++)
        {
            edgeLengths[i] = GeodesicMathService.DistanceBetween(
                areaCoordinates[i][1], areaCoordinates[i][0],
                areaCoordinates[i + 1][1], areaCoordinates[i + 1][0]);
            totalPerimeter += edgeLengths[i];
        }

        if (totalPerimeter <= 0)
        {
            return CalculateCircular(parameters);
        }

        // Distribute waypoints equally along perimeter, offset outward by radius
        var waypoints = new List<Waypoint>(photoCount);
        var spacing = totalPerimeter / photoCount;

        for (var i = 0; i < photoCount; i++)
        {
            var targetDist = i * spacing;
            var accumulated = 0.0;
            var segIdx = 0;

            while (segIdx < edgeLengths.Length - 1 && accumulated + edgeLengths[segIdx] < targetDist)
            {
                accumulated += edgeLengths[segIdx];
                segIdx++;
            }

            var segT = (targetDist - accumulated) / edgeLengths[segIdx];
            var baseLat = areaCoordinates[segIdx][1] + segT * (areaCoordinates[segIdx + 1][1] - areaCoordinates[segIdx][1]);
            var baseLon = areaCoordinates[segIdx][0] + segT * (areaCoordinates[segIdx + 1][0] - areaCoordinates[segIdx][0]);

            // Offset outward from center by radius
            var bearingFromCenter = GeodesicMathService.BearingBetween(centerLat, centerLon, baseLat, baseLon);
            var (lat, lon) = GeodesicMathService.DestinationPoint(centerLat, centerLon, bearingFromCenter, parameters.RadiusM);

            var gimbalYaw = GeodesicMathService.BearingBetween(lat, lon, centerLat, centerLon);
            waypoints.Add(new Waypoint(lat, lon, parameters.AltitudeM, gimbalPitch, gimbalYaw));
        }

        var statistics = ComputeStatistics(waypoints, parameters);
        return new FlightPlanResult(waypoints, statistics);
    }
}
