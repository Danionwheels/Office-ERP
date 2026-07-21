using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Domain.Modules.Contracts;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ProductCatalogRevisionRecordConfiguration
    : IEntityTypeConfiguration<ProductCatalogRevisionRecord>
{
    public void Configure(EntityTypeBuilder<ProductCatalogRevisionRecord> builder)
    {
        builder.ToTable(
            "product_catalog_revisions",
            table => table.HasCheckConstraint(
                "ck_product_catalog_revisions_lineage",
                "(revision_number = 1 AND supersedes_catalog_revision_id IS NULL) OR " +
                "(revision_number > 1 AND supersedes_catalog_revision_id IS NOT NULL)"));

        builder.HasKey(revision => revision.CatalogRevisionId);

        builder.HasAlternateKey(revision => new
            {
                revision.CatalogRevisionId,
                revision.RevisionNumber
            })
            .HasName("ak_product_catalog_revisions_id_number");

        builder.Property(revision => revision.CatalogRevisionId)
            .HasColumnName("catalog_revision_id")
            .HasConversion(
                id => id.Value,
                value => ProductCatalogRevisionId.Create(value))
            .ValueGeneratedNever();

        builder.Property(revision => revision.RevisionNumber)
            .HasColumnName("revision_number")
            .IsRequired();

        builder.Property(revision => revision.SupersedesCatalogRevisionId)
            .HasColumnName("supersedes_catalog_revision_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? ProductCatalogRevisionId.Create(value.Value) : null);

        builder.HasOne<ProductCatalogRevisionRecord>()
            .WithMany()
            .HasForeignKey(revision => revision.SupersedesCatalogRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(revision => revision.ModulesJson)
            .HasColumnName("modules_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(revision => revision.ModuleGroupsJson)
            .HasColumnName("module_groups_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(revision => revision.ResourcesJson)
            .HasColumnName("resources_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(revision => revision.ChangeReason)
            .HasColumnName("change_reason")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(revision => revision.PublishedBy)
            .HasColumnName("published_by")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(revision => revision.PublishedAtUtc)
            .HasColumnName("published_at_utc")
            .IsRequired();

        builder.HasIndex(revision => revision.RevisionNumber)
            .IsUnique()
            .HasDatabaseName("ux_product_catalog_revisions_number");

        builder.HasIndex(revision => revision.SupersedesCatalogRevisionId)
            .IsUnique()
            .HasFilter("supersedes_catalog_revision_id IS NOT NULL")
            .HasDatabaseName("ux_product_catalog_revisions_supersedes");
    }
}
