namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudCommercialDocumentEntity
{
    public Guid ClientId { get; set; }

    public string DocumentType { get; set; } = "";

    public Guid DocumentId { get; set; }

    public Guid? RelatedDocumentId { get; set; }

    public string Reference { get; set; } = "";

    public string Status { get; set; } = "";

    public DateOnly DocumentDate { get; set; }

    public decimal Amount { get; set; }

    public decimal BalanceAmount { get; set; }

    public string CurrencyCode { get; set; } = "PKR";

    public Guid LastMessageId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public DateTimeOffset LastUpdatedAtUtc { get; set; }

    public string DetailJson { get; set; } = "{}";
}
