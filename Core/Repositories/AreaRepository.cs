using DroneMesh3D.Core.Data;
using DroneMesh3D.Core.Entities;
using DroneMesh3D.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Core.Repositories;

public sealed class AreaRepository(AppDbContext context) : IAreaRepository
{
    public async Task AddAsync(AreaEntity entity, CancellationToken ct = default)
    {
        context.Areas.Add(entity);
        await context.SaveChangesAsync(ct);
    }

    public async Task<AreaEntity?> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
        => await context.Areas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);

    public async Task<List<AreaEntity>> GetAllAsync(Guid userId, CancellationToken ct = default)
        => await context.Areas
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task UpdateAsync(AreaEntity entity, CancellationToken ct = default)
    {
        context.Areas.Update(entity);
        await context.SaveChangesAsync(ct);
    }
}
