using DroneMesh3D.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DroneMesh3D.Core.Data.Configurations;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> entity)
    {
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
        entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        entity.Property(e => e.GoogleId).HasMaxLength(128);
        entity.HasIndex(e => e.GoogleId).IsUnique();
        entity.Property(e => e.Email).HasMaxLength(256);
        entity.HasIndex(e => e.Email).IsUnique();
        entity.Property(e => e.Name).HasMaxLength(256);
        entity.Property(e => e.AvatarUrl).HasMaxLength(2048);
    }
}
