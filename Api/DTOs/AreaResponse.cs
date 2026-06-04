namespace DroneMesh3D.Api.DTOs;

using DroneMesh3D.Core.Models;

public record AreaResponse(Guid Id, DateTimeOffset CreatedAt, GeoJsonGeometry Geometry);
