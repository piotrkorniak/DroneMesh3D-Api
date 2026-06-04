using DroneMesh3D.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DroneMesh3D.Core.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AreaEntity> Areas => Set<AreaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isNpgsql = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";

        if (isNpgsql) modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<AreaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            if (isNpgsql)
            {
                entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
                entity.Property(e => e.Geometry).HasColumnType("geometry(Polygon, 4326)");
                entity.HasIndex(e => e.Geometry).HasMethod("gist");
            }
            else
            {
                // SQLite with NTS: use generic GEOMETRY type (most permissive)
                entity.Property(e => e.Geometry)
                    .IsRequired()
                    .HasColumnType("GEOMETRY");
            }
        });
    }
}
