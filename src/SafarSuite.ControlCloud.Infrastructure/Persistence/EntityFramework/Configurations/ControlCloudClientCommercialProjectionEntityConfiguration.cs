using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientCommercialProjectionEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientCommercialProjectionEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientCommercialProjectionEntity> builder)
    {
        builder.ToTable("client_commercial_projections");

        builder.HasKey(projection => projection.ClientId);

        builder.Property(projection => projection.ClientId)
            .HasColumnName("client_id")
            .ValueGeneratedNever();
        builder.Property(projection => projection.LastUpdatedAtUtc)
            .HasColumnName("last_updated_at_utc")
            .IsRequired();
        builder.Property(projection => projection.ProjectionJson)
            .HasColumnName("projection_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex(projection => projection.LastUpdatedAtUtc)
            .HasDatabaseName("ix_client_commercial_projections_last_updated_at_utc");
    }
}
