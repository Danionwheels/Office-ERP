namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ClientPortalBillingSummaryResponse(
    decimal TotalOutstanding,
    int UnpaidInvoiceCount,
    DateOnly? LastPaymentDate,
    string CurrencyCode);

public sealed record ClientPortalInvoiceListResponse(
    IReadOnlyCollection<ClientPortalInvoiceListItemResponse> Invoices);

public sealed record ClientPortalInvoiceListItemResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceRemaining,
    string CurrencyCode,
    string Status);

public sealed record ClientPortalInvoiceDetailResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceRemaining,
    string CurrencyCode,
    string Status,
    ClientPortalBillingClientResponse Client,
    IReadOnlyCollection<ClientPortalInvoiceLineResponse> Lines,
    IReadOnlyCollection<ClientPortalInvoicePaymentResponse> Payments);

public sealed record ClientPortalBillingClientResponse(
    string Name,
    string? ContactName,
    string? Email,
    string? Phone);

public sealed record ClientPortalInvoiceLineResponse(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string CurrencyCode);

public sealed record ClientPortalInvoicePaymentResponse(
    Guid PaymentId,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    string Status,
    string Method);

public sealed record CreateClientPortalPaymentClaimRequest(
    Guid InvoiceId,
    decimal Amount,
    string TransferReferenceNumber,
    Guid? ProofAttachmentId);

public sealed record ClientPortalPaymentClaimListResponse(
    IReadOnlyCollection<ClientPortalPaymentClaimResponse> Claims);

public sealed record ClientPortalPaymentClaimResponse(
    Guid ClaimId,
    Guid ClientId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string CurrencyCode,
    string TransferReferenceNumber,
    Guid? ProofAttachmentId,
    ClientPortalAttachmentSummaryResponse? ProofAttachment,
    string Status,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? ReviewedAtUtc,
    string? RejectionReason);

public sealed record ClientPortalAttachmentSummaryResponse(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAtUtc);

public sealed record ClientPortalAttachmentUploadResponse(
    Guid AttachmentId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAtUtc);

public sealed record ClientPortalBankDetailsResponse(
    bool IsConfigured,
    string BankName,
    string AccountTitle,
    string AccountNumber,
    string Iban,
    string BranchOrRoutingInfo);

public sealed record RejectClientPortalPaymentClaimRequest(
    string Reason);

