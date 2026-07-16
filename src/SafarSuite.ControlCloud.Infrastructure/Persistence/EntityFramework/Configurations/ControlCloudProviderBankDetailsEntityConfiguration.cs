using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudProviderBankDetailsEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudProviderBankDetailsEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudProviderBankDetailsEntity> builder)
    {
        builder.ToTable("provider_bank_details");

        builder.HasKey(details => details.BankDetailsId);

        builder.Property(details => details.BankDetailsId)
            .HasColumnName("bank_details_id")
            .HasMaxLength(32)
            .ValueGeneratedNever();
        builder.Property(details => details.BankName)
            .HasColumnName("bank_name")
            .HasMaxLength(180)
            .IsRequired();
        builder.Property(details => details.AccountTitle)
            .HasColumnName("account_title")
            .HasMaxLength(180)
            .IsRequired();
        builder.Property(details => details.AccountNumber)
            .HasColumnName("account_number")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(details => details.Iban)
            .HasColumnName("iban")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(details => details.BranchOrRoutingInfo)
            .HasColumnName("branch_or_routing_info")
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(details => details.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
    }
}
