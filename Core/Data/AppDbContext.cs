using DroneMesh3D.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Core.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AreaEntity> Areas => Set<AreaEntity>();
    public DbSet<FlightPlanEntity> FlightPlans => Set<FlightPlanEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
