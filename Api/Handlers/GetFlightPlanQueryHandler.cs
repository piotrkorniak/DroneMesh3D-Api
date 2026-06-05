using System.Text.Json;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Core.Interfaces;
using MediatR;

namespace DroneMesh3D.Api.Handlers;

public sealed class GetFlightPlanQueryHandler(
    IFlightPlanRepository flightPlanRepository,
    ILogger<GetFlightPlanQueryHandler> logger)
    : IRequestHandler<GetFlightPlanQuery, FlightPlanResponse?>
{
    public async Task<FlightPlanResponse?> Handle(GetFlightPlanQuery query, CancellationToken ct)
    {
        var entity = await flightPlanRepository.GetByIdAsync(query.Id, ct);
        if (entity is null)
        {
            return null;
        }

        List<WaypointDto> waypoints;
        try
        {
            waypoints = JsonSerializer.Deserialize<List<WaypointDto>>(entity.WaypointsJson) ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to deserialize waypoints for flight plan {FlightPlanId}", entity.Id);
            waypoints = [];
        }

        return new FlightPlanResponse(
            entity.Id,
            entity.AreaId,
            entity.Mode.ToString(),
            waypoints,
            new FlightStatisticsDto(
                entity.TotalDistanceM,
                entity.EstimatedFlightTimeS,
                entity.PhotoCount,
                entity.CoveredAreaM2),
            entity.CreatedAt);
    }
}
