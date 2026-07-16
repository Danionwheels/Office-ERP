using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientContractConfiguration : IEntityTypeConfiguration<ClientContract>
{
    public void Configure(EntityTypeBuilder<ClientContract> builder)
    {
        builder.ToTable("client_contracts", table =>
        {
            table.HasCheckConstraint(
                "ck_client_contracts_named_users",
                "allowed_named_users IS NULL OR allowed_named_users >= 0");
            table.HasCheckConstraint(
                "ck_client_contracts_concurrent_users",
                "allowed_concurrent_users IS NULL OR allowed_concurrent_users >= 0");
            table.HasCheckConstraint(
                "ck_client_contracts_user_limit_order",
                "allowed_named_users IS NULL OR allowed_concurrent_users IS NULL OR allowed_concurrent_users <= allowed_named_users");
        });

        builder.HasKey(contract => contract.Id);

        builder.Property(contract => contract.Id)
            .HasColumnName("contract_id")
            .HasConversion(
                id => id.Value,
                value => ContractId.Create(value))
            .ValueGeneratedNever();

        builder.Property(contract => contract.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(contract => contract.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(contract => contract.RevisionNumber)
            .HasColumnName("revision_number")
            .IsRequired();

        builder.Property(contract => contract.SupersedesContractId)
            .HasColumnName("supersedes_contract_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? ContractId.Create(value.Value) : null);

        builder.HasOne<ClientContract>()
            .WithMany()
            .HasForeignKey(contract => contract.SupersedesContractId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(contract => contract.Number)
            .HasColumnName("number")
            .HasMaxLength(40)
            .HasConversion(
                number => number.Value,
                value => ContractNumber.Create(value))
            .IsRequired();

        builder.HasIndex(contract => contract.Number)
            .IsUnique()
            .HasDatabaseName("ux_client_contracts_number");

        builder.OwnsOne(contract => contract.Term, term =>
        {
            term.Property(value => value.StartsOn)
                .HasColumnName("starts_on")
                .IsRequired();

            term.Property(value => value.EndsOn)
                .HasColumnName("ends_on")
                .IsRequired();
        });

        builder.Navigation(contract => contract.Term)
            .IsRequired();

        builder.OwnsOne(contract => contract.Pricing, pricing =>
        {
            pricing.OwnsOne(value => value.RecurringAmount, money =>
            {
                MoneyConfiguration.Configure(money, "recurring_amount", "currency_code");
            });

            pricing.Navigation(value => value.RecurringAmount)
                .IsRequired();

            pricing.Property(value => value.BillingCycle)
                .HasColumnName("billing_cycle")
                .HasMaxLength(32)
                .HasConversion<string>()
                .IsRequired();

            pricing.Property(value => value.BillingDayOfMonth)
                .HasColumnName("billing_day_of_month")
                .IsRequired();
        });

        builder.Navigation(contract => contract.Pricing)
            .IsRequired();

        builder.OwnsOne(contract => contract.DeviceAllowance, allowance =>
        {
            allowance.Property(value => value.AllowedDevices)
                .HasColumnName("allowed_devices")
                .IsRequired();
        });

        builder.Navigation(contract => contract.DeviceAllowance)
            .IsRequired();

        builder.OwnsOne(contract => contract.BranchAllowance, allowance =>
        {
            allowance.Property(value => value.AllowedBranches)
                .HasColumnName("allowed_branches")
                .IsRequired();
        });

        builder.Navigation(contract => contract.BranchAllowance)
            .IsRequired();

        builder.OwnsOne(contract => contract.UserAllowance, allowance =>
        {
            allowance.Property(value => value.AllowedNamedUsers)
                .HasColumnName("allowed_named_users");

            allowance.Property(value => value.AllowedConcurrentUsers)
                .HasColumnName("allowed_concurrent_users");
        });

        builder.Navigation(contract => contract.UserAllowance)
            .IsRequired();

        builder.Property(contract => contract.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(contract => contract.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(contract => contract.ActivatedAtUtc)
            .HasColumnName("activated_at_utc");

        builder.Property(contract => contract.ApprovedBy)
            .HasColumnName("approved_by")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(contract => contract.ApprovalReason)
            .HasColumnName("approval_reason")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(contract => contract.ApprovedAtUtc)
            .HasColumnName("approved_at_utc")
            .IsRequired();

        builder.HasIndex(contract => new { contract.ClientId, contract.Status })
            .HasDatabaseName("ix_client_contracts_client_status");

        builder.HasIndex(contract => new { contract.ClientId, contract.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("ux_client_contracts_client_revision");

        builder.HasIndex(contract => contract.SupersedesContractId)
            .IsUnique()
            .HasFilter("supersedes_contract_id IS NOT NULL")
            .HasDatabaseName("ux_client_contracts_supersedes");

        builder.Property(contract => contract.ProductCatalogRevisionId)
            .HasColumnName("product_catalog_revision_id")
            .HasConversion(
                id => id.Value,
                value => ProductCatalogRevisionId.Create(value))
            .IsRequired();

        builder.HasOne<ProductCatalogRevisionRecord>()
            .WithMany()
            .HasForeignKey(contract => new
            {
                contract.ProductCatalogRevisionId,
                contract.ProductCatalogRevisionNumber
            })
            .HasPrincipalKey(revision => new
            {
                revision.CatalogRevisionId,
                revision.RevisionNumber
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(contract => contract.ProductCatalogRevisionNumber)
            .HasColumnName("product_catalog_revision_number")
            .IsRequired();

        builder.HasIndex(contract => contract.ProductCatalogRevisionId)
            .HasDatabaseName("ix_client_contracts_product_catalog_revision");

        builder.HasIndex(contract => contract.ClientId)
            .IsUnique()
            .HasFilter("status = 'Active'")
            .HasDatabaseName("ux_client_contracts_client_active");

        builder.Navigation(contract => contract.ModuleAllowances)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(contract => contract.ModuleAllowances, module =>
        {
            module.ToTable("client_contract_module_allowances");

            module.WithOwner()
                .HasForeignKey("contract_id");

            module.Property<int>("client_contract_module_allowance_row_id")
                .HasColumnName("client_contract_module_allowance_row_id")
                .ValueGeneratedOnAdd();

            module.HasKey("client_contract_module_allowance_row_id");

            module.Property(moduleAllowance => moduleAllowance.ModuleCode)
                .HasColumnName("module_code")
                .HasMaxLength(64)
                .HasConversion(
                    moduleCode => moduleCode.Value,
                    value => ModuleCode.Create(value))
                .IsRequired();

            module.Property(moduleAllowance => moduleAllowance.IsEnabled)
                .HasColumnName("is_enabled")
                .IsRequired();

            module.HasIndex("contract_id", nameof(ModuleAllowance.ModuleCode))
                .IsUnique()
                .HasDatabaseName("ux_client_contract_modules_contract_code");
        });

        builder.OwnsMany(contract => contract.FeatureLimits, limit =>
        {
            limit.ToTable("client_contract_feature_limits", table =>
                table.HasCheckConstraint(
                    "ck_client_contract_feature_limits_value",
                    "limit_value >= 0"));

            limit.WithOwner()
                .HasForeignKey("contract_id");

            limit.Property<int>("client_contract_feature_limit_row_id")
                .HasColumnName("client_contract_feature_limit_row_id")
                .ValueGeneratedOnAdd();

            limit.HasKey("client_contract_feature_limit_row_id");

            limit.Property(featureLimit => featureLimit.ModuleCode)
                .HasColumnName("module_code")
                .HasMaxLength(64)
                .HasConversion(
                    moduleCode => moduleCode.Value,
                    value => ModuleCode.Create(value))
                .IsRequired();

            limit.Property(featureLimit => featureLimit.FeatureCode)
                .HasColumnName("feature_code")
                .HasMaxLength(64)
                .HasConversion(
                    featureCode => featureCode.Value,
                    value => ModuleFeatureCode.Create(value))
                .IsRequired();

            limit.Property(featureLimit => featureLimit.LimitValue)
                .HasColumnName("limit_value")
                .IsRequired();

            limit.Property(featureLimit => featureLimit.Unit)
                .HasColumnName("unit")
                .HasMaxLength(32)
                .IsRequired();

            limit.HasIndex(
                    "contract_id",
                    nameof(ModuleFeatureLimit.ModuleCode),
                    nameof(ModuleFeatureLimit.FeatureCode))
                .IsUnique()
                .HasDatabaseName("ux_client_contract_feature_limits_contract_key");
        });

        builder.Navigation(contract => contract.FeatureLimits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
