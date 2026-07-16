using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.Id)
            .HasColumnName("payment_id")
            .HasConversion(
                id => id.Value,
                value => PaymentId.Create(value))
            .ValueGeneratedNever();

        builder.Property(payment => payment.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(payment => payment.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(payment => payment.InvoiceId)
            .HasColumnName("invoice_id")
            .HasConversion(
                id => id.Value,
                value => InvoiceId.Create(value))
            .IsRequired();

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(payment => payment.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(payment => payment.Method)
            .HasColumnName("method")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(payment => payment.Reference)
            .HasColumnName("reference")
            .HasMaxLength(80)
            .HasConversion(
                reference => reference.Value,
                value => PaymentReference.Create(value))
            .IsRequired();

        builder.OwnsOne(payment => payment.Amount, money =>
        {
            MoneyConfiguration.Configure(money, "amount", "currency_code");
        });

        builder.Navigation(payment => payment.Amount)
            .IsRequired();

        builder.Property(payment => payment.ReceivedOn)
            .HasColumnName("received_on")
            .IsRequired();

        builder.Property(payment => payment.RecordedAtUtc)
            .HasColumnName("recorded_at_utc")
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(payment => payment.DecisionNote)
            .HasColumnName("decision_note")
            .HasMaxLength(1000);

        builder.Property(payment => payment.PortalClaimId)
            .HasColumnName("portal_claim_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue
                    ? PortalPaymentClaimId.Create(value.Value)
                    : (PortalPaymentClaimId?)null);

        builder.HasIndex(payment => payment.ClientId)
            .HasDatabaseName("ix_payments_client_id");

        builder.HasIndex(payment => new
            {
                payment.ClientId,
                payment.ReceivedOn,
                payment.RecordedAtUtc,
                payment.Id
            })
            .HasDatabaseName("ix_payments_client_received_recorded_id");

        builder.HasIndex(payment => new
            {
                payment.ClientId,
                payment.Status,
                payment.ReceivedOn,
                payment.RecordedAtUtc,
                payment.Id
            })
            .HasDatabaseName("ix_payments_client_status_received_recorded_id");

        builder.HasIndex(payment => payment.InvoiceId)
            .HasDatabaseName("ix_payments_invoice_id");

        builder.HasIndex(payment => payment.Reference)
            .HasDatabaseName("ix_payments_reference");

        builder.HasIndex(payment => payment.PortalClaimId)
            .IsUnique()
            .HasFilter("\"portal_claim_id\" IS NOT NULL")
            .HasDatabaseName("ux_payments_portal_claim_id");
    }
}
