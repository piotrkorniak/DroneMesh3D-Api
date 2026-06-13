using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core;
using DroneMesh3D.Core.Interfaces;
using DroneMesh3D.Core.Validation;
using MediatR;

namespace DroneMesh3D.Api.Handlers;

public sealed class UpdateAreaNameCommandHandler(IAreaRepository areaRepository)
    : IRequestHandler<UpdateAreaNameCommand, AreaResponse?>
{
    public async Task<AreaResponse?> Handle(UpdateAreaNameCommand command, CancellationToken ct)
    {
        var entity = await areaRepository.GetByIdAsync(command.Id, ct);
        if (entity is null) return null;

        entity.Name = AreaNameValidator.NormalizeAndValidate(command.Name);
        await areaRepository.UpdateAsync(entity, ct);

        return new AreaResponse(
            entity.Id,
            entity.CreatedAt,
            GeometryConverter.ToGeoJson(entity.Geometry),
            entity.Name,
            entity.SequentialNumber);
    }
}
