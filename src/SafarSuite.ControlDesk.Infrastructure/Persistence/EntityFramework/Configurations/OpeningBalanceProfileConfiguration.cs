using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class OpeningBalanceProfileConfiguration : IEntityTypeConfiguration<OpeningBalanceProfile>
{
    public void Configure(EntityTypeBuilder<OpeningBalanceProfile> builder)
    {
        builder.ToTable("opening_balance_profiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .HasColumnName("opening_balance_profile_id")
            .HasConversion(
                id => id.Value,
                value => OpeningBalanceProfileId.Create(value))
            .ValueGeneratedNever();

        builder.Property(profile => profile.CompanyCode)
            .HasColumnName("company_code")
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(profile => profile.CompanyCode)
            .IsUnique()
            .HasDatabaseName("ux_opening_balance_profiles_company");

        builder.Property(profile => profile.FiscalYearFrom)
            .HasColumnName("fiscal_year_from")
            .IsRequired();

        builder.Property(profile => profile.FiscalYearTo)
            .HasColumnName("fiscal_year_to")
            .IsRequired();

        builder.Property(profile => profile.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(profile => profile.TransactionsAllowed)
            .HasColumnName("transactions_allowed")
            .IsRequired();

        builder.Property(profile => profile.ProfitAndLossCarryForwardAccountId)
            .HasColumnName("profit_and_loss_carry_forward_account_id")
            .HasConversion(
                id => id!.Value.Value,
                value => LedgerAccountId.Create(value));

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(profile => profile.ProfitAndLossCarryForwardAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(profile => profile.ProfitAndLossCarryForwardAccountId)
            .HasDatabaseName("ix_opening_balance_profiles_pl_carry_forward_account_id");

        builder.Property(profile => profile.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(profile => profile.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Ignore(profile => profile.IsConfigured);
    }
}
