using DroneMesh3D.Core.FlightPath;

namespace DroneMesh3D.Core.Entities;

public sealed class FlightPlanEntity
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public FlightMode Mode { get; set; }
    public string ParametersJson { get; set; } = string.Empty;
    public string WaypointsJson { get; set; } = string.Empty;
    public double TotalDistanceM { get; set; }
    public double EstimatedFlightTimeS { get; set; }
    public int PhotoCount { get; set; }
    public double CoveredAreaM2 { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public AreaEntity Area { get; set; } = null!;
}
