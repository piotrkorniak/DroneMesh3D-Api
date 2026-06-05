using DroneMesh3D.Core.FlightPath;
using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.Interfaces;

public interface IFlightPathCalculator
{
    FlightPlanResult CalculateGrid(Polygon area, GridModeParameters parameters);
    FlightPlanResult CalculatePoi(PoiModeParameters parameters);
}
