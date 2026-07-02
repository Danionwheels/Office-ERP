using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlDeskEnvelopeReceiptEntityConfiguration
    : IEntityTypeConfiguration<ControlDeskEnvelopeReceiptEntity>
{
    public void Configure(EntityTypeBuilder<ControlDeskEnvelopeReceiptEntity> builder)
    {
        builder.ToTable("control_desk_envelope_receipts");

        builder.HasKey(receipt => receipt.ReceiptId);

        builder.Property(receipt => receipt.ReceiptId)
            .HasColumnName("receipt_id")
            .ValueGeneratedNever();
        builder.Property(receipt => receipt.MessageId)
            .HasColumnName("message_id")
            .IsRequired();
        builder.Property(receipt => receipt.MessageType)
            .HasColumnName("message_type")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(receipt => receipt.SubjectType)
            .HasColumnName("subject_type")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(receipt => receipt.SubjectId)
            .HasColumnName("subject_id")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(receipt => receipt.SourceSystem)
            .HasColumnName("source_system")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(receipt => receipt.SourceEnvironment)
            .HasColumnName("source_environment")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(receipt => receipt.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(240)
            .IsRequired();
        builder.Property(receipt => receipt.SignatureKeyId)
            .HasColumnName("signature_key_id")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(receipt => receipt.SignatureValue)
            .HasColumnName("signature_value")
            .HasMaxLength(512)
            .IsRequired();
        builder.Property(receipt => receipt.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(receipt => receipt.CloudReference)
            .HasColumnName("cloud_reference")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(receipt => receipt.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();
        builder.Property(receipt => receipt.PreparedAtUtc)
            .HasColumnName("prepared_at_utc")
            .IsRequired();
        builder.Property(receipt => receipt.ReceivedAtUtc)
            .HasColumnName("received_at_utc")
            .IsRequired();
        builder.Property(receipt => receipt.Detail)
            .HasColumnName("detail")
            .HasMaxLength(1000);

        builder.HasIndex(receipt => receipt.IdempotencyKey)
            .IsUnique()
            .HasFilter("status = 'Accepted'")
            .HasDatabaseName("ux_control_desk_envelope_receipts_accepted_idempotency_key");
        builder.HasIndex(receipt => new { receipt.Status, receipt.IdempotencyKey })
            .HasDatabaseName("ix_control_desk_envelope_receipts_status_idempotency_key");
        builder.HasIndex(receipt => receipt.MessageId)
            .HasDatabaseName("ix_control_desk_envelope_receipts_message_id");
    }
}
