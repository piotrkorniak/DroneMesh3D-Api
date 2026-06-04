using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.Interfaces;

namespace DroneMesh3D.Core.Repositories;

public sealed class AreaRepository(AppDbContext context) : IAreaRepository
{
    public async Task AddAsync(AreaEntity entity, CancellationToken ct = default)
    {
        context.Areas.Add(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<AreaEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Areas.FindAsync([id], ct);
}
