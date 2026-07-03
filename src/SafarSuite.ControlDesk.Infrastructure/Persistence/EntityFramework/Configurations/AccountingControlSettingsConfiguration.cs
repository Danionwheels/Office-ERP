using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class AccountingControlSettingsConfiguration : IEntityTypeConfiguration<AccountingControlSettings>
{
    public void Configure(EntityTypeBuilder<AccountingControlSettings> builder)
    {
        builder.ToTable("accounting_control_settings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.Id)
            .HasColumnName("accounting_control_settings_id")
            .HasConversion(
                id => id.Value,
                value => AccountingControlSettingsId.Create(value))
            .ValueGeneratedNever();

        builder.Property(settings => settings.CompanyCode)
            .HasColumnName("company_code")
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(settings => settings.CompanyCode)
            .IsUnique()
            .HasDatabaseName("ux_accounting_control_settings_company");

        builder.Property(settings => settings.BaseCurrencyCode)
            .HasColumnName("base_currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(settings => settings.RetainedEarningsAccountId)
            .HasColumnName("retained_earnings_account_id")
            .HasConversion(
                id => id!.Value.Value,
                value => LedgerAccountId.Create(value));

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(settings => settings.RetainedEarningsAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(settings => settings.RetainedEarningsAccountId)
            .HasDatabaseName("ix_accounting_control_settings_retained_earnings_account_id");

        builder.Property(settings => settings.IncomeSummaryAccountId)
            .HasColumnName("income_summary_account_id")
            .HasConversion(
                id => id!.Value.Value,
                value => LedgerAccountId.Create(value));

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(settings => settings.IncomeSummaryAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(settings => settings.IncomeSummaryAccountId)
            .HasDatabaseName("ix_accounting_control_settings_income_summary_account_id");

        builder.Property(settings => settings.RoundingAccountId)
            .HasColumnName("rounding_account_id")
            .HasConversion(
                id => id!.Value.Value,
                value => LedgerAccountId.Create(value));

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(settings => settings.RoundingAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(settings => settings.RoundingAccountId)
            .HasDatabaseName("ix_accounting_control_settings_rounding_account_id");

        builder.Property(settings => settings.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(settings => settings.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Ignore(settings => settings.IsConfigured);
    }
}
