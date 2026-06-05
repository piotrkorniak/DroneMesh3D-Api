using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.FlightPath;
using MediatR;
using OneOf;

namespace DroneMesh3D.Api.Commands;

public record CalculateFlightPathCommand(
    Guid AreaId,
    FlightMode Mode,
    GridModeParameters? Grid,
    PoiModeParameters? Poi)
    : IRequest<OneOf<FlightPlanResponse, ValidationErrorResponse, ErrorResponse>>;
