using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudClientPortalPaymentClaimEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudClientPortalPaymentClaimEntity>
{
    public const string ClientReferenceUniqueConstraintName =
        "ux_client_portal_payment_claims_client_reference";

    public void Configure(EntityTypeBuilder<ControlCloudClientPortalPaymentClaimEntity> builder)
    {
        builder.ToTable("client_portal_payment_claims");

        builder.HasKey(claim => claim.ClaimId);

        builder.Property(claim => claim.ClaimId)
            .HasColumnName("claim_id")
            .ValueGeneratedNever();
        builder.Property(claim => claim.ClientId)
            .HasColumnName("client_id")
            .IsRequired();
        builder.Property(claim => claim.SubmittedByUserId)
            .HasColumnName("submitted_by_user_id")
            .IsRequired();
        builder.Property(claim => claim.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();
        builder.Property(claim => claim.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(claim => claim.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(claim => claim.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(claim => claim.TransferReferenceNumber)
            .HasColumnName("transfer_reference_number")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(claim => claim.NormalizedTransferReferenceNumber)
            .HasColumnName("normalized_transfer_reference_number")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(claim => claim.ProofAttachmentId)
            .HasColumnName("proof_attachment_id");
        builder.Property(claim => claim.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(claim => claim.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .IsRequired();
        builder.Property(claim => claim.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc");
        builder.Property(claim => claim.VerifiedPaymentId)
            .HasColumnName("verified_payment_id");
        builder.Property(claim => claim.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(1000);
        builder.Property(claim => claim.ConcurrencyToken)
            .HasColumnName("concurrency_token")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasIndex(claim => new
            {
                claim.ClientId,
                claim.NormalizedTransferReferenceNumber
            })
            .IsUnique()
            .HasDatabaseName(ClientReferenceUniqueConstraintName);
        builder.HasIndex(claim => new { claim.ClientId, claim.Status, claim.SubmittedAtUtc })
            .HasDatabaseName("ix_client_portal_payment_claims_client_status_submitted");
        builder.HasIndex(claim => claim.InvoiceId)
            .HasDatabaseName("ix_client_portal_payment_claims_invoice_id");
    }
}
