using DroneMesh3D.Core.Entities;

namespace DroneMesh3D.Core.Interfaces;

public interface IFlightPlanRepository
{
    Task AddAsync(FlightPlanEntity entity, CancellationToken ct = default);
    Task<FlightPlanEntity?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
    Task<List<FlightPlanEntity>> ListAsync(Guid? areaId, Guid userId, int limit, int offset, CancellationToken ct = default);
}
