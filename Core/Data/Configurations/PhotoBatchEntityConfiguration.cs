using DroneMesh3D.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneMesh3D.Core.Data.Configurations;

public sealed class PhotoBatchEntityConfiguration : IEntityTypeConfiguration<PhotoBatchEntity>
{
    public void Configure(EntityTypeBuilder<PhotoBatchEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.UploadedAt).HasDefaultValueSql("now()");
        entity.Property(e => e.BucketPath).HasMaxLength(512);

        entity.HasOne(e => e.FlightPlan)
            .WithMany()
            .HasForeignKey(e => e.FlightPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
