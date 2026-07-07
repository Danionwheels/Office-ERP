namespace SafarSuite.ControlDesk.Application.Modules.Billing.IssueInvoice;

public sealed record IssueInvoiceResult(
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    Guid JournalEntryId,
    string JournalEntryStatus,
    DateOnly PostingDate,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<IssueInvoiceJournalLineResult> JournalLines);

public sealed record IssueInvoiceJournalLineResult(
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
