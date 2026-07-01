using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Accounting;
using SafarSuite.ControlDesk.Domain.Modules.Clients;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientAccountingProfileConfiguration : IEntityTypeConfiguration<ClientAccountingProfile>
{
    public void Configure(EntityTypeBuilder<ClientAccountingProfile> builder)
    {
        builder.ToTable("client_accounting_profiles");

        builder.HasKey(profile => profile.Id);

        builder.Property(profile => profile.Id)
            .HasColumnName("client_accounting_profile_id")
            .HasConversion(
                id => id.Value,
                value => ClientAccountingProfileId.Create(value))
            .ValueGeneratedNever();

        builder.Property(profile => profile.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(profile => profile.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(profile => profile.ClientId)
            .IsUnique()
            .HasDatabaseName("ux_client_accounting_profiles_client_id");

        builder.Property(profile => profile.AccountsReceivableAccountId)
            .HasColumnName("accounts_receivable_account_id")
            .HasConversion(
                id => id.Value,
                value => LedgerAccountId.Create(value))
            .IsRequired();

        builder.HasOne<LedgerAccount>()
            .WithMany()
            .HasForeignKey(profile => profile.AccountsReceivableAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(profile => profile.DefaultCurrencyCode)
            .HasColumnName("default_currency_code")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(profile => profile.CloudCustomerId)
            .HasColumnName("cloud_customer_id")
            .HasMaxLength(128);

        builder.Property(profile => profile.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(profile => profile.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();
    }
}
