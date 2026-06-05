namespace DroneMesh3D.Core.FlightPath;

public sealed record FlightPlanResult(
    IReadOnlyList<Waypoint> Waypoints,
    FlightStatistics Statistics);
