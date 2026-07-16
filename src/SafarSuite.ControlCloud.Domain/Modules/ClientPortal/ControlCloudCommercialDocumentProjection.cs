namespace SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

public static class ControlCloudCommercialDocumentTypes
{
    public const string Invoice = "Invoice";
    public const string Payment = "Payment";
    public const string CreditNote = "CreditNote";
    public const string Refund = "Refund";
    public const string CreditApplication = "CreditApplication";

    public static readonly IReadOnlyCollection<string> All =
    [
        Invoice,
        Payment,
        CreditNote,
        Refund,
        CreditApplication
    ];

    public static bool TryNormalize(string? value, out string documentType)
    {
        documentType = All.FirstOrDefault(candidate =>
            string.Equals(candidate, value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "";

        return documentType.Length > 0;
    }
}

public sealed record ControlCloudCommercialDocumentProjection(
    Guid ClientId,
    string DocumentType,
    Guid DocumentId,
    Guid? RelatedDocumentId,
    string Reference,
    string Status,
    DateOnly DocumentDate,
    decimal Amount,
    decimal BalanceAmount,
    string CurrencyCode,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset LastUpdatedAtUtc);

public abstract record ControlCloudCommercialProjectionChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc);

public sealed record ControlCloudInvoiceIssuedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    ControlCloudInvoiceProjection Invoice)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudInvoiceStatusChangedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    Guid InvoiceId,
    string InvoiceStatus,
    decimal BalanceDue)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudInvoiceVoidedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    Guid InvoiceId,
    string InvoiceStatus,
    decimal BalanceDue,
    DateOnly VoidedOn,
    string VoidReason)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudPaymentRecordedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    ControlCloudPaymentProjection Payment)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudPaymentReversedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    Guid PaymentId,
    Guid InvoiceId,
    string PaymentStatus,
    decimal Amount,
    decimal InvoiceBalanceDue)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudCreditNoteIssuedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    ControlCloudCreditNoteProjection CreditNote)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudRefundIssuedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    ControlCloudClientRefundProjection Refund)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudCreditAppliedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    ControlCloudCreditApplicationProjection CreditApplication)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);

public sealed record ControlCloudEntitlementIssuedChange(
    Guid ClientId,
    string CurrencyCode,
    Guid MessageId,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset ProjectedAtUtc,
    ControlCloudEntitlementProjection Entitlement)
    : ControlCloudCommercialProjectionChange(
        ClientId,
        CurrencyCode,
        MessageId,
        OccurredAtUtc,
        ProjectedAtUtc);
