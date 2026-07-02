using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientPortalInvitationEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientPortalInvitationEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudClientPortalInvitationEntity> builder)
    {
        builder.ToTable("client_portal_invitations");

        builder.HasKey(invitation => invitation.InvitationId);

        builder.Property(invitation => invitation.InvitationId)
            .HasColumnName("invitation_id")
            .ValueGeneratedNever();
        builder.Property(invitation => invitation.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(invitation => invitation.Email)
            .HasColumnName("email")
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(invitation => invitation.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(invitation => invitation.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(180)
            .IsRequired();
        builder.Property(invitation => invitation.Role)
            .HasColumnName("role")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(invitation => invitation.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(invitation => invitation.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(invitation => invitation.CreatedBy)
            .HasColumnName("created_by")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(invitation => invitation.InvitedAtUtc)
            .HasColumnName("invited_at_utc")
            .IsRequired();
        builder.Property(invitation => invitation.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();
        builder.Property(invitation => invitation.AcceptedAtUtc)
            .HasColumnName("accepted_at_utc");
        builder.Property(invitation => invitation.AcceptedUserId)
            .HasColumnName("accepted_user_id");

        builder.HasIndex(invitation => invitation.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_client_portal_invitations_token_hash");
        builder.HasIndex(invitation => new { invitation.ClientId, invitation.NormalizedEmail, invitation.Status })
            .HasDatabaseName("ix_client_portal_invitations_client_email_status");
    }
}
