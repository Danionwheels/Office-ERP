using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Configurations;

public sealed class ControlCloudCommercialDocumentEntityConfiguration
    : IEntityTypeConfiguration<ControlCloudCommercialDocumentEntity>
{
    public void Configure(EntityTypeBuilder<ControlCloudCommercialDocumentEntity> builder)
    {
        builder.ToTable("commercial_documents");

        builder.HasKey(document => new
        {
            document.ClientId,
            document.DocumentType,
            document.DocumentId
        });

        builder.Property(document => document.ClientId)
            .HasColumnName("client_id")
            .ValueGeneratedNever();
        builder.Property(document => document.DocumentType)
            .HasColumnName("document_type")
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(document => document.DocumentId)
            .HasColumnName("document_id")
            .ValueGeneratedNever();
        builder.Property(document => document.RelatedDocumentId)
            .HasColumnName("related_document_id");
        builder.Property(document => document.Reference)
            .HasColumnName("reference")
            .HasMaxLength(160)
            .IsRequired();
        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(document => document.DocumentDate)
            .HasColumnName("document_date")
            .HasColumnType("date")
            .IsRequired();
        builder.Property(document => document.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);
        builder.Property(document => document.BalanceAmount)
            .HasColumnName("balance_amount")
            .HasPrecision(18, 2);
        builder.Property(document => document.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsRequired();
        builder.Property(document => document.LastMessageId)
            .HasColumnName("last_message_id")
            .ValueGeneratedNever();
        builder.Property(document => document.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();
        builder.Property(document => document.LastUpdatedAtUtc)
            .HasColumnName("last_updated_at_utc")
            .IsRequired();
        builder.Property(document => document.DetailJson)
            .HasColumnName("detail_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasOne<ControlCloudClientCommercialProjectionEntity>()
            .WithMany()
            .HasForeignKey(document => document.ClientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => new
            {
                document.ClientId,
                document.DocumentType,
                document.DocumentDate,
                document.DocumentId
            })
            .IsDescending(false, false, true, true)
            .HasDatabaseName("ix_commercial_documents_client_type_date_id");
        builder.HasIndex(document => new
            {
                document.ClientId,
                document.DocumentType,
                document.RelatedDocumentId,
                document.DocumentDate,
                document.DocumentId
            })
            .IsDescending(false, false, false, true, true)
            .HasDatabaseName("ix_commercial_documents_related_date_id");
    }
}
