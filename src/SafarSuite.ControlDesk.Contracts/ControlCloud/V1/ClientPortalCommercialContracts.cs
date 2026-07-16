namespace SafarSuite.ControlDesk.Contracts.ControlCloud.V1;

public sealed record ClientPortalCommercialSummaryResponse(
    Guid ClientId,
    string CurrencyCode,
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal TotalCredited,
    decimal TotalRefunded,
    decimal TotalCreditApplied,
    decimal BalanceDue,
    decimal AvailableCredit,
    bool IsPaid,
    DateTimeOffset LastUpdatedAtUtc,
    IReadOnlyCollection<ClientPortalInvoiceSummaryResponse> Invoices,
    IReadOnlyCollection<ClientPortalPaymentSummaryResponse> Payments,
    IReadOnlyCollection<ClientPortalCreditNoteSummaryResponse> CreditNotes,
    IReadOnlyCollection<ClientPortalRefundSummaryResponse> Refunds,
    IReadOnlyCollection<ClientPortalCreditApplicationSummaryResponse> CreditApplications,
    ClientPortalEntitlementSummaryResponse? LatestEntitlement);

public sealed record ClientPortalCommercialDocumentsPageResponse(
    Guid ClientId,
    string DocumentType,
    int PageSize,
    bool HasMore,
    string? NextCursor,
    IReadOnlyCollection<ClientPortalCommercialDocumentSummaryResponse> Items);

public sealed record ClientPortalCommercialDocumentSummaryResponse(
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

public sealed record ClientPortalInvoiceSummaryResponse(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid ContractId,
    string InvoiceStatus,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal BalanceDue,
    string CurrencyCode,
    DateOnly? VoidedOn,
    string? VoidReason);

public sealed record ClientPortalPaymentSummaryResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string PaymentStatus,
    string PaymentMethod,
    string PaymentReference,
    decimal Amount,
    decimal InvoiceBalanceDue,
    string CurrencyCode,
    DateOnly ReceivedOn);

public sealed record ClientPortalCreditNoteSummaryResponse(
    Guid CreditNoteId,
    string CreditNoteNumber,
    Guid InvoiceId,
    string InvoiceNumber,
    string CreditNoteStatus,
    DateOnly CreditDate,
    decimal Amount,
    string CurrencyCode,
    string Reason);

public sealed record ClientPortalRefundSummaryResponse(
    Guid RefundId,
    string RefundStatus,
    string RefundMethod,
    string RefundReference,
    decimal Amount,
    decimal ClientBalanceBefore,
    decimal ClientBalanceAfter,
    string CurrencyCode,
    DateOnly RefundedOn);

public sealed record ClientPortalCreditApplicationSummaryResponse(
    Guid CreditApplicationId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string CreditApplicationStatus,
    string Reference,
    decimal Amount,
    decimal InvoiceBalanceBefore,
    decimal InvoiceBalanceAfter,
    decimal AvailableCreditBefore,
    decimal AvailableCreditAfter,
    decimal ClientBalanceBefore,
    decimal ClientBalanceAfter,
    string CurrencyCode,
    DateOnly AppliedOn);

public sealed record ClientPortalEntitlementSummaryResponse(
    Guid EntitlementSnapshotId,
    Guid ClientAccessRevisionId,
    long EntitlementVersion,
    Guid ContractId,
    long ContractRevisionNumber,
    Guid ProductCatalogRevisionId,
    long ProductCatalogRevisionNumber,
    Guid SourceInvoiceId,
    string SourceInvoiceNumber,
    string Status,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyCollection<ClientPortalEntitlementModuleSummaryResponse> Modules,
    int? AllowedNamedUsers = null,
    int? AllowedConcurrentUsers = null,
    IReadOnlyCollection<ClientPortalEntitlementFeatureLimitSummaryResponse>? FeatureLimits = null);

public sealed record ClientPortalEntitlementModuleSummaryResponse(
    string ModuleCode,
    bool IsEnabled);

public sealed record ClientPortalEntitlementFeatureLimitSummaryResponse(
    string ModuleCode,
    string FeatureCode,
    long LimitValue,
    string Unit);
