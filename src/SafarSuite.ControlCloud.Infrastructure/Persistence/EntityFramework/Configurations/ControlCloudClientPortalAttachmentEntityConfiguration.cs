using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientPortalAttachmentEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientPortalAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientPortalAttachmentEntity> builder)
    {
        builder.ToTable("client_portal_attachments");

        builder.HasKey(attachment => attachment.AttachmentId);

        builder.Property(attachment => attachment.AttachmentId)
            .HasColumnName("attachment_id")
            .ValueGeneratedNever();
        builder.Property(attachment => attachment.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(attachment => attachment.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .IsRequired();
        builder.Property(attachment => attachment.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(255)
            .IsRequired();
        builder.Property(attachment => attachment.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(attachment => attachment.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();
        builder.Property(attachment => attachment.Sha256)
            .HasColumnName("sha256")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(attachment => attachment.Content)
            .HasColumnName("content")
            .HasColumnType("bytea")
            .IsRequired();
        builder.Property(attachment => attachment.UploadedAtUtc)
            .HasColumnName("uploaded_at_utc")
            .IsRequired();

        builder.HasIndex(attachment => new { attachment.ClientId, attachment.UploadedAtUtc })
            .HasDatabaseName("ix_client_portal_attachments_client_uploaded");
    }
}
