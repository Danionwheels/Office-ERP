using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Billing;
using SafarSuite.ControlDesk.Domain.Modules.Clients;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Domain.Modules.Entitlements;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ClientAccessRevisionConfiguration : IEntityTypeConfiguration<ClientAccessRevision>
{
    public void Configure(EntityTypeBuilder<ClientAccessRevision> builder)
    {
        builder.ToTable("client_access_revisions", table =>
        {
            table.HasCheckConstraint(
                "ck_client_access_revisions_named_users",
                "allowed_named_users IS NULL OR allowed_named_users >= 0");
            table.HasCheckConstraint(
                "ck_client_access_revisions_concurrent_users",
                "allowed_concurrent_users IS NULL OR allowed_concurrent_users >= 0");
            table.HasCheckConstraint(
                "ck_client_access_revisions_user_limit_order",
                "allowed_named_users IS NULL OR allowed_concurrent_users IS NULL OR allowed_concurrent_users <= allowed_named_users");
        });

        builder.HasKey(revision => revision.Id);

        builder.Property(revision => revision.Id)
            .HasColumnName("client_access_revision_id")
            .HasConversion(
                id => id.Value,
                value => ClientAccessRevisionId.Create(value))
            .ValueGeneratedNever();

        builder.Property(revision => revision.ClientId)
            .HasColumnName("client_id")
            .HasConversion(
                id => id.Value,
                value => ClientId.Create(value))
            .IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(revision => revision.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(revision => revision.ContractId)
            .HasColumnName("contract_id")
            .HasConversion(
                id => id.Value,
                value => ContractId.Create(value))
            .IsRequired();

        builder.HasOne<ClientContract>()
            .WithMany()
            .HasForeignKey(revision => revision.ContractId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(revision => revision.ContractRevisionNumber)
            .HasColumnName("contract_revision_number")
            .IsRequired();

        builder.Property(revision => revision.ProductCatalogRevisionId)
            .HasColumnName("product_catalog_revision_id")
            .HasConversion(
                id => id.Value,
                value => ProductCatalogRevisionId.Create(value))
            .IsRequired();

        builder.HasOne<ProductCatalogRevisionRecord>()
            .WithMany()
            .HasForeignKey(revision => new
            {
                revision.ProductCatalogRevisionId,
                revision.ProductCatalogRevisionNumber
            })
            .HasPrincipalKey(catalogRevision => new
            {
                catalogRevision.CatalogRevisionId,
                catalogRevision.RevisionNumber
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(revision => revision.ProductCatalogRevisionNumber)
            .HasColumnName("product_catalog_revision_number")
            .IsRequired();

        builder.Property(revision => revision.SourceInvoiceId)
            .HasColumnName("source_invoice_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? InvoiceId.Create(value.Value) : null);

        builder.HasOne<Invoice>()
            .WithMany()
            .HasForeignKey(revision => revision.SourceInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(revision => revision.SourceInvoiceNumber)
            .HasColumnName("source_invoice_number")
            .HasMaxLength(40);

        builder.Property(revision => revision.EvidenceType)
            .HasColumnName("evidence_type")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(revision => revision.RevisionNumber)
            .HasColumnName("revision_number")
            .IsRequired();

        builder.Property(revision => revision.SupersedesRevisionId)
            .HasColumnName("supersedes_revision_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? ClientAccessRevisionId.Create(value.Value) : null);

        builder.HasOne<ClientAccessRevision>()
            .WithMany()
            .HasForeignKey(revision => revision.SupersedesRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(revision => revision.PaidUntil)
            .HasColumnName("paid_until")
            .IsRequired();

        builder.Property(revision => revision.GraceUntil)
            .HasColumnName("grace_until")
            .IsRequired();

        builder.Property(revision => revision.OfflineValidUntil)
            .HasColumnName("offline_valid_until")
            .IsRequired();

        builder.Property(revision => revision.AllowedDevices)
            .HasColumnName("allowed_devices")
            .IsRequired();

        builder.Property(revision => revision.AllowedBranches)
            .HasColumnName("allowed_branches")
            .IsRequired();

        builder.Property(revision => revision.AllowedNamedUsers)
            .HasColumnName("allowed_named_users");

        builder.Property(revision => revision.AllowedConcurrentUsers)
            .HasColumnName("allowed_concurrent_users");

        builder.Property(revision => revision.EffectiveFromUtc)
            .HasColumnName("effective_from_utc")
            .IsRequired();

        builder.Property(revision => revision.ApprovedBy)
            .HasColumnName("approved_by")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(revision => revision.ApprovalReason)
            .HasColumnName("approval_reason")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(revision => revision.ApprovedAtUtc)
            .HasColumnName("approved_at_utc")
            .IsRequired();

        builder.HasIndex(revision => new { revision.ClientId, revision.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("ux_client_access_revisions_client_number");

        builder.HasIndex(revision => new { revision.ClientId, revision.EffectiveFromUtc })
            .HasDatabaseName("ix_client_access_revisions_client_effective_from");

        builder.HasIndex(revision => revision.SourceInvoiceId)
            .HasDatabaseName("ix_client_access_revisions_source_invoice");

        builder.HasIndex(revision => revision.ContractId)
            .HasDatabaseName("ix_client_access_revisions_contract");

        builder.HasIndex(revision => revision.ProductCatalogRevisionId)
            .HasDatabaseName("ix_client_access_revisions_product_catalog_revision");

        builder.HasIndex(revision => revision.SupersedesRevisionId)
            .IsUnique()
            .HasFilter("supersedes_revision_id IS NOT NULL")
            .HasDatabaseName("ux_client_access_revisions_supersedes");

        builder.HasIndex(revision => revision.ClientId)
            .IsUnique()
            .HasFilter("supersedes_revision_id IS NULL")
            .HasDatabaseName("ux_client_access_revisions_client_root");

        builder.Navigation(revision => revision.Modules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(revision => revision.Modules, module =>
        {
            module.ToTable("client_access_revision_modules");

            module.WithOwner()
                .HasForeignKey("client_access_revision_id");

            module.Property<int>("client_access_revision_module_row_id")
                .HasColumnName("client_access_revision_module_row_id")
                .ValueGeneratedOnAdd();

            module.HasKey("client_access_revision_module_row_id");

            module.Property(revisionModule => revisionModule.ModuleCode)
                .HasColumnName("module_code")
                .HasMaxLength(64)
                .HasConversion(
                    moduleCode => moduleCode.Value,
                    value => ModuleCode.Create(value))
                .IsRequired();

            module.Property(revisionModule => revisionModule.IsEnabled)
                .HasColumnName("is_enabled")
                .IsRequired();

            module.HasIndex("client_access_revision_id", nameof(ClientAccessRevisionModule.ModuleCode))
                .IsUnique()
                .HasDatabaseName("ux_client_access_revision_modules_revision_code");
        });

        builder.OwnsMany(revision => revision.FeatureLimits, limit =>
        {
            limit.ToTable("client_access_revision_feature_limits", table =>
                table.HasCheckConstraint(
                    "ck_client_access_revision_feature_limits_value",
                    "limit_value >= 0"));

            limit.WithOwner()
                .HasForeignKey("client_access_revision_id");

            limit.Property<int>("client_access_revision_feature_limit_row_id")
                .HasColumnName("client_access_revision_feature_limit_row_id")
                .ValueGeneratedOnAdd();

            limit.HasKey("client_access_revision_feature_limit_row_id");

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
                    "client_access_revision_id",
                    nameof(ModuleFeatureLimit.ModuleCode),
                    nameof(ModuleFeatureLimit.FeatureCode))
                .IsUnique()
                .HasDatabaseName("ux_client_access_revision_feature_limits_revision_key");
        });

        builder.Navigation(revision => revision.FeatureLimits)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
