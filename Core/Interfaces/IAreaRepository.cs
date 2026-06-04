using DroneMesh3D.Core.Entities;

namespace DroneMesh3D.Core.Interfaces;

public interface IAreaRepository
{
    Task AddAsync(AreaEntity entity, CancellationToken ct = default);
    Task<AreaEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
