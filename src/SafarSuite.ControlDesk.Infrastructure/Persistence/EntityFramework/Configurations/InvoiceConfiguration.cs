using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.Id)
            .HasColumnName("invoice_id")
            .HasConversion(
                id => id.Value,
                value => InvoiceId.Create(value))
            .ValueGeneratedNever();

        builder.Property(invoice => invoice.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(invoice => invoice.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(invoice => invoice.ContractId)
            .HasColumnName("contract_id")
            .HasConversion(
                id => id.Value,
                value => ContractId.Create(value))
            .IsRequired();

        builder.Property(invoice => invoice.Number)
            .HasColumnName("number")
            .HasMaxLength(40)
            .HasConversion(
                number => number.Value,
                value => InvoiceNumber.Create(value))
            .IsRequired();

        builder.HasIndex(invoice => invoice.Number)
            .IsUnique()
            .HasDatabaseName("ux_invoices_number");

        builder.Property(invoice => invoice.IssueDate)
            .HasColumnName("issue_date")
            .IsRequired();

        builder.Property(invoice => invoice.DueDate)
            .HasColumnName("due_date")
            .IsRequired();

        builder.Property(invoice => invoice.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(invoice => invoice.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(invoice => invoice.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.OwnsOne(invoice => invoice.AmountPaid, money =>
        {
            MoneyConfiguration.Configure(money, "amount_paid_amount", "amount_paid_currency_code");
        });

        builder.Navigation(invoice => invoice.AmountPaid)
            .IsRequired();

        builder.Ignore(invoice => invoice.TotalAmount);
        builder.Ignore(invoice => invoice.BalanceDue);

        builder.HasIndex(invoice => invoice.ClientId)
            .HasDatabaseName("ix_invoices_client_id");

        builder.HasIndex(invoice => new
            {
                invoice.ClientId,
                invoice.IssueDate,
                invoice.CreatedAtUtc,
                invoice.Id
            })
            .HasDatabaseName("ix_invoices_client_issue_created_id");

        builder.HasIndex(invoice => new
            {
                invoice.ClientId,
                invoice.Status,
                invoice.IssueDate,
                invoice.CreatedAtUtc,
                invoice.Id
            })
            .HasDatabaseName("ix_invoices_client_status_issue_created_id");

        builder.Navigation(invoice => invoice.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(invoice => invoice.Lines, line =>
        {
            line.ToTable("invoice_lines");

            line.WithOwner()
                .HasForeignKey("invoice_id");

            line.Property<int>("invoice_line_row_id")
                .HasColumnName("invoice_line_row_id")
                .ValueGeneratedOnAdd();

            line.HasKey("invoice_line_row_id");

            line.HasIndex("invoice_id")
                .HasDatabaseName("ix_invoice_lines_invoice_id");

            line.Property(invoiceLine => invoiceLine.Description)
                .HasColumnName("description")
                .HasMaxLength(512)
                .IsRequired();

            line.Property(invoiceLine => invoiceLine.LineType)
                .HasColumnName("line_type")
                .HasMaxLength(32)
                .HasConversion<string>()
                .HasDefaultValue(InvoiceLineType.Charge)
                .IsRequired();

            line.Property(invoiceLine => invoiceLine.ProductModuleCode)
                .HasColumnName("product_module_code")
                .HasMaxLength(64)
                .HasConversion(
                    code => code!.Value,
                    value => ModuleCode.Create(value));

            line.OwnsOne(invoiceLine => invoiceLine.Amount, money =>
            {
                MoneyConfiguration.Configure(money, "amount", "currency_code");
            });

            line.Navigation(invoiceLine => invoiceLine.Amount)
                .IsRequired();

            line.Property(invoiceLine => invoiceLine.ChargeCodeId)
                .HasColumnName("charge_code_id")
                .HasConversion(
                    id => id!.Value.Value,
                    value => ChargeCodeId.Create(value));

            line.HasOne<ChargeCode>()
                .WithMany()
                .HasForeignKey(invoiceLine => invoiceLine.ChargeCodeId)
                .OnDelete(DeleteBehavior.Restrict);

            line.HasIndex(invoiceLine => invoiceLine.ChargeCodeId)
                .HasDatabaseName("ix_invoice_lines_charge_code_id");

            line.HasIndex(invoiceLine => invoiceLine.ProductModuleCode)
                .HasDatabaseName("ix_invoice_lines_product_module_code");
        });
    }
}
