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
        builder.Property(projection => projection.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(projection => projection.TotalInvoiced)
            .HasColumnName("total_invoiced")
            .HasPrecision(18, 2);
        builder.Property(projection => projection.TotalPaid)
            .HasColumnName("total_paid")
            .HasPrecision(18, 2);
        builder.Property(projection => projection.TotalCredited)
            .HasColumnName("total_credited")
            .HasPrecision(18, 2);
        builder.Property(projection => projection.TotalRefunded)
            .HasColumnName("total_refunded")
            .HasPrecision(18, 2);
        builder.Property(projection => projection.TotalCreditApplied)
            .HasColumnName("total_credit_applied")
            .HasPrecision(18, 2);
        builder.Property(projection => projection.BalanceDue)
            .HasColumnName("balance_due")
            .HasPrecision(18, 2);
        builder.Property(projection => projection.AvailableCredit)
            .HasColumnName("available_credit")
            .HasPrecision(18, 2);
        builder.Property(projection => projection.IsPaid)
            .HasColumnName("is_paid")
            .IsRequired();
        builder.Property(projection => projection.LastUpdatedAtUtc)
            .HasColumnName("last_updated_at_utc")
            .IsRequired();
        builder.Property(projection => projection.LatestEntitlementJson)
            .HasColumnName("latest_entitlement_json")
            .HasColumnType("jsonb");

        builder.HasIndex(projection => projection.LastUpdatedAtUtc)
            .HasDatabaseName("ix_client_commercial_projections_last_updated_at_utc");
    }
}
