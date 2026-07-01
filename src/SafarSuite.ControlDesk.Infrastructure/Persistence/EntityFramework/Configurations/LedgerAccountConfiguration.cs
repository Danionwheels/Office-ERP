using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class LedgerAccountConfiguration : IEntityTypeConfiguration<LedgerAccount>
{
    public void Configure(EntityTypeBuilder<LedgerAccount> builder)
    {
        builder.ToTable("ledger_accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("ledger_account_id")
            .HasConversion(
                id => id.Value,
                value => LedgerAccountId.Create(value))
            .ValueGeneratedNever();

        builder.Property(account => account.Code)
            .HasColumnName("code")
            .HasMaxLength(32)
            .HasConversion(
                code => code.Value,
                value => LedgerAccountCode.Create(value))
            .IsRequired();

        builder.HasIndex(account => account.Code)
            .IsUnique()
            .HasDatabaseName("ux_ledger_accounts_code");

        builder.Property(account => account.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(account => account.Type)
            .HasColumnName("type")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(account => account.NormalBalance)
            .HasColumnName("normal_balance")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(account => account.ParentAccountId)
            .HasColumnName("parent_account_id")
            .HasConversion(
                id => id!.Value.Value,
                value => LedgerAccountId.Create(value));

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(account => account.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(account => account.IsPostingAccount)
            .HasColumnName("is_posting_account")
            .IsRequired();

        builder.Property(account => account.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(account => account.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();
    }
}
