using DroneMesh3D.Core.Models;

namespace DroneMesh3D.Api.DTOs;

public record AreaResponse(Guid Id, DateTimeOffset CreatedAt, GeoJsonGeometry Geometry, string? Name, int SequentialNumber);
