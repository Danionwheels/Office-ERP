using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("credit_notes");

        builder.HasKey(creditNote => creditNote.Id);

        builder.Property(creditNote => creditNote.Id)
            .HasColumnName("credit_note_id")
            .HasConversion(
                id => id.Value,
                value => CreditNoteId.Create(value))
            .ValueGeneratedNever();

        builder.Property(creditNote => creditNote.InvoiceId)
            .HasColumnName("invoice_id")
            .HasConversion(
                id => id.Value,
                value => InvoiceId.Create(value))
            .IsRequired();

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(creditNote => creditNote.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(creditNote => creditNote.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(creditNote => creditNote.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(creditNote => creditNote.ContractId)
            .HasColumnName("contract_id")
            .HasConversion(
                id => id.Value,
                value => ContractId.Create(value))
            .IsRequired();

        builder.Property(creditNote => creditNote.Number)
            .HasColumnName("number")
            .HasMaxLength(40)
            .HasConversion(
                number => number.Value,
                value => CreditNoteNumber.Create(value))
            .IsRequired();

        builder.HasIndex(creditNote => creditNote.Number)
            .IsUnique()
            .HasDatabaseName("ux_credit_notes_number");

        builder.Property(creditNote => creditNote.CreditDate)
            .HasColumnName("credit_date")
            .IsRequired();

        builder.OwnsOne(creditNote => creditNote.TotalAmount, money =>
        {
            MoneyConfiguration.Configure(money, "total_amount", "total_amount_currency_code");
        });

        builder.Navigation(creditNote => creditNote.TotalAmount)
            .IsRequired();

        builder.Property(creditNote => creditNote.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(creditNote => creditNote.Reason)
            .HasColumnName("reason")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(creditNote => creditNote.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(creditNote => creditNote.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.HasIndex(creditNote => creditNote.ClientId)
            .HasDatabaseName("ix_credit_notes_client_id");

        builder.HasIndex(creditNote => creditNote.InvoiceId)
            .IsUnique()
            .HasDatabaseName("ux_credit_notes_invoice_id");
    }
}
