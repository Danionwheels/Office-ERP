namespace SafarSuite.ControlDesk.Application.Modules.Payments.Common;

public sealed record PortalPaymentClaimResult(
    Guid ClaimId,
    Guid ClientId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string CurrencyCode,
    string TransferReferenceNumber,
    Guid? ProofAttachmentId,
    PortalPaymentClaimProofSummaryResult? ProofAttachment,
    string Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    string? RejectionReason);

public sealed record PortalPaymentClaimProofSummaryResult(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAtUtc);
