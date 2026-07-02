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
    Guid ContractId,
    Guid SourceInvoiceId,
    string SourceInvoiceNumber,
    string Status,
    DateOnly PaidUntil,
    DateOnly GraceUntil,
    DateOnly OfflineValidUntil,
    int AllowedDevices,
    int AllowedBranches,
    DateTimeOffset IssuedAtUtc,
    IReadOnlyCollection<ClientPortalEntitlementModuleSummaryResponse> Modules);

public sealed record ClientPortalEntitlementModuleSummaryResponse(
    string ModuleCode,
    bool IsEnabled);
