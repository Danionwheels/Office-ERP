using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class AccountCodeRangeConfiguration : IEntityTypeConfiguration<AccountCodeRange>
{
    public void Configure(EntityTypeBuilder<AccountCodeRange> builder)
    {
        builder.ToTable("account_code_ranges");

        builder.HasKey(range => range.Id);

        builder.Property(range => range.Id)
            .HasColumnName("account_code_range_id")
            .HasConversion(
                id => id.Value,
                value => AccountCodeRangeId.Create(value))
            .ValueGeneratedNever();

        builder.Property(range => range.CompanyCode)
            .HasColumnName("company_code")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(range => range.Role)
            .HasColumnName("role")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(range => new { range.CompanyCode, range.Role })
            .IsUnique()
            .HasDatabaseName("ux_account_code_ranges_company_role");

        builder.Property(range => range.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(range => range.SearchPrefix)
            .HasColumnName("search_prefix")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(range => range.RangeStart)
            .HasColumnName("range_start")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(range => range.RangeEnd)
            .HasColumnName("range_end")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(range => range.CodeLength)
            .HasColumnName("code_length")
            .IsRequired();

        builder.Property(range => range.AccountType)
            .HasColumnName("account_type")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(range => range.NormalBalance)
            .HasColumnName("normal_balance")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(range => range.IsPostingAccount)
            .HasColumnName("is_posting_account")
            .IsRequired();

        builder.Property(range => range.ParentCode)
            .HasColumnName("parent_code")
            .HasMaxLength(32);

        builder.Property(range => range.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(range => range.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(range => range.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
    }
}
