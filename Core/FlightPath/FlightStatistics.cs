namespace DroneMesh3D.Core.FlightPath;

public sealed record FlightStatistics(
    double TotalDistanceM,
    double EstimatedFlightTimeS,
    int PhotoCount,
    double CoveredAreaM2);
