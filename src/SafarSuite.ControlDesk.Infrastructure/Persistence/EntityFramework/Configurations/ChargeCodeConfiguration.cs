using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Billing;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ChargeCodeConfiguration : IEntityTypeConfiguration<ChargeCode>
{
    public void Configure(EntityTypeBuilder<ChargeCode> builder)
    {
        builder.ToTable("charge_codes");

        builder.HasKey(chargeCode => chargeCode.Id);

        builder.Property(chargeCode => chargeCode.Id)
            .HasColumnName("charge_code_id")
            .HasConversion(
                id => id.Value,
                value => ChargeCodeId.Create(value))
            .ValueGeneratedNever();

        builder.Property(chargeCode => chargeCode.Code)
            .HasColumnName("code")
            .HasMaxLength(32)
            .HasConversion(
                code => code.Value,
                value => ChargeCodeKey.Create(value))
            .IsRequired();

        builder.HasIndex(chargeCode => chargeCode.Code)
            .IsUnique()
            .HasDatabaseName("ux_charge_codes_code");

        builder.Property(chargeCode => chargeCode.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(chargeCode => chargeCode.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.OwnsOne(chargeCode => chargeCode.DefaultUnitPrice, money =>
        {
            MoneyConfiguration.Configure(money, "default_unit_price_amount", "default_unit_price_currency_code");
        });

        builder.Navigation(chargeCode => chargeCode.DefaultUnitPrice)
            .IsRequired();

        builder.Property(chargeCode => chargeCode.RevenueAccountId)
            .HasColumnName("revenue_account_id")
            .HasConversion(
                id => id.Value,
                value => LedgerAccountId.Create(value))
            .IsRequired();

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(chargeCode => chargeCode.RevenueAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(chargeCode => chargeCode.TaxAccountId)
            .HasColumnName("tax_account_id")
            .HasConversion(
                id => id!.Value.Value,
                value => LedgerAccountId.Create(value));

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(chargeCode => chargeCode.TaxAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(chargeCode => chargeCode.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(chargeCode => chargeCode.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
    }
}
