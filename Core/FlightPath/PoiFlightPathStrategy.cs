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
    ///     Calculates a POI orbital flight plan for the given parameters.
    /// </summary>
    /// <param name="parameters">POI mode flight parameters.</param>
    /// <returns>A flight plan result with ordered waypoints and statistics.</returns>
    public FlightPlanResult Calculate(PoiModeParameters parameters)
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
}
