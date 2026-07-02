using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientChargeRuleConfiguration : IEntityTypeConfiguration<ClientChargeRule>
{
    public void Configure(EntityTypeBuilder<ClientChargeRule> builder)
    {
        builder.ToTable("client_charge_rules");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .HasColumnName("client_charge_rule_id")
            .HasConversion(
                id => id.Value,
                value => ClientChargeRuleId.Create(value))
            .ValueGeneratedNever();

        builder.Property(rule => rule.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(rule => rule.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(rule => rule.ContractId)
            .HasColumnName("contract_id")
            .HasConversion(
                id => id!.Value.Value,
                value => ContractId.Create(value));

        builder.Property(rule => rule.ChargeCodeId)
            .HasColumnName("charge_code_id")
            .HasConversion(
                id => id.Value,
                value => ChargeCodeId.Create(value))
            .IsRequired();

        builder.HasOne<ChargeCode>()
            .WithMany()
            .HasForeignKey(rule => rule.ChargeCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(rule => rule.DescriptionOverride)
            .HasColumnName("description_override")
            .HasMaxLength(512);

        builder.OwnsOne(rule => rule.UnitPrice, money =>
        {
            MoneyConfiguration.Configure(money, "unit_price_amount", "unit_price_currency_code");
        });

        builder.Navigation(rule => rule.UnitPrice)
            .IsRequired();

        builder.Property(rule => rule.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 4)
            .IsRequired();

        builder.Property(rule => rule.TaxPercent)
            .HasColumnName("tax_percent")
            .HasPrecision(9, 4)
            .IsRequired();

        builder.Property(rule => rule.BillingCycle)
            .HasColumnName("billing_cycle")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(rule => rule.BillingDayOfMonth)
            .HasColumnName("billing_day_of_month")
            .IsRequired();

        builder.OwnsOne(rule => rule.EffectivePeriod, period =>
        {
            period.Property(value => value.StartsOn)
                .HasColumnName("effective_starts_on")
                .IsRequired();

            period.Property(value => value.EndsOn)
                .HasColumnName("effective_ends_on")
                .IsRequired();
        });

        builder.Navigation(rule => rule.EffectivePeriod)
            .IsRequired();

        builder.Property(rule => rule.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(rule => rule.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Ignore(rule => rule.LineAmount);
        builder.Ignore(rule => rule.TaxAmount);
        builder.Ignore(rule => rule.TotalLineAmount);

        builder.HasIndex(rule => rule.ClientId)
            .HasDatabaseName("ix_client_charge_rules_client_id");

        builder.HasIndex(rule => rule.ChargeCodeId)
            .HasDatabaseName("ix_client_charge_rules_charge_code_id");
    }
}
