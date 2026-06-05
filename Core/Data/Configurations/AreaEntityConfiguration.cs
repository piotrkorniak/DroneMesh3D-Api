using DroneMesh3D.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneMesh3D.Core.Data.Configurations;

public sealed class AreaEntityConfiguration : IEntityTypeConfiguration<AreaEntity>
{
    public void Configure(EntityTypeBuilder<AreaEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.Property(e => e.Geometry).HasColumnType("geometry(Polygon, 4326)");
        entity.HasIndex(e => e.Geometry).HasMethod("gist");
    }
}
