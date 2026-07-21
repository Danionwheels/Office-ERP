namespace SafarSuite.ControlDesk.Application.Modules.Payments.IssueClientRefund;

public sealed record IssueClientRefundResult(
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
    IReadOnlyCollection<IssueClientRefundJournalLineResult> JournalLines);

public sealed record IssueClientRefundJournalLineResult(
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
