using DroneMesh3D.Api.Commands;
using DroneMesh3D.Api.DTOs;
using DroneMesh3D.Core;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.Interfaces;
using DroneMesh3D.Core.Models;
using DroneMesh3D.Core.Validation;
using MediatR;
using OneOf;

namespace DroneMesh3D.Api.Handlers;

public sealed class CreateAreaCommandHandler(
    IAreaValidator areaValidator,
    IAreaRepository areaRepository)
    : IRequestHandler<CreateAreaCommand, OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>>
{
    public async Task<OneOf<AreaResponse, ValidationErrorResponse, ErrorResponse>> Handle(
        CreateAreaCommand command,
        CancellationToken ct)
    {
        // 1. Validate GeoJSON structure
        if (!GeoJsonValidator.IsValidPolygon(command.Type, command.Coordinates))
        {
            return new ErrorResponse("Invalid GeoJSON Polygon geometry.");
        }

        // 2. Reject multi-ring polygons (holes not supported)
        if (command.Coordinates.Length > 1)
        {
            return new ErrorResponse("Multi-ring polygons (holes) are not supported.");
        }

        // 3. Validate geometry rules
        var outerRing = command.Coordinates[0];
        var validationResult = areaValidator.Validate(outerRing);
        if (!validationResult.IsValid)
        {
            return new ValidationErrorResponse(validationResult.Errors);
        }

        // 4. Convert to NTS polygon
        var geometry = GeometryConverter.ToPolygon(outerRing);

        // 5. Normalize name (trim, nullify whitespace-only)
        var name = string.IsNullOrWhiteSpace(command.Name) ? null : command.Name.Trim();
        if (name?.Length > 50) name = name[..50];

        // 6. Create and persist entity (SequentialNumber assigned by DB identity column)
        var entity = new AreaEntity
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = DateTimeOffset.UtcNow,
            Geometry = geometry,
            Name = name
        };

        await areaRepository.AddAsync(entity, ct);

        // 7. Return response
        return new AreaResponse(
            entity.Id,
            entity.CreatedAt,
            new GeoJsonGeometry(GeoJsonType.Polygon, command.Coordinates),
            entity.Name,
            entity.SequentialNumber);
    }
}
