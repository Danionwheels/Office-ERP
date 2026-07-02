namespace SafarSuite.ControlDesk.Application.Modules.Billing.VoidInvoice;

public sealed record VoidInvoiceResult(
    Guid InvoiceId,
    string InvoiceNumber,
    string InvoiceStatus,
    Guid OriginalJournalEntryId,
    Guid ReversalJournalEntryId,
    string ReversalJournalEntryStatus,
    DateOnly VoidDate,
    decimal TotalDebit,
    decimal TotalCredit,
    string CurrencyCode,
    IReadOnlyCollection<VoidInvoiceJournalLineResult> JournalLines);

public sealed record VoidInvoiceJournalLineResult(
    Guid LedgerAccountId,
    decimal Debit,
    decimal Credit,
    string? Description);
