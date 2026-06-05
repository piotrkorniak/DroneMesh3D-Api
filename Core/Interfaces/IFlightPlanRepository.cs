using DroneMesh3D.Core.Entities;

namespace DroneMesh3D.Core.Interfaces;

public interface IFlightPlanRepository
{
    Task AddAsync(FlightPlanEntity entity, CancellationToken ct = default);
    Task<FlightPlanEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
