namespace DroneMesh3D.Core.FlightPath;

public sealed record Waypoint(
    double Latitude,
    double Longitude,
    double AltitudeAglM,
    double GimbalPitchDegrees,
    double GimbalYawDegrees);
