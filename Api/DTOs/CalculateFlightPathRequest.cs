using DroneMesh3D.Core.FlightPath;

namespace DroneMesh3D.Api.DTOs;

public sealed record CalculateFlightPathRequest(
    Guid AreaId,
    FlightMode Mode,
    GridModeParametersDto? Grid,
    PoiModeParametersDto? Poi);
