using NetTopologySuite.Geometries;

namespace DroneMesh3D.Core.Entities;

public sealed class AreaEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public required Geometry Geometry { get; set; }
}
