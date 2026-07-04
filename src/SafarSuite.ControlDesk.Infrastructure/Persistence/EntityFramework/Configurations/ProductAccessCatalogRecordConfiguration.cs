using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

namespace SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework.Configurations;

internal sealed class ProductAccessCatalogRecordConfiguration : IEntityTypeConfiguration<ProductAccessCatalogRecord>
{
    public void Configure(EntityTypeBuilder<ProductAccessCatalogRecord> builder)
    {
        builder.ToTable("product_access_catalogs");

        builder.HasKey(catalog => catalog.CatalogId);

        builder.Property(catalog => catalog.CatalogId)
            .HasColumnName("catalog_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(catalog => catalog.ModuleGroupsJson)
            .HasColumnName("module_groups_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(catalog => catalog.ResourcesJson)
            .HasColumnName("resources_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(catalog => catalog.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.Property(catalog => catalog.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(160)
            .IsRequired();
    }
}
