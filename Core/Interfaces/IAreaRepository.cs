namespace DroneMesh3D.Core.Interfaces;

using DroneMesh3D.Core.Entities;

public interface IAreaRepository
{
    Task AddAsync(AreaEntity entity, CancellationToken ct = default);
    Task<AreaEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
