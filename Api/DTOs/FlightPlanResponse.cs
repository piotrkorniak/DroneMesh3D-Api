namespace DroneMesh3D.Api.DTOs;

public sealed record FlightPlanResponse(
    Guid Id,
    Guid AreaId,
    string Mode,
    List<WaypointDto> Waypoints,
    FlightStatisticsDto Statistics,
    DateTimeOffset CreatedAt);

public sealed record WaypointDto(
    double Latitude,
    double Longitude,
    double AltitudeAglM,
    double GimbalPitchDegrees,
    double GimbalYawDegrees);

public sealed record FlightStatisticsDto(
    double TotalDistanceM,
    double EstimatedFlightTimeS,
    int PhotoCount,
    double CoveredAreaM2);
