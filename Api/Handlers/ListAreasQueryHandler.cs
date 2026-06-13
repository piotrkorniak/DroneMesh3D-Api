using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Core;
using DroneMesh3D.Core.Interfaces;
using MediatR;

namespace DroneMesh3D.Api.Handlers;

public sealed class ListAreasQueryHandler(IAreaRepository areaRepository)
    : IRequestHandler<ListAreasQuery, List<AreaResponse>>
{
    public async Task<List<AreaResponse>> Handle(ListAreasQuery query, CancellationToken ct)
    {
        var entities = await areaRepository.GetAllAsync(ct);

        return entities
            .Select(entity => new AreaResponse(
                entity.Id,
                entity.CreatedAt,
                GeometryConverter.ToGeoJson(entity.Geometry),
                entity.Name,
                entity.SequentialNumber))
            .ToList();
    }
}
