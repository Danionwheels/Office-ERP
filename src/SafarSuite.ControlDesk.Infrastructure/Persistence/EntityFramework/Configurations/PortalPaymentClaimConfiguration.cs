using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class PortalPaymentClaimConfiguration : IEntityTypeConfiguration<PortalPaymentClaim>
{
    public void Configure(EntityTypeBuilder<PortalPaymentClaim> builder)
    {
        builder.ToTable("portal_payment_claims");
        builder.HasKey(claim => claim.Id);

        builder.Property(claim => claim.Id)
            .HasColumnName("claim_id")
            .HasConversion(id => id.Value, value => PortalPaymentClaimId.Create(value))
            .ValueGeneratedNever();

        builder.Property(claim => claim.ClientId)
            .HasColumnName("client_id")
            .HasConversion(id => id.Value, value => ClientId.Create(value))
            .IsRequired();
        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(claim => claim.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(claim => claim.InvoiceId)
            .HasColumnName("invoice_id")
            .HasConversion(id => id.Value, value => InvoiceId.Create(value))
            .IsRequired();
        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(claim => claim.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(claim => claim.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(80)
            .IsRequired();

        builder.OwnsOne(claim => claim.Amount, money =>
        {
            MoneyConfiguration.Configure(money, "amount", "currency_code");
        });
        builder.Navigation(claim => claim.Amount).IsRequired();

        builder.Property(claim => claim.TransferReferenceNumber)
            .HasColumnName("transfer_reference_number")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(claim => claim.ProofAttachmentId)
            .HasColumnName("proof_attachment_id");
        builder.Property(claim => claim.ProofFileName)
            .HasColumnName("proof_file_name")
            .HasMaxLength(255);
        builder.Property(claim => claim.ProofContentType)
            .HasColumnName("proof_content_type")
            .HasMaxLength(120);
        builder.Property(claim => claim.ProofSizeBytes)
            .HasColumnName("proof_size_bytes");
        builder.Property(claim => claim.ProofUploadedAtUtc)
            .HasColumnName("proof_uploaded_at_utc");
        builder.Property(claim => claim.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(claim => claim.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .IsRequired();
        builder.Property(claim => claim.ImportedAtUtc)
            .HasColumnName("imported_at_utc")
            .IsRequired();
        builder.Property(claim => claim.ReviewedAtUtc)
            .HasColumnName("reviewed_at_utc");
        builder.Property(claim => claim.VerifiedPaymentId)
            .HasColumnName("verified_payment_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? PaymentId.Create(value.Value) : (PaymentId?)null);
        builder.Property(claim => claim.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(1000);

        builder.HasIndex(claim => claim.ClientId)
            .HasDatabaseName("ix_portal_payment_claims_client_id");
        builder.HasIndex(claim => new { claim.ClientId, claim.Status, claim.SubmittedAtUtc })
            .HasDatabaseName("ix_portal_payment_claims_client_status_submitted");
        builder.HasIndex(claim => claim.InvoiceId)
            .HasDatabaseName("ix_portal_payment_claims_invoice_id");
    }
}
