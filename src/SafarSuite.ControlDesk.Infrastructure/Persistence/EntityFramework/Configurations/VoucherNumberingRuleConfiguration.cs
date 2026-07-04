using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class VoucherNumberingRuleConfiguration : IEntityTypeConfiguration<VoucherNumberingRule>
{
    public void Configure(EntityTypeBuilder<VoucherNumberingRule> builder)
    {
        builder.ToTable("voucher_numbering_rules");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .HasColumnName("voucher_numbering_rule_id")
            .HasConversion(
                id => id.Value,
                value => VoucherNumberingRuleId.Create(value))
            .ValueGeneratedNever();

        builder.Property(rule => rule.CompanyCode)
            .HasColumnName("company_code")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(rule => rule.SourceType)
            .HasColumnName("source_type")
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(rule => rule.Prefix)
            .HasColumnName("prefix")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(rule => rule.NumberPaddingWidth)
            .HasColumnName("number_padding_width")
            .IsRequired();

        builder.Property(rule => rule.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(rule => rule.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(rule => rule.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasIndex(rule => new { rule.CompanyCode, rule.SourceType })
            .IsUnique()
            .HasDatabaseName("ux_voucher_numbering_rules_company_source");
    }
}
