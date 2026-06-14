using DroneMesh3D.Core.Entities;

namespace DroneMesh3D.Core.Interfaces;

public interface IAreaRepository
{
    Task AddAsync(AreaEntity entity, CancellationToken ct = default);
    Task<AreaEntity?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<List<AreaEntity>> GetAllAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAsync(AreaEntity entity, CancellationToken ct = default);
}
