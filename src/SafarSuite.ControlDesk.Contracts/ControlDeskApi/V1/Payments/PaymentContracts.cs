using SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Billing;

namespace SafarSuite.ControlDesk.Contracts.ControlDeskApi.V1.Payments;

public sealed record RecordInvoicePaymentRequest(
    Guid InvoiceId,
    string Method,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly ReceivedOn,
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate);

public sealed record RecordInvoicePaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string PaymentStatus,
    decimal Amount,
    decimal BalanceDue,
    string CurrencyCode,
    Guid? JournalEntryId,
    string? JournalEntryStatus,
    DateOnly? PostingDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<RecordInvoicePaymentJournalLineResponse> JournalLines);

public sealed record InvoicePaymentDocumentResponse(
    GenerateInvoiceDraftResponse Invoice,
    RecordInvoicePaymentResponse Payment,
    ReverseInvoicePaymentResponse? Reversal);

public sealed record RecordInvoicePaymentJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    string? LedgerAccountType,
    string? LedgerAccountNormalBalance,
    string? LedgerAccountLevel,
    bool? IsPostingAccount,
    string? LedgerAccountStatus);

public sealed record ApproveInvoicePaymentRequest(
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate,
    string? DecisionNote);

public sealed record ApproveInvoicePaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string PaymentStatus,
    decimal Amount,
    decimal BalanceDue,
    string CurrencyCode,
    Guid JournalEntryId,
    string JournalEntryStatus,
    DateOnly PostingDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<ApproveInvoicePaymentJournalLineResponse> JournalLines);

public sealed record ApproveInvoicePaymentJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    string? LedgerAccountType,
    string? LedgerAccountNormalBalance,
    string? LedgerAccountLevel,
    bool? IsPostingAccount,
    string? LedgerAccountStatus);

public sealed record RejectInvoicePaymentRequest(
    string DecisionNote);

public sealed record RejectInvoicePaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string PaymentStatus,
    string? DecisionNote);

public sealed record ReverseInvoicePaymentRequest(
    DateOnly ReversalDate,
    string DecisionNote);

public sealed record ReverseInvoicePaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string PaymentStatus,
    decimal Amount,
    decimal BalanceDue,
    string CurrencyCode,
    Guid ReversalJournalEntryId,
    string ReversalJournalEntryStatus,
    DateOnly ReversalDate,
    Guid OriginalJournalEntryId,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<ReverseInvoicePaymentJournalLineResponse> JournalLines);

public sealed record ReverseInvoicePaymentJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    string? LedgerAccountType,
    string? LedgerAccountNormalBalance,
    string? LedgerAccountLevel,
    bool? IsPostingAccount,
    string? LedgerAccountStatus);

public sealed record IssueClientRefundRequest(
    Guid ClientId,
    string Method,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly RefundedOn,
    Guid CashOrBankAccountId,
    Guid AccountsReceivableAccountId,
    DateOnly PostingDate,
    string? Note);

public sealed record IssueClientRefundResponse(
    Guid RefundId,
    Guid ClientId,
    string RefundStatus,
    string Method,
    string Reference,
    decimal Amount,
    decimal ClientBalanceBefore,
    decimal ClientBalanceAfter,
    string CurrencyCode,
    DateOnly RefundedOn,
    Guid JournalEntryId,
    string JournalEntryStatus,
    DateOnly PostingDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyCollection<IssueClientRefundJournalLineResponse> JournalLines);

public sealed record ClientRefundDocumentResponse(
    IssueClientRefundResponse Refund);

public sealed record IssueClientRefundJournalLineResponse(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    string? LedgerAccountType,
    string? LedgerAccountNormalBalance,
    string? LedgerAccountLevel,
    bool? IsPostingAccount,
    string? LedgerAccountStatus);

public sealed record ApplyClientCreditRequest(
    Guid ClientId,
    Guid InvoiceId,
    string Reference,
    decimal Amount,
    string CurrencyCode,
    DateOnly AppliedOn,
    string? Note);

public sealed record ApplyClientCreditResponse(
    Guid CreditApplicationId,
    Guid ClientId,
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    string Reference,
    decimal Amount,
    decimal InvoiceBalanceBefore,
    decimal InvoiceBalanceAfter,
    decimal AvailableCreditBefore,
    decimal AvailableCreditAfter,
    decimal ClientBalanceBefore,
    decimal ClientBalanceAfter,
    string CurrencyCode,
    DateOnly AppliedOn,
    string CreditApplicationStatus);
