using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Core.Repositories;

public sealed class FlightPlanRepository(AppDbContext context) : IFlightPlanRepository
{
    public async Task AddAsync(FlightPlanEntity entity, CancellationToken ct = default)
    {
        context.FlightPlans.Add(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<FlightPlanEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.FlightPlans
            .Include(e => e.Area)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
}
