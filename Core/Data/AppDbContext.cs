namespace DroneMesh3D.Core.Data;

using Microsoft.EntityFrameworkCore;
using DroneMesh3D.Core.Entities;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AreaEntity> Areas => Set<AreaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<AreaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(e => e.Geometry).HasColumnType("geometry(Polygon, 4326)");
            entity.HasIndex(e => e.Geometry).HasMethod("gist");
        });
    }
}
