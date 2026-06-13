using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Core.Interfaces;
using MediatR;

namespace DroneMesh3D.Api.Handlers;

public sealed class GetFlightPlanQueryHandler(
    IFlightPlanRepository flightPlanRepository)
    : IRequestHandler<GetFlightPlanQuery, FlightPlanResponse?>
{
    public async Task<FlightPlanResponse?> Handle(GetFlightPlanQuery query, CancellationToken ct)
    {
        var entity = await flightPlanRepository.GetByIdAsync(query.Id, query.UserId, ct);
        if (entity is null)
        {
            return null;
        }

        var waypoints = entity.Waypoints
            .Select(w => new WaypointDto(w.Latitude, w.Longitude, w.AltitudeAglM, w.GimbalPitchDegrees, w.GimbalYawDegrees))
            .ToList();

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
