using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Api.Services;
using DroneMesh3D.Core.Interfaces;
using MediatR;

namespace DroneMesh3D.Api.Handlers;

public sealed class ListFlightPlansQueryHandler(
    IFlightPlanRepository flightPlanRepository,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<ListFlightPlansQuery, List<FlightPlanResponse>>
{
    public async Task<List<FlightPlanResponse>> Handle(ListFlightPlansQuery request, CancellationToken ct)
    {
        var entities = await flightPlanRepository.ListAsync(request.AreaId, currentUser.UserId, request.Limit, request.Offset, ct);

        return entities.Select(entity =>
        {
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
        }).ToList();
    }
}
