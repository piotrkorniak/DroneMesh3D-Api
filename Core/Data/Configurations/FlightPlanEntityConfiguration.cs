using DroneMesh3D.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneMesh3D.Core.Data.Configurations;

public sealed class FlightPlanEntityConfiguration : IEntityTypeConfiguration<FlightPlanEntity>
{
    public void Configure(EntityTypeBuilder<FlightPlanEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.Property(e => e.Mode).HasConversion<string>();
        entity.Property(e => e.ParametersJson).HasColumnType("jsonb");
        entity.Property(e => e.WaypointsJson).HasColumnType("jsonb");
        entity.HasOne(e => e.Area)
            .WithMany()
            .HasForeignKey(e => e.AreaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
