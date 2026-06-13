using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Api.Queries;
using DroneMesh3D.Core;
using DroneMesh3D.Core.Interfaces;
using MediatR;

namespace DroneMesh3D.Api.Handlers;

public sealed class GetAreaQueryHandler(IAreaRepository areaRepository)
    : IRequestHandler<GetAreaQuery, AreaResponse?>
{
    public async Task<AreaResponse?> Handle(GetAreaQuery query, CancellationToken ct)
    {
        var entity = await areaRepository.GetByIdAsync(query.Id, ct);
        if (entity is null)
        {
            return null;
        }

        return new AreaResponse(
            entity.Id,
            entity.CreatedAt,
            GeometryConverter.ToGeoJson(entity.Geometry),
            entity.Name,
            entity.SequentialNumber);
    }
}
