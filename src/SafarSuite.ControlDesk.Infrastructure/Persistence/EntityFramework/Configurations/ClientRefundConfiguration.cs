using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientRefundConfiguration : IEntityTypeConfiguration<ClientRefund>
{
    public void Configure(EntityTypeBuilder<ClientRefund> builder)
    {
        builder.ToTable("client_refunds");

        builder.HasKey(refund => refund.Id);

        builder.Property(refund => refund.Id)
            .HasColumnName("client_refund_id")
            .HasConversion(
                id => id.Value,
                value => ClientRefundId.Create(value))
            .ValueGeneratedNever();

        builder.Property(refund => refund.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(refund => refund.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(refund => refund.Method)
            .HasColumnName("method")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(refund => refund.Reference)
            .HasColumnName("reference")
            .HasMaxLength(80)
            .HasConversion(
                reference => reference.Value,
                value => ClientRefundReference.Create(value))
            .IsRequired();

        builder.OwnsOne(refund => refund.Amount, money =>
        {
            MoneyConfiguration.Configure(money, "amount", "amount_currency_code");
        });

        builder.Navigation(refund => refund.Amount)
            .IsRequired();

        builder.Property(refund => refund.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(refund => refund.RefundedOn)
            .HasColumnName("refunded_on")
            .IsRequired();

        builder.Property(refund => refund.Note)
            .HasColumnName("note")
            .HasMaxLength(1000);

        builder.Property(refund => refund.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(refund => refund.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.HasIndex(refund => refund.ClientId)
            .HasDatabaseName("ix_client_refunds_client_id");

        builder.HasIndex(refund => new
            {
                refund.ClientId,
                refund.RefundedOn,
                refund.CreatedAtUtc,
                refund.Id
            })
            .HasDatabaseName("ix_client_refunds_client_refunded_created_id");

        builder.HasIndex(refund => refund.Reference)
            .IsUnique()
            .HasDatabaseName("ux_client_refunds_reference");
    }
}
