using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientCreditApplicationConfiguration : IEntityTypeConfiguration<ClientCreditApplication>
{
    public void Configure(EntityTypeBuilder<ClientCreditApplication> builder)
    {
        builder.ToTable("client_credit_applications");

        builder.HasKey(application => application.Id);

        builder.Property(application => application.Id)
            .HasColumnName("client_credit_application_id")
            .HasConversion(
                id => id.Value,
                value => ClientCreditApplicationId.Create(value))
            .ValueGeneratedNever();

        builder.Property(application => application.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(application => application.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(application => application.InvoiceId)
            .HasColumnName("invoice_id")
            .HasConversion(
                id => id.Value,
                value => InvoiceId.Create(value))
            .IsRequired();

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(application => application.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(application => application.Reference)
            .HasColumnName("reference")
            .HasMaxLength(80)
            .HasConversion(
                reference => reference.Value,
                value => ClientCreditApplicationReference.Create(value))
            .IsRequired();

        builder.OwnsOne(application => application.Amount, money =>
        {
            MoneyConfiguration.Configure(money, "amount", "amount_currency_code");
        });

        builder.Navigation(application => application.Amount)
            .IsRequired();

        builder.Property(application => application.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(application => application.AppliedOn)
            .HasColumnName("applied_on")
            .IsRequired();

        builder.Property(application => application.Note)
            .HasColumnName("note")
            .HasMaxLength(1000);

        builder.Property(application => application.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(application => application.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(application => application.ClientId)
            .HasDatabaseName("ix_client_credit_applications_client_id");

        builder.HasIndex(application => application.InvoiceId)
            .HasDatabaseName("ix_client_credit_applications_invoice_id");

        builder.HasIndex(application => application.Reference)
            .IsUnique()
            .HasDatabaseName("ux_client_credit_applications_reference");
    }
}
