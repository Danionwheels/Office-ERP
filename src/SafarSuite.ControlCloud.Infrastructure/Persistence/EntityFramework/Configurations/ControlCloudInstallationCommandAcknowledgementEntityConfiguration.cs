using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudInstallationCommandAcknowledgementEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudInstallationCommandAcknowledgementEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudInstallationCommandAcknowledgementEntity> builder)
    {
        builder.ToTable("installation_command_acknowledgements");

        builder.HasKey(acknowledgement => acknowledgement.AcknowledgementId);

        builder.Property(acknowledgement => acknowledgement.AcknowledgementId)
            .HasColumnName("acknowledgement_id")
            .ValueGeneratedNever();
        builder.Property(acknowledgement => acknowledgement.CommandId)
            .HasColumnName("command_id")
            .IsRequired();
        builder.Property(acknowledgement => acknowledgement.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(acknowledgement => acknowledgement.InstallationId)
            .HasColumnName("installation_id")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(acknowledgement => acknowledgement.CommandVersion)
            .HasColumnName("command_version")
            .IsRequired();
        builder.Property(acknowledgement => acknowledgement.ResultStatus)
            .HasColumnName("result_status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(acknowledgement => acknowledgement.Detail)
            .HasColumnName("detail")
            .HasMaxLength(1000);
        builder.Property(acknowledgement => acknowledgement.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(acknowledgement => acknowledgement.AcknowledgedAtUtc)
            .HasColumnName("acknowledged_at_utc")
            .IsRequired();

        builder.HasIndex(acknowledgement => acknowledgement.CommandId)
            .HasDatabaseName("ix_installation_command_acknowledgements_command_id");
        builder.HasIndex(acknowledgement => acknowledgement.InstallationId)
            .HasDatabaseName("ix_installation_command_acknowledgements_installation_id");

        builder.HasOne<ControlCloudInstallationCommandEntity>()
            .WithMany()
            .HasForeignKey(acknowledgement => acknowledgement.CommandId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
