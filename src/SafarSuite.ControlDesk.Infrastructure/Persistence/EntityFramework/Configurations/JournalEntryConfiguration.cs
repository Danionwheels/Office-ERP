using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.SharedKernel;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("journal_entries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Id)
            .HasColumnName("journal_entry_id")
            .HasConversion(
                id => id.Value,
                value => JournalEntryId.Create(value))
            .ValueGeneratedNever();

        builder.Property(entry => entry.EntryDate)
            .HasColumnName("entry_date")
            .IsRequired();

        builder.Property(entry => entry.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(entry => entry.SourceType)
            .HasColumnName("source_type")
            .HasMaxLength(64)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(entry => entry.SourceReference)
            .HasColumnName("source_reference")
            .HasMaxLength(128);

        builder.Property(entry => entry.Memo)
            .HasColumnName("memo")
            .HasMaxLength(512);

        builder.Property(entry => entry.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(entry => entry.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(entry => entry.PostedAtUtc)
            .HasColumnName("posted_at_utc");

        builder.Property(entry => entry.VoidedAtUtc)
            .HasColumnName("voided_at_utc");

        builder.HasIndex(entry => new { entry.EntryDate, entry.CreatedAtUtc, entry.Id })
            .HasDatabaseName("ix_journal_entries_entry_date_created_id");

        builder.HasIndex(entry => entry.SourceType)
            .HasDatabaseName("ix_journal_entries_source_type");

        builder.Navigation(entry => entry.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(entry => entry.Lines, line =>
        {
            line.ToTable("journal_lines");

            line.WithOwner()
                .HasForeignKey("journal_entry_id");

            line.Property<int>("journal_line_row_id")
                .HasColumnName("journal_line_row_id")
                .ValueGeneratedOnAdd();

            line.HasKey("journal_line_row_id");

            line.Property(journalLine => journalLine.LedgerAccountId)
                .HasColumnName("ledger_account_id")
                .HasConversion(
                    id => id.Value,
                    value => LedgerAccountId.Create(value))
                .IsRequired();

            line.HasOne<LedgerAccount>()
                .WithMany()
                .HasForeignKey(journalLine => journalLine.LedgerAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            ConfigureMoney(line, journalLine => journalLine.Debit, "debit");
            ConfigureMoney(line, journalLine => journalLine.Credit, "credit");

            line.Property(journalLine => journalLine.Description)
                .HasColumnName("description")
                .HasMaxLength(512);

            line.HasIndex(journalLine => journalLine.LedgerAccountId)
                .HasDatabaseName("ix_journal_lines_ledger_account_id");
        });
    }

    private static void ConfigureMoney(
        OwnedNavigationBuilder<JournalEntry, JournalLine> line,
        global::System.Linq.Expressions.Expression<Func<JournalLine, Money?>> navigationExpression,
        string columnPrefix)
    {
        line.OwnsOne(navigationExpression, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName($"{columnPrefix}_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(value => value.CurrencyCode)
                .HasColumnName($"{columnPrefix}_currency_code")
                .HasMaxLength(3)
                .IsRequired();
        });
    }
}
