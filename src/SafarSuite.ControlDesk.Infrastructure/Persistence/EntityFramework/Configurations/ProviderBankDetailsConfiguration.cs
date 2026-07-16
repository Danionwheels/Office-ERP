using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Payments;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ProviderBankDetailsConfiguration : IEntityTypeConfiguration<ProviderBankDetails>
{
    public void Configure(EntityTypeBuilder<ProviderBankDetails> builder)
    {
        builder.ToTable("provider_bank_details");
        builder.HasKey(details => details.Id);
        builder.Property(details => details.Id)
            .HasColumnName("provider_bank_details_id")
            .HasConversion(id => id.Value, value => ProviderBankDetailsId.Create(value))
            .ValueGeneratedNever();
        builder.Property(details => details.IsConfigured)
            .HasColumnName("is_configured")
            .IsRequired();
        builder.Property(details => details.BankName)
            .HasColumnName("bank_name")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(details => details.AccountTitle)
            .HasColumnName("account_title")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(details => details.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(details => details.Iban)
            .HasColumnName("iban")
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(details => details.BranchOrRoutingInfo)
            .HasColumnName("branch_or_routing_info")
            .HasMaxLength(240)
            .IsRequired();
        builder.Property(details => details.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
    }
}
