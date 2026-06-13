using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.FlightPath;
using DroneMesh3D.Core.Interfaces;
using MediatR;
using NetTopologySuite.Geometries;
using OneOf;

namespace DroneMesh3D.Api.Handlers;

public sealed class CalculateFlightPathCommandHandler(
    IAreaRepository areaRepository,
    IFlightPathCalculator flightPathCalculator,
    IFlightPlanRepository flightPlanRepository,
    ILogger<CalculateFlightPathCommandHandler> logger)
    : IRequestHandler<CalculateFlightPathCommand, OneOf<FlightPlanResponse, ValidationErrorResponse, ErrorResponse>>
{
    public async Task<OneOf<FlightPlanResponse, ValidationErrorResponse, ErrorResponse>> Handle(
        CalculateFlightPathCommand command,
        CancellationToken ct)
    {
        // 1. Load AreaEntity by ID
        var area = await areaRepository.GetByIdAsync(command.AreaId, ct);
        if (area is null)
        {
            return new ErrorResponse($"Area with ID '{command.AreaId}' was not found.");
        }

        // 2. Dispatch to IFlightPathCalculator based on mode
        var result = command.Mode switch
        {
            FlightMode.Grid => flightPathCalculator.CalculateGrid(
                (Polygon)area.Geometry, command.Grid!, ct),
            FlightMode.Poi => flightPathCalculator.CalculatePoi(command.Poi!, ct),
            _ => throw new InvalidOperationException($"Unsupported flight mode: {command.Mode}")
        };

        // 3. Create FlightPlanEntity
        var entity = new FlightPlanEntity
        {
            Id = Guid.CreateVersion7(),
            AreaId = command.AreaId,
            Mode = command.Mode,
            GridParameters = command.Mode == FlightMode.Grid ? command.Grid : null,
            PoiParameters = command.Mode == FlightMode.Poi ? command.Poi : null,
            Waypoints = result.Waypoints.ToList(),
            TotalDistanceM = result.Statistics.TotalDistanceM,
            EstimatedFlightTimeS = result.Statistics.EstimatedFlightTimeS,
            PhotoCount = result.Statistics.PhotoCount,
            CoveredAreaM2 = result.Statistics.CoveredAreaM2,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // 4. Persist to database
        try
        {
            await flightPlanRepository.AddAsync(entity, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist flight plan for area {AreaId}", command.AreaId);
            return new ErrorResponse("Failed to save flight plan to the database.");
        }

        // 5. Map to response
        var waypoints = result.Waypoints.Select(w => new WaypointDto(
            w.Latitude,
            w.Longitude,
            w.AltitudeAglM,
            w.GimbalPitchDegrees,
            w.GimbalYawDegrees)).ToList();

        var statistics = new FlightStatisticsDto(
            result.Statistics.TotalDistanceM,
            result.Statistics.EstimatedFlightTimeS,
            result.Statistics.PhotoCount,
            result.Statistics.CoveredAreaM2);

        return new FlightPlanResponse(
            entity.Id,
            entity.AreaId,
            entity.Mode.ToString(),
            waypoints,
            statistics,
            entity.CreatedAt);
    }
}
