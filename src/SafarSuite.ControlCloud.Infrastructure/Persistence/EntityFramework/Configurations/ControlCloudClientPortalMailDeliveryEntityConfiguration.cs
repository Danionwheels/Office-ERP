using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientPortalMailDeliveryEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientPortalMailDeliveryEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientPortalMailDeliveryEntity> builder)
    {
        builder.ToTable("client_portal_mail_deliveries");

        builder.HasKey(delivery => delivery.DeliveryId);

        builder.Property(delivery => delivery.DeliveryId)
            .HasColumnName("delivery_id")
            .ValueGeneratedNever();
        builder.Property(delivery => delivery.ClientId)
            .HasColumnName("client_id");
        builder.Property(delivery => delivery.RecipientEmail)
            .HasColumnName("recipient_email")
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(delivery => delivery.RecipientName)
            .HasColumnName("recipient_name")
            .HasMaxLength(180)
            .IsRequired();
        builder.Property(delivery => delivery.Subject)
            .HasColumnName("subject")
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(delivery => delivery.TextBody)
            .HasColumnName("text_body")
            .HasMaxLength(100_000)
            .IsRequired();
        builder.Property(delivery => delivery.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(delivery => delivery.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();
        builder.Property(delivery => delivery.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc")
            .IsRequired();
        builder.Property(delivery => delivery.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
        builder.Property(delivery => delivery.LastAttemptedAtUtc)
            .HasColumnName("last_attempted_at_utc");
        builder.Property(delivery => delivery.SentAtUtc)
            .HasColumnName("sent_at_utc");
        builder.Property(delivery => delivery.FailedAtUtc)
            .HasColumnName("failed_at_utc");
        builder.Property(delivery => delivery.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2_000);
        builder.Property(delivery => delivery.LeaseId)
            .HasColumnName("lease_id");
        builder.Property(delivery => delivery.LeaseExpiresAtUtc)
            .HasColumnName("lease_expires_at_utc");

        builder.HasIndex(delivery => new { delivery.Status, delivery.NextAttemptAtUtc })
            .HasDatabaseName("ix_client_portal_mail_deliveries_status_next_attempt");
        builder.HasIndex(delivery => new { delivery.Status, delivery.LeaseExpiresAtUtc })
            .HasDatabaseName("ix_client_portal_mail_deliveries_status_lease_expiry");
        builder.HasIndex(delivery => delivery.ClientId)
            .HasDatabaseName("ix_client_portal_mail_deliveries_client_id");
    }
}
