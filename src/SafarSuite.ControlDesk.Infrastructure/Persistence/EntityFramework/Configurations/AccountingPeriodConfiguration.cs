using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("accounting_periods");

        builder.HasKey(period => period.Id);

        builder.Property(period => period.Id)
            .HasColumnName("accounting_period_id")
            .HasConversion(
                id => id.Value,
                value => AccountingPeriodId.Create(value))
            .ValueGeneratedNever();

        builder.Property(period => period.CompanyCode)
            .HasColumnName("company_code")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(period => period.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(period => period.StartsOn)
            .HasColumnName("starts_on")
            .IsRequired();

        builder.Property(period => period.EndsOn)
            .HasColumnName("ends_on")
            .IsRequired();

        builder.Property(period => period.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(period => period.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(period => period.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(period => period.ClosedAtUtc)
            .HasColumnName("closed_at_utc");

        builder.Property(period => period.ReopenedAtUtc)
            .HasColumnName("reopened_at_utc");

        builder.HasIndex(period => new { period.CompanyCode, period.StartsOn })
            .IsUnique()
            .HasDatabaseName("ux_accounting_periods_company_start");

        builder.HasIndex(period => new { period.CompanyCode, period.Status })
            .HasDatabaseName("ix_accounting_periods_company_status");

        builder.Navigation(period => period.CloseArtifacts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(period => period.CloseArtifacts, artifact =>
        {
            artifact.ToTable("accounting_period_close_artifacts");

            artifact.WithOwner()
                .HasForeignKey("accounting_period_id");

            artifact.Property<int>("accounting_period_close_artifact_row_id")
                .HasColumnName("accounting_period_close_artifact_row_id")
                .ValueGeneratedOnAdd();

            artifact.HasKey("accounting_period_close_artifact_row_id");

            artifact.Property(closeArtifact => closeArtifact.GeneratedAtUtc)
                .HasColumnName("generated_at_utc")
                .IsRequired();

            artifact.Property(closeArtifact => closeArtifact.GeneratedBy)
                .HasColumnName("generated_by")
                .HasMaxLength(128)
                .IsRequired();

            artifact.Property(closeArtifact => closeArtifact.CheckCount)
                .HasColumnName("check_count")
                .IsRequired();

            artifact.Property(closeArtifact => closeArtifact.BlockedCheckCount)
                .HasColumnName("blocked_check_count")
                .IsRequired();

            artifact.Property(closeArtifact => closeArtifact.CurrencyCount)
                .HasColumnName("currency_count")
                .IsRequired();

            artifact.Property(closeArtifact => closeArtifact.PostedJournalCount)
                .HasColumnName("posted_journal_count")
                .IsRequired();

            artifact.Property(closeArtifact => closeArtifact.DraftJournalCount)
                .HasColumnName("draft_journal_count")
                .IsRequired();

            artifact.Property(closeArtifact => closeArtifact.SnapshotJson)
                .HasColumnName("snapshot_json")
                .IsRequired();

            artifact.HasIndex("accounting_period_id", nameof(AccountingPeriodCloseArtifact.GeneratedAtUtc))
                .HasDatabaseName("ix_accounting_period_close_artifacts_period_generated");
        });
    }
}
