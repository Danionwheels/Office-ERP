using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.ControlCloud;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class CloudOutboxMessageConfiguration : IEntityTypeConfiguration<CloudOutboxMessage>
{
    public void Configure(EntityTypeBuilder<CloudOutboxMessage> builder)
    {
        builder.ToTable("cloud_outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("cloud_outbox_message_id")
            .HasConversion(
                id => id.Value,
                value => CloudOutboxMessageId.Create(value))
            .ValueGeneratedNever();

        builder.Property(message => message.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                clientId => clientId.HasValue ? clientId.Value.Value : (Guid?)null,
                value => value.HasValue ? ClientId.Create(value.Value) : null);

        builder.Property(message => message.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.SubjectType)
            .HasColumnName("subject_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.SubjectId)
            .HasColumnName("subject_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(message => message.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired();

        builder.Property(message => message.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(message => message.LastAttemptedAtUtc)
            .HasColumnName("last_attempted_at_utc");

        builder.Property(message => message.NextAttemptAtUtc)
            .HasColumnName("next_attempt_at_utc");

        builder.Property(message => message.SentAtUtc)
            .HasColumnName("sent_at_utc");

        builder.Property(message => message.FailedAtUtc)
            .HasColumnName("failed_at_utc");

        builder.Property(message => message.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(CloudOutboxMessage.MaximumFailureReasonLength);

        builder.HasIndex(message => new { message.Status, message.MessageType, message.OccurredAtUtc, message.Id })
            .IsDescending(false, false, true, true)
            .HasDatabaseName("ix_cloud_outbox_messages_status_type_occurred");

        builder.HasIndex(message => new { message.OccurredAtUtc, message.Id })
            .IsDescending()
            .HasDatabaseName("ix_cloud_outbox_messages_occurred");

        builder.HasIndex(message => new { message.ClientId, message.OccurredAtUtc, message.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("ix_cloud_outbox_messages_client_occurred");

        builder.HasIndex(message => new { message.ClientId, message.Status })
            .HasDatabaseName("ix_cloud_outbox_messages_client_status");

        builder.HasIndex(message => new { message.Status, message.NextAttemptAtUtc, message.AttemptCount })
            .HasDatabaseName("ix_cloud_outbox_messages_publish_ready");
    }
}
