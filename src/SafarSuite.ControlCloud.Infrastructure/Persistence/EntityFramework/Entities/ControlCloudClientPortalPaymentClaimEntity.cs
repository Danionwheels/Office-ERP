namespace SafarSuite.ControlCloud.Infrastructure.Persistence.EntityFramework.Entities;

public sealed class ControlCloudClientPortalPaymentClaimEntity
{
    public Guid ClaimId { get; set; }
    public Guid ClientId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public Guid InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "PKR";
    public string TransferReferenceNumber { get; set; } = "";
    public string NormalizedTransferReferenceNumber { get; set; } = "";
    public Guid? ProofAttachmentId { get; set; }
    public string Status { get; set; } = "";
    public DateTimeOffset SubmittedAtUtc { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public Guid? VerifiedPaymentId { get; set; }
    public string? RejectionReason { get; set; }
    public Guid ConcurrencyToken { get; set; }
}
