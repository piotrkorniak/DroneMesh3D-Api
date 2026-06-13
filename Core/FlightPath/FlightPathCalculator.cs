using DroneMesh3D.Core.Interfaces;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.FlightPath;

/// <summary>
///     Strategy dispatcher that implements IFlightPathCalculator by delegating
///     to the appropriate flight path strategy based on the requested flight mode.
/// </summary>
public sealed class FlightPathCalculator(
    GridFlightPathStrategy gridStrategy,
    PoiFlightPathStrategy poiStrategy) : IFlightPathCalculator
{
    public FlightPlanResult CalculateGrid(Polygon area, GridModeParameters parameters, CancellationToken ct = default)
        => gridStrategy.Calculate(area, parameters, ct);

    public FlightPlanResult CalculatePoi(PoiCalculationRequest request, CancellationToken ct = default)
        => poiStrategy.Calculate(request);
}
